﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Newtonsoft.Json;
using PassiveBOT.Configuration;
using PassiveBOT.Services;

namespace PassiveBOT.Commands
{
    [RequireUserPermission(GuildPermission.Administrator)]
    [RequireContext(ContextType.Guild)]
    public class GuildSetup : InteractiveBase
    {
        private readonly RssService _rss;

        public GuildSetup(RssService rss)
        {
            _rss = rss;
        }


        [Command("Setup", RunMode = RunMode.Async)]
        [Summary("Setup")]
        [Remarks("Initialises the servers configuration file")]
        public async Task Setup()
        {
            await ReplyAsync("```\n" +
                             "Reply with the command you would like to perform\n" +
                             "[1] Initialise the config file\n" +
                             "[2] Read the config file\n" +
                             "[3] Delete the config file\n" +
                             "" +
                             "```");
            var n = await NextMessageAsync();
            if (n.Content == "1")
            {
                GuildConfig.Setup(Context.Guild);
                await ConfigInfo();
            }
            else if (n.Content == "2")
            {
                await ConfigInfo();
            }
            else if (n.Content == "3")
            {
                var file = Path.Combine(AppContext.BaseDirectory, $"setup/server/{Context.Guild.Id}/config.json");
                if (File.Exists(file))
                {
                    File.Delete(file);
                    await ReplyAsync("The config file has been deleted.");
                }
                else
                {
                    await ReplyAsync($"The config file does not exist, please use option 1 to initialise it");
                }
            }
            else
            {
                await ReplyAsync("Please only reply with the character of your option ie. if you picked 2, reply with just `2`");
            }
        }

        

        public async Task ConfigInfo()
        {
            var embed = new EmbedBuilder();
            var l = GuildConfig.Load(Context.Guild.Id);

            try
            {
                embed.AddField("DJ Role", $"Role: {Context.Guild.GetRole(l.DjRoleId).Name}");
            }
            catch
            {
                //
            }
            try
            {
                embed.AddField("Error logging", $"Status: {l.ErrorLog}");
            }
            catch
            {
                //
            }
            try
            {
                embed.AddField("Guild ID & Name", $"{l.GuildName}, {l.GuildId}");
            }
            catch
            {
                //
            }
            try
            {
                var list = "";
                foreach (var role in l.Roles)
                {
                    list += Context.Guild.GetRole(role).Name;
                }
                embed.AddField("SubRoles", $"Role: {list}");
            }
            catch
            {
                //
            }
            try
            {
                embed.AddField("RSS URL/Channel", $"{l.Rss}, {Context.Guild.GetChannel(l.RssChannel)}");
            }
            catch
            {
                //
            }
            try
            {
                var dict = GuildConfig.Load(Context.Guild.Id).Dict;
                var list = "";
                foreach (var tagging in dict)
                    list += $"{tagging.Tagname}, ";

                var res = list.Substring(0, list.Length - 2);
                embed.AddField("Tags", $"Tags: {res}");
            }
            catch
            {
                //
            }
            try
            {
                embed.AddField("Welcome", $"Status: {l.WelcomeEvent}\n" +
                                          $"Channel: {Context.Guild.GetChannel(l.WelcomeChannel).Name}\n" +
                                          $"Message: {l.WelcomeMessage}");
            }
            catch
            {
                //
            }
            try
            {
                embed.AddField("NoMention", $"Status: {l.MentionAll}");
            }
            catch
            {
                //
            }


            await ReplyAsync("", false, embed.Build());
        }

        [Command("Welcome")]
        [Summary("Welcome <message>")]
        [Remarks("Sets the welcome message for new users in the server")]
        public async Task Welcome([Remainder] string message)
        {
            GuildConfig.SetWMessage(Context.Guild.Id, message);
            GuildConfig.SetWChannel(Context.Guild.Id, Context.Channel.Id);
            await ReplyAsync("The Welcome Message for this server has been set to:\n" +
                             $"**{message}**\n" +
                             $"In the channel **{Context.Channel.Name}**");
        }

        [Command("WelcomeChannel")]
        [Alias("wc")]
        [Summary("wc")]
        [Remarks("Sets the current channel as the welcome channel")]
        public async Task Wchannel()
        {
            GuildConfig.SetWChannel(Context.Guild.Id, Context.Channel.Id);
            await ReplyAsync("The Welcome Channel for this server has been set to:\n" +
                             $"**{Context.Channel.Name}**");
        }

        [Command("WelcomeStatus")]
        [Alias("ws")]
        [Summary("ws <true/false>")]
        [Remarks("sets the welcome message as true or false (on/off)")]
        public async Task WOff(bool status)
        {
            GuildConfig.SetWelcomeStatus(Context.Guild.Id, status);

            await ReplyAsync($"Welcome Messageing for this server has been set to: {status}");
        }

