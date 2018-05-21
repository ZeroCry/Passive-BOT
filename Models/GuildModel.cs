﻿using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace PassiveBOT.Models
{
    public class GuildModel
    {
        /// <summary>
        /// The Server ID
        /// </summary>
        public ulong ID { get; set; }

        /// <summary>
        /// General Setup of the Server
        /// </summary>
        public gsettings Settings { get; set; } = new gsettings();

        /// <summary>
        /// Partner Program Setup
        /// </summary>
        public partner Partner { get; set; } = new partner();

        /// <summary>
        /// Levels Setup, userdata and rewards
        /// </summary>
        public levelling Levels { get; set; } = new levelling();

        /// <summary>
        /// Disabled Commands and Modules
        /// </summary>
        public hidden Disabled { get; set; } = new hidden();

        /// <summary>
        /// Channel Auto-Messaging
        /// </summary>
        public autoMessage AutoMessage { get; set; } = new autoMessage();

        /// <summary>
        /// Join and Leave Events
        /// </summary>
        public events Events { get; set; } = new events();

        public class autoMessage
        {
            /// <summary>
            /// A List of Channels that use Auto Messaging
            /// </summary>
            public List<amChannel> AutoMessageChannels { get; set; } = new List<amChannel>();
            public class amChannel
            {
                /// <summary>
                /// The Channel ID that we will send an automessage to
                /// </summary>
                public ulong ChannelID { get; set; }
                /// <summary>
                /// True = send messages
                /// False = Do not Send Messages
                /// </summary>
                public bool Enabled { get; set; } = false;

                /// <summary>
                /// The Amount of messages sent since last AutoMessage
                /// </summary>
                public int Count { get; set; } = 0;

                /// <summary>
                /// The Amount of messages required before next AutoMessage
                /// </summary>
                public int Limit { get; set; } = 100;
            }
        }

        public class hidden
        {
            /// <summary>
            /// List of Modules that have been configured
            /// </summary>
            public List<hiddentype> Modules { get; set; } = new List<hiddentype>();

            /// <summary>
            /// List of Commands that have been configured
            /// </summary>
            public List<hiddentype> Commands { get; set; } = new List<hiddentype>();
            public class hiddentype
            {
                /// <summary>
                /// Name of configured item
                /// </summary>
                public string Name { get; set; }

                /// <summary>
                /// WhiteListed Role IDs
                /// </summary>
                public List<ulong> WhiteList { get; set; } = new List<ulong>();

                /// <summary>
                /// Blacklisted Role IDs
                /// </summary>
                public List<ulong> Blacklist { get; set; } = new List<ulong>();

                /// <summary>
                /// True = No Access unless server owner or whitelisted
                /// False = Full Access unless blacklisted
                /// </summary>
                public bool Disabled { get; set; } = false;
            }
        }

        public class events
        {
            /// <summary>
            /// Welcome Event
            /// </summary>
            public _event Welcome { get; set; } = new _event
            {
                Message = "Has Joined the Server!"
            };
            /// <summary>
            /// GoodBye Event
            /// </summary>
            public _event Goodbye { get; set; } = new _event
            {
                Message = "Has Left the Server!"
            };
            public class _event
            {
                /// <summary>
                /// The channel the Event message will be sent to
                /// </summary>
                public ulong ChannelID { get; set; }

                /// <summary>
                /// The Event Message
                /// </summary>
                public string Message { get; set; }

                /// <summary>
                /// True = Send Event Messages
                /// False = Do not send event Messages
                /// </summary>
                public bool Enabled { get; set; } = false;
            }
        }

        public class levelling
        {
            /// <summary>
            /// Levelling Settings and setup
            /// </summary>
            public lsettings Settings { get; set; } = new lsettings();
            public class lsettings
            {
                /// <summary>
                /// True = Use Levelling
                /// False = Levelling is disabled
                /// </summary>
                public bool Enabled { get; set; } = false;

                /// <summary>
                /// True = Log all Level up messages in a specific channel
                /// </summary>
                public bool UseLogChannel { get; set; } = false;

                /// <summary>
                /// Channel ID used for level logging
                /// </summary>
                public ulong LogChannelID { get; set; }

                /// <summary>
                /// True = Send users a private message when they level up
                /// </summary>
                public bool DMLevelUps { get; set; } = false;

                /// <summary>
                /// Reply in the current channel with level up message
                /// </summary>
                public bool ReplyLevelUps { get; set; } = true;
            }

            /// <summary>
            /// Level Up Rewards
            /// </summary>
            public List<levelreward> RewardRoles { get; set; } = new List<levelreward>();
            public class levelreward
            {
                /// <summary>
                /// Level Requirement to receive reward
                /// </summary>
                public int Requirement { get; set; }

                /// <summary>
                /// ID of role to receive for level up
                /// </summary>
                public ulong RoleID { get; set; }
            }

            /// <summary>
            /// List of all users that have been initialised in levelling
            /// </summary>
            public List<luser> Users { get; set; } = new List<luser>();
            public class luser
            {
                /// <summary>
                /// User ID
                /// </summary>
                public ulong UserID { get; set; }

                /// <summary>
                /// User Level
                /// </summary>
                public int Level { get; set; } = 1;

                /// <summary>
                /// User total XP
                /// </summary>
                public int XP { get; set; } = 0;
            }
        }

        public class partner
        {
            /// <summary>
            /// Partner Settings
            /// </summary>
            public psettings Settings { get; set; } = new psettings();

            /// <summary>
            /// Partner Message
            /// </summary>
            public message Message { get; set; } = new message();
            public class psettings
            {
                /// <summary>
                /// Bot admin Toggled, True = Partner Messages will no longer be sent at all.
                /// </summary>
                public bool Banned { get; set; } = false;

                /// <summary>
                /// True = Using Partner Program
                /// </summary>
                public bool Enabled { get; set; } = false;

                /// <summary>
                /// ID Of partner message channel, where messages will be sent to
                /// </summary>
                public ulong ChannelID { get; set; }
            }
            public class message
            {
                /// <summary>
                /// Main Text body of partner message
                /// </summary>
                public string Content { get; set; } = null;

                /// <summary>
                /// Optional Image for Partner message
                /// </summary>
                public string ImageUrl { get; set; } = null;

                /// <summary>
                /// Toggle whether or not to show server user count in partner message
                /// </summary>
                public bool UserCount { get; set; } = false;

                /// <summary>
                /// Toggle whether or not to show server icon in partner message
                /// </summary>
                public bool ThumbNail { get; set; } = false;
            }
        }

        public class gsettings
        {
            /// <summary>
            /// Bot Custom Prefix config
            /// </summary>
            public prefix Prefix { get; set; } = new prefix();
            public class prefix
            {
                /// <summary>
                /// Deny the @BOTNAME prefix for commands
                /// </summary>
                public bool DenyMentionPrefix { get; set; } = false;

                /// <summary>
                /// Deny the default bot prefix in commands
                /// </summary>
                public bool DenyDefaultPrefix { get; set; } = false;

                /// <summary>
                /// Server's Own cutom prefix for bot commands
                /// </summary>
                public string CustomPrefix { get; set; } = null;
            }
        }
    }
}