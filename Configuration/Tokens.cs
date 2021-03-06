﻿using System;
using System.IO;
using Newtonsoft.Json;

namespace PassiveBOT.Configuration
{
    public class Tokens
    {
        public string TwitchToken { get; set; } = null;
        public string DialogFlowToken { get; set; } = "";
        public string DiscordBotsListToken { get; set; } = null;
        public string DiscordBotsListUrl { get; set; } = null;
        public string FortniteToken { get; set; } = null;
        public string SupportServer { get; set; } = "https://discord.me/passive";
        public string PerspectiveAPI { get; set; } = null;

        public static Tokens Load()
        {
            var file = Path.Combine(AppContext.BaseDirectory, "setup/config/Tokens.json");
            return JsonConvert.DeserializeObject<Tokens>(File.ReadAllText(file));
        }

        public static void SaveTokens(Tokens config)
        {
            var file = Path.Combine(AppContext.BaseDirectory, "setup/config/Tokens.json");
            var output = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(file, output);
        }

        public static void CheckExistence()
        {
            if (!File.Exists(Path.Combine(AppContext.BaseDirectory, "setup/config/Tokens.json")))
            {
                var NewTokens = new Tokens();
                SaveTokens(NewTokens);
            }
        }
    }
}