        [Command("NoInvite")]
        [Summary("NoInvite <true/false>")]
        [Remarks("disables/enables the sending of invites in a server from regular members")]
        public async Task NoInvite(bool status)
        {
            var file = Path.Combine(AppContext.BaseDirectory, $"setup/server/{Context.Guild.Id}/config.json");
            if (File.Exists(file))
            {
                var jsonObj = JsonConvert.DeserializeObject<GuildConfig>(File.ReadAllText(file));
                jsonObj.Invite = status;
                var output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
                File.WriteAllText(file, output);

                if (status)
                {
                    
                    await ReplyAsync("Invite links will now be deleted!");
                }
                else
                {
                    await ReplyAsync("Invite links are now allowed to be sent");
                }
            }
            else
            {
                await ReplyAsync($"The config file does not exist, please type `{Load.Pre}setup` to initialise it");
            }
        }

        [Command("NoMention")]
        [Summary("NoMention <true/false>")]
        [Remarks("disables/enables the use of @ everyone and @ here in a server from regular members")]
        public async Task NoMention(bool status)
        {
            var file = Path.Combine(AppContext.BaseDirectory, $"setup/server/{Context.Guild.Id}/config.json");
            if (File.Exists(file))
            {
                var jsonObj = JsonConvert.DeserializeObject<GuildConfig>(File.ReadAllText(file));
                jsonObj.MentionAll = status;
                var output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
                File.WriteAllText(file, output);

                if (status)
                {

                    await ReplyAsync("Mass Mentions will now be deleted!");
                }
                else
                {
                    await ReplyAsync("Mass Mentions are now allowed to be sent");
                }
            }
            else
            {
                await ReplyAsync($"The config file does not exist, please type `{Load.Pre}setup` to initialise it");
            }
        }

        [Command("SetDj")]
        [Summary("SetDj <@role>")]
        [Remarks("Sets the DJ role")]
        public async Task Dj([Remainder] IRole role)
        {
            GuildConfig.SetDj(Context.Guild.Id, role.Id);
            await ReplyAsync($"The DJ Role has been set to: {role.Name}");
        }

        [Command("Errors")]
        [Summary("Errors <true/false>")]
        [Remarks("Toggles Error Status")]
        public async Task Errors(bool status)
        {
            GuildConfig.SetError(Context.Guild.Id, status);
            if (status)
                await ReplyAsync("Errors will now be Logged");
            else
                await ReplyAsync("Errors will no longer be logged");
        }

        [Group("blacklist")]
        public class Blacklist : InteractiveBase
        {
            [Command]
            public async Task B()
            {
                var file = Path.Combine(AppContext.BaseDirectory, $"setup/server/{Context.Guild.Id}/config.json");
                if (File.Exists(file))
                {
                    var jsonObj = JsonConvert.DeserializeObject<GuildConfig>(File.ReadAllText(file));
                    if (jsonObj.Blacklist == null)
                    {
                        jsonObj.Blacklist = new List<string>();
                    }
                    var embed = new EmbedBuilder();
                    var blackl = "";
                    foreach (var word in jsonObj.Blacklist)
                    {
                        blackl += $"{word} \n";
                    }
                    try
                    {
                        embed.AddField("Blacklisted Words", blackl);
                    }
                    catch
                    {
                        //
                    }
                    embed.AddField("Timeout", "This message self destructs after 5 seconds.");

                    await ReplyAndDeleteAsync("", false, embed.Build(), TimeSpan.FromSeconds(5));

                }
                else
                {
                    await ReplyAsync($"The config file does not exist, please type `{Load.Pre}setup` to initialise it");
                }
            }

            [Command("add")]
            [Summary("add <word>")]
            [Remarks("adds a word to the blacklist")]
            public async Task Ab(string keyword)
            {
                var file = Path.Combine(AppContext.BaseDirectory, $"setup/server/{Context.Guild.Id}/config.json");
                if (File.Exists(file))
                {
                    var jsonObj = JsonConvert.DeserializeObject<GuildConfig>(File.ReadAllText(file));
                    if (jsonObj.Blacklist == null)
                    {
                        jsonObj.Blacklist = new List<string>();
                    }
                    if (!jsonObj.Blacklist.Contains(keyword))
                    {
                        jsonObj.Blacklist.Add(keyword);
                        await Context.Message.DeleteAsync();
                        await ReplyAsync("Added to the Blacklist");
                    }
                    else
                    {
                        await Context.Message.DeleteAsync();
                        await ReplyAsync("Keyword is already in the blacklist");
                        return;
                    }

                    var output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
                    File.WriteAllText(file, output);
                }
                else
                {
                    await ReplyAsync($"The config file does not exist, please type `{Load.Pre}setup` to initialise it");
                }
            }

