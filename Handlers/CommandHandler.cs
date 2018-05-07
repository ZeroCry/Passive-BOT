﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ApiAiSDK;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using PassiveBOT.Configuration;
using PassiveBOT.Handlers.Services;
using PassiveBOT.strings;
using RedditSharp.Things;

namespace PassiveBOT.Handlers
{
    public class CommandHandler
    {
        public static List<CMD> CommandUses = new List<CMD>();

        public static List<SubReddit> SubReddits = new List<SubReddit>();
        public static List<Con4GameList> Connect4List = new List<Con4GameList>();
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly TimerService _service;
        private readonly List<NoSpamGuild> NoSpam = new List<NoSpamGuild>();
        private readonly IServiceProvider Provider;
        private ApiAi _apiAi;

        public List<Delays> AntiSpamMsgDelays = new List<Delays>();
        private bool DoOnce;

        public CommandHandler(IServiceProvider provider)
        {
            Provider = provider;
            _client = Provider.GetService<DiscordSocketClient>();
            _commands = new CommandService();
            _service = provider.GetService<TimerService>();

            _client.MessageReceived += DoCommand;
            _client.Ready += _client_Ready;
        }

        private Task _client_Ready()
        {
            var inv =
                $"https://discordapp.com/oauth2/authorize?client_id={_client.CurrentUser.Id}&scope=bot&permissions=2146958591";
            LogHandler.LogInfo($"Invite: {inv}");
            return Task.CompletedTask;
        }

        public async Task ConfigureAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        private async Task AutoMessage(SocketUserMessage message, SocketCommandContext context)
        {
            if (context.Channel is IDMChannel) return;

            var guild = GuildConfig.GetServer(context.Guild);
            try
            {
                if (!(context.Channel is IDMChannel))
                    if (File.Exists(Path.Combine(AppContext.BaseDirectory, $"setup/server/{context.Guild.Id}.json")) &&
                        guild.AutoMessage.Any(x => x.channelID == context.Channel.Id))
                    {
                        var chan = guild.AutoMessage.First(x => x.channelID == context.Channel.Id);
                        if (chan.enabled)
                        {
                            chan.messages++;
                            if (chan.messages >= chan.sendlimit)
                            {
                                var embed = new EmbedBuilder();
                                embed.AddField("AutoMessage", chan.automessage);
                                embed.Color = Color.Green;
                                await context.Channel.SendMessageAsync("", false, embed.Build());
                                chan.messages = 0;
                            }

                            GuildConfig.SaveServer(guild);
                        }
                    }
            }
            catch
            {
                //
            }
        }

