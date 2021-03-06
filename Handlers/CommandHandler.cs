﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ApiAiSDK;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using PassiveBOT.Configuration;
using PassiveBOT.Handlers.Services;
using RedditSharp.Things;

namespace PassiveBOT.Handlers
{
    public class CommandHandler
    {
        public static List<CMD> CommandUses = new List<CMD>();
        public static List<SubReddit> SubReddits = new List<SubReddit>();
        public static List<Con4GameList> Connect4List = new List<Con4GameList>();
        public static List<LevellingObj> Levels = new List<LevellingObj>();
        public static List<AutoMessages> AutomessageList = new List<AutoMessages>();
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly TimerService _service;
        private readonly IServiceProvider Provider;
        private ApiAi _apiAi;

        public List<EventHandler.Delays> AntiSpamMsgDelays = new List<EventHandler.Delays>();
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

        private static async Task AutoMessage(SocketCommandContext context)
        {
            if (context.Channel is IDMChannel) return;

            var gauto = AutomessageList.FirstOrDefault(x => x.GuildID == context.Guild.Id)?.Channels.FirstOrDefault(x => x.channelID == context.Channel.Id);
            if (gauto != null)
            {
                gauto.messages++;
                if (gauto.messages > gauto.sendlimit)
                {
                    var guild = GuildConfig.GetServer(context.Guild);
                    var autochannel = guild.AutoMessage.FirstOrDefault(x => x.channelID == context.Channel.Id);
                    if (autochannel != null && autochannel.enabled)
                    {
                        var embed = new EmbedBuilder
                        {
                            Title = "AutoMessage",
                            Description = autochannel.automessage,
                            Color = Color.Green
                        };
                        await context.Channel.SendMessageAsync("", false, embed.Build());
                        gauto.messages = 0;
                    }
                }
            }
        }