            [Command("del")]
            [Summary("del <word>")]
            [Remarks("removes a word from the blacklist")]
            public async Task Db(string keyword)
            {
                var file = Path.Combine(AppContext.BaseDirectory, $"setup/server/{Context.Guild.Id}/config.json");
                if (File.Exists(file))
                {
                    var jsonObj = JsonConvert.DeserializeObject<GuildConfig>(File.ReadAllText(file));

                    if (jsonObj.Blacklist == null)
                    {
                        jsonObj.Blacklist = new List<string>();
                    }

                    if (jsonObj.Blacklist.Contains(keyword))
                    {
                        jsonObj.Blacklist.Remove(keyword);
                        await ReplyAsync($"{keyword} is has been removed from the blacklist");
                    }
                    else
                    {
                        await ReplyAsync($"{keyword} is not in the blacklist");
                        return;
                    }

                    var output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
                    File.WriteAllText(file, output);
                }
                else
                {
                    await ReplyAsync($"The config file does not exist, please type `{Load.Pre}setup` to initialise it");
                }
            }

            [Command("clear")]
            [Summary("clear")]
            [Remarks("clears the blacklist")]
            public async Task Clear()
            {
                var file = Path.Combine(AppContext.BaseDirectory, $"setup/server/{Context.Guild.Id}/config.json");
                if (File.Exists(file))
                {
                    var jsonObj = JsonConvert.DeserializeObject<GuildConfig>(File.ReadAllText(file));
                    jsonObj.Blacklist = new List<string>();

                    var output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
                    File.WriteAllText(file, output);

                    await ReplyAsync("The blacklist has been cleared.");
                }
                else
                {
                    await ReplyAsync($"The config file does not exist, please type `{Load.Pre}setup` to initialise it");
                }
            }
        }
        [Command("addrole")]
        [Summary("addrole @role")]
        [Remarks("adds a subscribable role")]
        public async Task Arole(IRole role)
        {
            var file = Path.Combine(AppContext.BaseDirectory, $"setup/server/{Context.Guild.Id}/config.json");
            if (File.Exists(file))
            {
                var jsonObj = JsonConvert.DeserializeObject<GuildConfig>(File.ReadAllText(file));
                if (jsonObj.Roles == null)
                {
                    jsonObj.Roles = new List<ulong>();
                }
                if (!jsonObj.Roles.Contains(role.Id))
                {
                    jsonObj.Roles.Add(role.Id);
                    await ReplyAsync($"{role.Name} has been added to the subscribable roles list");
                }
                else
                {
                    await ReplyAsync($"{role.Name} is already subscribable");
                    return;
                }

                var output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
                File.WriteAllText(file, output);
            }
            else
            {
                await ReplyAsync($"The config file does not exist, please type `{Load.Pre}setup` to initialise it");
            }
        }

        [Command("delrole")]
        [Summary("delrole @role")]
        [Remarks("removes the subscribable role")]
        public async Task Drole(IRole role)
        {
            var file = Path.Combine(AppContext.BaseDirectory, $"setup/server/{Context.Guild.Id}/config.json");
            if (File.Exists(file))
            {
                var jsonObj = JsonConvert.DeserializeObject<GuildConfig>(File.ReadAllText(file));

                if (jsonObj.Roles.Contains(role.Id))
                {
                    jsonObj.Roles.Remove(role.Id);
                    await ReplyAsync($"{role.Name} is has been removed from the subscribable roles list");
                }
                else
                {
                    await ReplyAsync($"{role.Name} is not a subscribable role");
                    return;
                }

                var output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
                File.WriteAllText(file, output);
            }
            else
            {
                await ReplyAsync($"The config file does not exist, please type `{Load.Pre}setup` to initialise it");
            }
        }

        [Command("rss", RunMode = RunMode.Async)]
        [Summary("rss <feed url>")]
        [Remarks("adds an rss feed")]
        public async Task Rss(string url1 = null)
        {
            if (url1 != null)
            {
                var file = Path.Combine(AppContext.BaseDirectory, $"setup/server/{Context.Guild.Id}/config.json");
                if (File.Exists(file))
                {
                    GuildConfig.RssSet(Context.Guild.Id, Context.Channel.Id, url1, true);
                    await ReplyAsync("Rss Config has been updated!\n" +
                                     $"Updates will be posted in: {Context.Channel.Name}\n" +
                                     $"Url: {url1}");
                }
                else
                {
                    await ReplyAsync($"The config file does not exist, please type `{Load.Pre}setup` to initialise it");
                }

                await _rss.Rss(url1, Context.Channel as IGuildChannel);
            }
            else
            {
                await ReplyAsync("The RSS Feed has been removed (null input)");
                await _rss.Rss(null, Context.Channel as IGuildChannel);
            }
        }
    }
}