        private async Task<bool> CheckMessage(SocketUserMessage message, SocketCommandContext context)
        {
            if (context.Channel is IDMChannel) return false;

            var guild = GuildConfig.GetServer(context.Guild);
            var excemtcheck =
                guild.Antispams.IngoreRoles.Where(x => ((IGuildUser) context.User).RoleIds.Contains(x.RoleID)).ToList();
            var BypassAntispam = excemtcheck.Any(x => x.AntiSpam);
            var BypassMention = excemtcheck.Any(x => x.Mention);
            var BypassInvite = excemtcheck.Any(x => x.Advertising);
            var BypassBlacklist = excemtcheck.Any(x => x.Blacklist);
            var BypassIP = excemtcheck.Any(x => x.Privacy);
            if (guild.Antispams.Antispam.NoSpam || guild.Levels.LevellingEnabled)
            {
                var detected = false;
                var SpamGuild = NoSpam.FirstOrDefault(x => x.guildID == ((SocketGuildUser) context.User).Guild.Id);
                if (SpamGuild == null)
                {
                    NoSpam.Add(new NoSpamGuild
                    {
                        guildID = ((SocketGuildUser) context.User).Guild.Id,
                        Users = new List<NoSpamGuild.NoSpam>
                        {
                            new NoSpamGuild.NoSpam
                            {
                                UserID = context.User.Id,
                                Messages = new List<NoSpamGuild.NoSpam.msg>
                                {
                                    new NoSpamGuild.NoSpam.msg
                                    {
                                        LastMessage = message.Content,
                                        LastMessageDate = DateTime.UtcNow
                                    }
                                }
                            }
                        }
                    });
                }
                else
                {
                    var user = SpamGuild.Users.FirstOrDefault(x => x.UserID == context.User.Id);
                    if (user == null)
                    {
                        SpamGuild.Users.Add(new NoSpamGuild.NoSpam
                        {
                            UserID = context.User.Id,
                            Messages = new List<NoSpamGuild.NoSpam.msg>
                            {
                                new NoSpamGuild.NoSpam.msg
                                {
                                    LastMessage = message.Content,
                                    LastMessageDate = DateTime.UtcNow
                                }
                            }
                        });
                    }
                    else
                    {
                        user.Messages.Add(new NoSpamGuild.NoSpam.msg
                        {
                            LastMessage = message.Content,
                            LastMessageDate = DateTime.UtcNow
                        });
                        if (user.Messages.Count >= 2)
                        {
                            var msgs = user.Messages.Where(x =>
                                x.LastMessageDate > DateTime.UtcNow - TimeSpan.FromSeconds(10)).ToList();
                            if (msgs.GroupBy(n => n.LastMessage.ToLower()).Any(c => c.Count() > 1)) detected = true;

                            if (msgs.Count(x => x.LastMessageDate > DateTime.UtcNow - TimeSpan.FromSeconds(5)) > 3)
                                detected = true;
                        }

                        if (user.Messages.Count > 10)
                        {
                            var msgs = user.Messages.OrderBy(x => x.LastMessageDate).ToList();
                            msgs.RemoveRange(0, 1);
                            msgs = msgs.Where(x => x.LastMessageDate > DateTime.UtcNow - TimeSpan.FromSeconds(10))
                                .ToList();
                            user.Messages = msgs;
                        }

                        if (detected && guild.Antispams.Antispam.NoSpam)
                            if (!BypassAntispam)
                                if (!guild.Antispams.Antispam.AntiSpamSkip.Any(x =>
                                    message.Content.ToLower().Contains(x.ToLower())))
                                {
                                    await message.DeleteAsync();
                                    var delay = AntiSpamMsgDelays.FirstOrDefault(x => x.GuildID == guild.GuildId);
                                    if (delay != null)
                                    {
                                        if (delay._delay > DateTime.UtcNow)
                                            return true;
                                        delay._delay = DateTime.UtcNow.AddSeconds(5);
                                        var emb = new EmbedBuilder
                                        {
                                            Title = $"{context.User} - No Spamming!!"
                                        };
                                        await context.Channel.SendMessageAsync("", false, emb.Build());
                                    }
                                    else
                                    {
                                        AntiSpamMsgDelays.Add(new Delays
                                        {
                                            _delay = DateTime.UtcNow.AddSeconds(5),
                                            GuildID = guild.GuildId
                                        });
                                    }

                                    return true;
                                }

                        if (!detected && guild.Levels.LevellingEnabled)
                        {
                            var apos = 0;
                            if (!context.Message.HasStringPrefix(Config.Load().Prefix, ref apos))
                                if (!(context.Message.HasStringPrefix(guild.Prefix, ref apos) &&
                                      !string.IsNullOrEmpty(guild.Prefix)))
                                    try
                                    {
                                        var userlv =
                                            guild.Levels.Users.FirstOrDefault(x => x.userID == context.User.Id);
                                        if (userlv != null)
                                        {
                                            if (!userlv.banned)
                                            {
                                                userlv.xp = userlv.xp + 10;
                                                var requiredxp = userlv.level * 50 + userlv.level * userlv.level * 25;
                                                if (userlv.xp >= requiredxp)
                                                {
                                                    userlv.level++;
                                                    string roleadded = null;
                                                    if (guild.Levels.LevelRoles.Any())
                                                    {
                                                        var rolesavailable =
                                                            guild.Levels.LevelRoles
                                                                .Where(x => x.LevelToEnter <= userlv.level - 1)
                                                                .ToList();
                                                        var roletoreceive = new List<GuildConfig.levelling.Level>();
                                                        if (rolesavailable.Any())
                                                            if (guild.Levels.IncrementLevelRewards)
                                                            {
                                                                var maxrole = rolesavailable.Max(x => x.LevelToEnter);
                                                                roletoreceive.Add(
                                                                    rolesavailable.FirstOrDefault(
                                                                        x => x.LevelToEnter == maxrole));
                                                            }
                                                            else
                                                            {
                                                                roletoreceive = rolesavailable;
                                                            }


                                                        if (roletoreceive.Count != 0)
                                                        {
                                                            foreach (var role in roletoreceive)
                                                                if (!((IGuildUser) context.User).RoleIds.Contains(
                                                                    role.RoleID))
                                                                {
                                                                    var grole = context.Guild.GetRole(role.RoleID);
                                                                    if (grole != null)
                                                                        try
                                                                        {
                                                                            await ((SocketGuildUser) context.User)
                                                                                .AddRoleAsync(
                                                                                    grole);
                                                                            roleadded += $"Role Reward: {grole.Name}\n";
                                                                        }
                                                                        catch
                                                                        {
                                                                            //
                                                                        }
                                                                    else
                                                                        guild.Levels.LevelRoles.Remove(role);
                                                                }

                                                            if (roletoreceive.Count != rolesavailable.Count &&
                                                                roletoreceive.Count == 1)
                                                            {
                                                                rolesavailable.Remove(roletoreceive.First());
                                                                var roles = rolesavailable
                                                                    .Select(x => context.Guild.GetRole(x.RoleID))
                                                                    .Where(x => x != null);

                                                                await ((SocketGuildUser) context.User).RemoveRolesAsync(
                                                                    roles);
                                                            }
                                                        }
                                                    }


                                                    var embed = new EmbedBuilder
                                                    {
                                                        Title = $"{context.User.Username} Levelled Up!",
                                                        ThumbnailUrl = context.User.GetAvatarUrl(),
                                                        Description = $"Level: {userlv.level - 1}\n" +
                                                                      $"{roleadded}" +
                                                                      $"XP: {requiredxp}\n" +
                                                                      $"Next Level At: {userlv.level * 50 + userlv.level * userlv.level * 25} XP",
                                                        Color = Color.Blue
                                                    };
                                                    if (guild.Levels.UseLevelChannel)
                                                    {
                                                        var chan = context.Guild.GetChannel(guild.Levels
                                                            .LevellingChannel);
                                                        if (chan != null)
                                                            await ((IMessageChannel) chan).SendMessageAsync("", false,
                                                                embed.Build());
                                                    }

                                                    if (guild.Levels.UseLevelMessages)
                                                        await context.Channel.SendMessageAsync("", false,
                                                            embed.Build());
                                                }
                                            }
                                        }
                                        else
                                        {
                                            guild.Levels.Users.Add(new GuildConfig.levelling.user
                                            {
                                                userID = context.User.Id,
                                                banned = false,
                                                level = 1,
                                                xp = 0
                                            });
                                        }

                                        GuildConfig.SaveServer(guild);
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine(e);
                                    }
                        }
                    }
                }
            }

            if (guild.Antispams.Advertising.Invite && !BypassInvite)
                if (message.Content.Contains("discord.gg"))
                {
                    await message.DeleteAsync();
                    var emb = new EmbedBuilder
                    {
                        Description =
                            guild.Antispams.Advertising.NoInviteMessage ??
                            $"{context.User.Mention} - Pls Daddy, no sending invite links... the admins might get angry"
                    };
                    await context.Channel.SendMessageAsync("", false, emb.Build());
                    //if
                    // 1. The server Has Invite Deletions turned on
                    // 2. The user is not an admin
                    // 3. The user does not have one of the invite excempt roles
                    return true;
                }

            if (guild.Antispams.Mention.RemoveMassMention && !BypassMention)
                if (message.MentionedRoles.Count + message.MentionedUsers.Count >= 5)
                {
                    await message.DeleteAsync();
                    var emb = new EmbedBuilder
                    {
                        Title = $"{context.User} - This server does not allow you to mention 5+ roles or uses at once"
                    };
                    await context.Channel.SendMessageAsync("", false, emb.Build());
                    return true;
                }


            if (guild.Antispams.Privacy.RemoveIPs && !BypassIP)
                if (Regex.IsMatch(message.Content,
                    @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$"))
                {
                    await message.DeleteAsync();
                    var emb = new EmbedBuilder
                    {
                        Title = $"{context.User} - This server does not allow you to post IP addresses"
                    };
                    await context.Channel.SendMessageAsync("", false, emb.Build());
                    return true;
                }


            if (guild.Antispams.Mention.MentionAll && !BypassMention)
                if (message.Content.Contains("@everyone") || message.Content.Contains("@here"))
                {
                    await message.DeleteAsync();

                    var rnd = new Random();
                    var res = rnd.Next(0, FunStr.Everyone.Length);
                    var emb = new EmbedBuilder();
                    if (guild.Antispams.Mention.MentionAllMessage != null)
                    {
                        emb.Description = guild.Antispams.Mention.MentionAllMessage;
                    }
                    else
                    {
                        emb.Title = $"{context.User} - the admins might get angry";
                        emb.ImageUrl = FunStr.Everyone[res];
                    }

                    await context.Channel.SendMessageAsync("", false, emb.Build());
                    return true;
                    //if
                    // 1. The server Has Mention Deletions turned on
                    // 2. The user is not an admin
                    // 3. The user does not have one of the mention excempt roles
                }


            if (guild.Antispams.Blacklist.BlacklistWordSet.Any() && !BypassBlacklist)
            {
                var blacklistdetected = false;
                var blacklistmessage = guild.Antispams.Blacklist.DefaultBlacklistMessage;

                if (guild.Antispams.Blacklist.BlacklistBetterFilter)
                {
                    var detectedblacklistmodule = guild.Antispams.Blacklist.BlacklistWordSet.FirstOrDefault(blist =>
                        blist.WordList.Any(x =>
                            ProfanityFilter.doreplacements(ProfanityFilter.RemoveDiacritics(context.Message.Content))
                                .ToLower().Contains(ProfanityFilter.doreplacements(ProfanityFilter.RemoveDiacritics(x))
                                    .ToLower())));
                    if (detectedblacklistmodule != null)
                    {
                        blacklistdetected = true;
                        blacklistmessage = detectedblacklistmodule.BlacklistResponse ??
                                           guild.Antispams.Blacklist.DefaultBlacklistMessage;
                    }
                }
                else
                {
                    var detectedblacklistmodule = guild.Antispams.Blacklist.BlacklistWordSet.FirstOrDefault(blist =>
                        blist.WordList.Any(x => context.Message.Content.ToLower().Contains(x.ToLower())));
                    if (detectedblacklistmodule != null)
                    {
                        blacklistdetected = true;
                        blacklistmessage = detectedblacklistmodule.BlacklistResponse ??
                                           guild.Antispams.Blacklist.DefaultBlacklistMessage;
                    }
                }

                if (blacklistdetected)
                {
                    await message.DeleteAsync();

                    if (!string.IsNullOrEmpty(blacklistmessage))
                    {
                        //var responsemessage = blacklistmessage.Replace("{user}", context.User.Username)
                        //    .Replace("{user.mention}", context.User.Mention).Replace("{guild}", context.Guild.Name)
                        //    .Replace("{channel}", context.Channel.Name).Replace("{channel.mention}",
                        //        ((SocketTextChannel) context.Channel).Mention);

                        var result = Regex.Replace(blacklistmessage, "{user}", context.User.Username,
                            RegexOptions.IgnoreCase);
                        result = Regex.Replace(result, "{user.mention}", context.User.Mention, RegexOptions.IgnoreCase);
                        result = Regex.Replace(result, "{guild}", context.Guild.Name, RegexOptions.IgnoreCase);
                        result = Regex.Replace(result, "{channel}", context.Channel.Name, RegexOptions.IgnoreCase);
                        result = Regex.Replace(result, "{channel.mention}",
                            ((SocketTextChannel) context.Channel).Mention, RegexOptions.IgnoreCase);
                        await context.Channel.SendMessageAsync(result);
                        return true;
                    }
                }
            }

            return false;
        }

        private void InitialisePartnerProgram()
        {
            if (DoOnce) return;
            try
            {
                var config = new AIConfiguration(Tokens.Load().DialogFlowToken, SupportedLanguage.English);
                _apiAi = new ApiAi(config);
            }
            catch (Exception e)
            {
                //Console.WriteLine(e);
            }

            try
            {
                foreach (var guild in _client.Guilds)
                    try
                    {
                        //GuildConfig.Setup(guild);
                        var guildconfig = GuildConfig.GetServer(guild);
                        if (guildconfig.PartnerSetup.PartherChannel == 0) continue;
                        if (guildconfig.PartnerSetup.IsPartner &&
                            _client.GetChannel(guildconfig.PartnerSetup.PartherChannel) is IMessageChannel)
                            TimerService.AcceptedServers.Add(guild.Id);
                    }
                    catch //(Exception e)
                    {
                        //Console.WriteLine(e);
                    }

                _service.Restart();
            }
            catch //(Exception e)
            {
                //Console.WriteLine(e);
            }

            DoOnce = true;
        }

        private async Task DoCommand(SocketMessage parameterMessage)
        {
            Load.Messages++;
            if (!(parameterMessage is SocketUserMessage message)) return;
            var argPos = 0;
            var context = new SocketCommandContext(_client, message); //new CommandContext(_client, message);
            if (context.User.IsBot) return;

            InitialisePartnerProgram();

            if (await CheckMessage(message, context)) return;

            await AutoMessage(message, context);
            var guild = GuildConfig.GetServer(context.Guild);
            if (message.HasMentionPrefix(_client.CurrentUser, ref argPos) && guild.chatwithmention)
            {
                var mcontent = context.Message.MentionedUsers.Aggregate(message.Content,
                    (current, mentionedUser) => current.Replace(mentionedUser.Mention, mentionedUser.Username));
                mcontent = context.Message.MentionedRoles.Aggregate(mcontent,
                    (current, mentionedRole) => current.Replace(mentionedRole.Mention, mentionedRole.Name));
                mcontent = context.Message.MentionedChannels.Aggregate(mcontent,
                    (current, mentionedChannel) =>
                        current.Replace(((ITextChannel) mentionedChannel).Mention, mentionedChannel.Name));
                //var newmessage = Regex.Replace(context.Message.Content, @"^\!?<@[0-9]+>\s*", "",
                //    RegexOptions.Multiline);
                try
                {
                    var response = _apiAi.TextRequest(mcontent);
                    if (response.Result.Fulfillment.Speech != "")
                        await context.Channel.SendMessageAsync(response.Result.Fulfillment.Speech);
                }
                catch
                {
                    //
                }

                return;
            }

            if (!(message.HasMentionPrefix(_client.CurrentUser, ref argPos) ||
                  message.HasStringPrefix(Load.Pre, ref argPos) ||
                  message.HasStringPrefix(GuildConfig.GetServer(context.Guild).Prefix, ref argPos))) return;

            if (Homeserver.Load().GlobalBans.Any(x => x.ID == context.User.Id))
                return;

            var result = await _commands.ExecuteAsync(context, argPos, Provider);
            var commandsuccess = result.IsSuccess;

            var server = context.Channel is IPrivateChannel ? "Direct Message " : context.Guild.Name;

            if (!commandsuccess)
            {
                try
                {
                    if (!(result.ErrorReason == "Unknown command." ||
                          result.ErrorReason == "The input text has too many parameters." ||
                          result.ErrorReason == "The input text has too few parameters." ||
                          result.ErrorReason == "Timeout" ||
                          result.ErrorReason == "This command may only be invoked in an NSFW channel." ||
                          result.ErrorReason == "Command can only be run by the owner of the bot" ||
                          result.ErrorReason == "This command is locked to NSFW Channels. Pervert."))
                    {
                        var s = Homeserver.Load().Error;
                        var c = context.Client.GetChannel(s);
                        var embed = new EmbedBuilder
                        {
                            Title = $"ERROR: {context.Message}",
                            Description = $"REASON:\n" +
                                          $"{result.ErrorReason}"
                        };
                        embed.WithFooter(x => { x.Text = $"{context.Message.CreatedAt} || {context.Guild.Name}"; });
                        embed.Color = Color.Red;
                        await ((ITextChannel) c).SendMessageAsync("", false, embed.Build());
                    }
                }
                catch
                {
                    //
                }

                var errmessage = await context.Channel.SendMessageAsync(
                    $"​**COMMAND: **{context.Message} \n**ERROR: **{result.ErrorReason}"); //if in server error responses are enabled reply on error
                await Task.Delay(5000);
                await errmessage.DeleteAsync();
                try
                {
                    await context.Message.DeleteAsync();
                }
                catch
                {
                    //
                }

                await LogHandler.In3Error($"{context.Message}", 'S', $"{context.Guild.Name}", 'E',
                    $"{result.ErrorReason}"); // log errors as arrors
            }
            else
            {
                await LogHandler.In3(
                    $"{context.Message}", 'S', $"{server}", 'U', $"{context.User}"); //if there is no error log normally

                Load.Commands++;
                var srch = _commands.Search(context, argPos);
                if (srch.IsSuccess)
                {
                    var name = srch.Commands.Select(x => x.Command.Name).FirstOrDefault();
                    if (name != null)
                        if (CommandUses.Any(x => x.Name.ToLower() == name.ToLower()))
                        {
                            var cmd = CommandUses.First(x => x.Name.ToLower() == name.ToLower());
                            cmd.Uses++;
                        }
                        else
                        {
                            CommandUses.Add(new CMD
                            {
                                Name = name.ToLower(),
                                Uses = 1
                            });
                        }
                }
            }
        }

        public class Con4GameList
        {
            public ulong channelID { get; set; }
            public bool gamerunning { get; set; } = false;
        }

        public class CMD
        {
            public string Name { get; set; }
            public int Uses { get; set; }
        }

        private class NoSpamGuild
        {
            public ulong guildID { get; set; }
            public List<NoSpam> Users { get; set; } = new List<NoSpam>();

            public class NoSpam
            {
                public ulong UserID { get; set; }
                public List<msg> Messages { get; set; } = new List<msg>();

                public class msg
                {
                    public string LastMessage { get; set; }
                    public DateTime LastMessageDate { get; set; }
                }
            }
        }

        public class Delays
        {
            public DateTime _delay { get; set; } = DateTime.UtcNow;
            public ulong GuildID { get; set; }
        }

        public class SubReddit
        {
            public string title { get; set; }
            public List<Post> Posts { get; set; }
            public DateTime LastUpdate { get; set; }
            public int Hits { get; set; } = 0;
        }
    }
}