        private async Task<bool> CheckMessage(SocketUserMessage message, SocketCommandContext context, GuildConfig guild)
        {
            if (context.Channel is IDMChannel) return false;
            try
            {
                var gmc = Load.GuildMsgCounts.FirstOrDefault(x => x.GuildID == context.Guild.Id);
                if (gmc == null)
                {
                    Load.GuildMsgCounts.Add(new Load.GuildMSGCount
                    {
                        GuildID = context.Guild.Id,
                        msgs = 0
                    });
                }
                else
                {
                    gmc.msgs++;
                }

                if (Homeserver.Load().DisableCheckMsg) return false;


                if (guild.Levels.LevellingEnabled)
                {
                    var apos = 0;
                    if (!context.Message.HasStringPrefix(Config.Load().Prefix, ref apos) && !(context.Message.HasStringPrefix(guild.Prefix, ref apos) && !string.IsNullOrEmpty(guild.Prefix)))
                    {
                        try
                        {
                            //var userlv = guild.Levels.Users.FirstOrDefault(x => x.userID == context.User.Id);
                            var LVGuild = Levels.FirstOrDefault(x => x.GuildID == guild.GuildId);
                            if (LVGuild == null)
                            {
                                Levels.Add(new LevellingObj
                                {
                                    GuildID = context.Guild.Id,
                                    UpdatesSinceLastSave = 0,
                                    Users = guild.Levels.Users
                                });
                                LVGuild = Levels.FirstOrDefault(x => x.GuildID == guild.GuildId);
                            }

                            var userlv = LVGuild.Users.FirstOrDefault(x => x.userID == context.User.Id);
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
                                            var rolesavailable = guild.Levels.LevelRoles.Where(x => x.LevelToEnter <= userlv.level - 1).ToList();
                                            var roletoreceive = new List<GuildConfig.levelling.Level>();
                                            if (rolesavailable.Any())
                                            {
                                                if (guild.Levels.IncrementLevelRewards)
                                                {
                                                    var maxrole = rolesavailable.Max(x => x.LevelToEnter);
                                                    roletoreceive.Add(rolesavailable.FirstOrDefault(x =>
                                                        x.LevelToEnter == maxrole));
                                                }
                                                else
                                                {
                                                    roletoreceive = rolesavailable;
                                                }
                                            }

                                            if (roletoreceive.Count != 0)
                                            {
                                                foreach (var role in roletoreceive)
                                                {
                                                    if (((IGuildUser) context.User).RoleIds.Contains(role.RoleID)) continue;
                                                    var grole = context.Guild.GetRole(role.RoleID);
                                                    if (grole != null)
                                                    {
                                                        try
                                                        {
                                                            await ((SocketGuildUser) context.User).AddRoleAsync(grole);
                                                            roleadded += $"Role Reward: {grole.Name}\n";
                                                        }
                                                        catch
                                                        {
                                                            //
                                                        }
                                                    }
                                                    else
                                                    {
                                                        guild.Levels.LevelRoles.Remove(role);
                                                    }
                                                }

                                                if (roletoreceive.Count != rolesavailable.Count && roletoreceive.Count == 1)
                                                {
                                                    try
                                                    {
                                                        rolesavailable.Remove(roletoreceive.First());
                                                        var roles = rolesavailable.Select(x => context.Guild.GetRole(x.RoleID)).Where(x => x != null);

                                                        await ((SocketGuildUser) context.User).RemoveRolesAsync(roles);
                                                    }
                                                    catch
                                                    {
                                                        //
                                                    }
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
                                            try
                                            {
                                                if (context.Guild.GetChannel(guild.Levels.LevellingChannel) is IMessageChannel chan)
                                                {
                                                    await chan.SendMessageAsync("", false, embed.Build());
                                                }
                                            }
                                            catch
                                            {
                                                //
                                            }
                                        }

                                        if (guild.Levels.UseLevelMessages)
                                        {
                                            try
                                            {
                                                await context.Channel.SendMessageAsync("", false, embed.Build());
                                            }
                                            catch
                                            {
                                                //
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                LVGuild.Users.Add(new GuildConfig.levelling.user
                                {
                                    userID = context.User.Id,
                                    banned = false,
                                    level = 1,
                                    xp = 0
                                });
                            }

                            LVGuild.UpdatesSinceLastSave++;
                            if (LVGuild.UpdatesSinceLastSave > 25)
                            {
                                guild.Levels.Users = LVGuild.Users;
                                GuildConfig.SaveServer(guild);
                                LVGuild.UpdatesSinceLastSave = 0;
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("CheckMessage Failed.");
                Console.WriteLine(e);
            }

            return false;
        }

        private void Initialise()
        {
            if (DoOnce) return;
            try
            {
                var config = new AIConfiguration(Tokens.Load().DialogFlowToken, SupportedLanguage.English);
                _apiAi = new ApiAi(config);
            }
            catch
            {
                //
            }

            try
            {
                foreach (var guild in _client.Guilds)
                    try
                    {
                        var guildconfig = GuildConfig.GetServer(guild);

                        try
                        {
                            if (guildconfig.Levels.LevellingEnabled)
                            {
                                Levels.Add(new LevellingObj
                                {
                                    GuildID = guildconfig.GuildId,
                                    Users = guildconfig.Levels.Users
                                });
                            }

                            if (guildconfig.AutoMessage.Any())
                            {
                                AutomessageList.Add(new AutoMessages
                                {
                                    GuildID = guildconfig.GuildId,
                                    Channels = guildconfig.AutoMessage
                                });
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }

                        if (guildconfig.PartnerSetup.banned) continue;
                        if (!guildconfig.PartnerSetup.IsPartner || !(_client.GetChannel(guildconfig.PartnerSetup.PartherChannel) is IMessageChannel)) continue;
                        Console.WriteLine($"{guild.Id} Added to Timers");
                        TimerService.AcceptedServers.Add(guild.Id);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
            }
            catch
            {
                //
            }

            _service.Restart();
            Console.WriteLine("DoOnce Completed.");
            DoOnce = true;
        }

        private async Task DoCommand(SocketMessage parameterMessage)
        {
            //Load.Messages++;
            if (!(parameterMessage is SocketUserMessage message)) return;
            var argPos = 0;
            var context = new SocketCommandContext(_client, message); //new CommandContext(_client, message);
            if (context.User.IsBot) return;
            Initialise();

            var guild = GuildConfig.GetServer(context.Guild);
            if (await CheckMessage(message, context, guild)) return;
            await AutoMessage(context);

            if (message.HasMentionPrefix(_client.CurrentUser, ref argPos) && guild.chatwithmention)
            {
                var mcontent = context.Message.MentionedUsers.Aggregate(message.Content,
                    (current, mentionedUser) => current.Replace(mentionedUser.Mention, mentionedUser.Username));
                mcontent = context.Message.MentionedRoles.Aggregate(mcontent,
                    (current, mentionedRole) => current.Replace(mentionedRole.Mention, mentionedRole.Name));
                mcontent = context.Message.MentionedChannels.Aggregate(mcontent,
                    (current, mentionedChannel) =>
                        current.Replace(((ITextChannel) mentionedChannel).Mention, mentionedChannel.Name));
                try
                {
                    var response = _apiAi.TextRequest(mcontent);
                    if (response.Result.Fulfillment.Speech != "")
                    {
                        await context.Channel.SendMessageAsync(response.Result.Fulfillment.Speech);
                    }
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

            var HS = Homeserver.Load();
            if (HS.GlobalBans.Any(x => x.ID == context.User.Id))
                return;


            var result = await _commands.ExecuteAsync(context, argPos, Provider);

            var server = context.Channel is IPrivateChannel ? "Direct Message " : context.Guild.Name;

            if (result.IsSuccess)
            {
                await LogHandler.In3($"{context.Message}", 'S', $"{server}", 'U', $"{context.User}"); //if there is no error log normally

                var srch = _commands.Search(context, argPos);
                if (srch.IsSuccess)
                {
                    var name = srch.Commands.Select(x => x.Command.Name).FirstOrDefault();
                    if (name != null)
                    {
                        if (CommandUses.Any(x => string.Equals(x.Name, name, StringComparison.CurrentCultureIgnoreCase)))
                        {
                            var cmd = CommandUses.First(x => string.Equals(x.Name, name, StringComparison.CurrentCultureIgnoreCase));
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
            else
            {
                try
                {
                    if (!(result.Error == CommandError.UnknownCommand || result.Error == CommandError.BadArgCount || result.Error == CommandError.UnmetPrecondition))
                    {
                        if (context.Client.GetChannel(HS.Error) is IMessageChannel ErrorChannel)
                        {
                            var embed = new EmbedBuilder
                            {
                                Title = $"ERROR: {context.Message}",
                                Description = "REASON:\n" +
                                              $"{result.ErrorReason}",
                                Color = Color.Red
                            };
                            embed.WithFooter(x => { x.Text = $"{context.Message.CreatedAt} || {context.Guild.Name}"; });
                            await ErrorChannel.SendMessageAsync("", false, embed.Build());
                        }
                    }
                }
                catch
                {
                    //
                }

                try
                {
                    //if in server error responses are enabled reply on error

                    var desc = "";
                    if (result.Error != CommandError.UnknownCommand)
                    {
                        var srch = _commands.Search(context, argPos);
                        var cmd = srch.Commands.FirstOrDefault();

                        desc += $"Command: {cmd.Command.Name}\n";
                        if (cmd.Command.Parameters.Any())
                        {
                            desc += $"Parameters: {string.Join(" ", cmd.Command.Parameters.Select(x => x.IsOptional ? $" `<(Optional){x.Name}>` " : $" `<{x.Name}>` "))}\n";
                        }
                        else
                        {
                            desc += "Parameters: N/A\n";
                        }

                        desc += $"Summary: `{cmd.Command?.Summary}`\n" +
                                $"Remarks: `{cmd.Command?.Remarks}`\n";

                        if (cmd.Command.Aliases.Any())
                        {
                            desc += $"Aliases: {string.Join(" ", cmd.Command.Aliases)}\n";
                        }

                        desc += "Error Reason:\n" +
                                $"{result.ErrorReason}";
                    }
                    else
                    {
                        desc += "Command: N/A";
                    }

                    var errmsg = new EmbedBuilder
                    {
                        Title = $"**Error, {context.User.Username}**",
                        Description = desc
                    };

                    try
                    {
                        await context.Channel.SendMessageAsync("", false, errmsg.Build());
                    }
                    catch
                    {
                        //
                    }

                    await LogHandler.In3Error($"{context.Message}", 'S', $"{context.Guild.Name}", 'E', $"{result.ErrorReason}"); // log errors as arrors
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        public class Con4GameList
        {
            public ulong ChannelID { get; set; }
            public bool Gamerunning { get; set; } = false;
        }

        public class CMD
        {
            public string Name { get; set; }
            public int Uses { get; set; }
        }


        public class LevellingObj
        {
            public ulong GuildID { get; set; }
            public List<GuildConfig.levelling.user> Users { get; set; } = new List<GuildConfig.levelling.user>();
            public int UpdatesSinceLastSave { get; set; }
        }

        public class AutoMessages
        {
            public ulong GuildID { get; set; }
            public List<GuildConfig.autochannels> Channels { get; set; } = new List<GuildConfig.autochannels>();
        }

        public class Delays
        {
            public DateTime _delay { get; set; } = DateTime.UtcNow;
            public ulong GuildID { get; set; }
        }

        public class SubReddit
        {
            public string Title { get; set; }
            public List<Post> Posts { get; set; }
            public DateTime LastUpdate { get; set; }
            public int Hits { get; set; } = 0;
        }
    }
}