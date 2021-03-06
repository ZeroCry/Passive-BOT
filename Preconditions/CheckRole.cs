﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using PassiveBOT.Configuration;

namespace PassiveBOT.Preconditions
{
    /*
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class CheckDj : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command,
            IServiceProvider services)
        {
            if (context.Channel is IDMChannel) return Task.FromResult(PreconditionResult.FromSuccess());

            var id = context.Guild.Id;
            var role = GuildConfig.GetServer(context.Guild).DjRoleId;
            if (role == 0)
                return Task.FromResult(PreconditionResult.FromSuccess());

            if (((IGuildUser) context.User).RoleIds.Contains(role))
                return Task.FromResult(PreconditionResult.FromSuccess());

            return Task.FromResult(PreconditionResult.FromError("User is Not DJ"));
        }
    }
    */

    /*
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class CheckModerator : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command,
            IServiceProvider services)
        {
            if (context.Channel is IDMChannel) return Task.FromResult(PreconditionResult.FromSuccess());

            var own = context.Client.GetApplicationInfoAsync();
            if (own.Result.Owner.Id == context.User.Id)
                return Task.FromResult(PreconditionResult.FromSuccess());

            var role = GuildConfig.GetServer(context.Guild).ModeratorRoleId;
            if (role == 0)
            {
                if (((IGuildUser) context.User).GuildPermissions.Administrator)
                    return Task.FromResult(PreconditionResult.FromSuccess());
            }
            else
            {
                if (((IGuildUser) context.User).RoleIds.Contains(role))
                    return Task.FromResult(PreconditionResult.FromSuccess());
            }


            return Task.FromResult(PreconditionResult.FromError("User is Not A Moderator or an Admin!"));
        }
    }*/

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireModerator : PreconditionAttribute
    {
        private readonly bool _allowAdministrator;
        private readonly bool _allowAdministratorRole;

        public RequireModerator(bool AllowAdminPermission = true, bool allowAdministratorRole = true)
        {
            _allowAdministrator = AllowAdminPermission;
            _allowAdministratorRole = allowAdministratorRole;
        }

        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command,
            IServiceProvider services)
        {
            if (context.Channel is IDMChannel) return Task.FromResult(PreconditionResult.FromSuccess());

            var own = context.Client.GetApplicationInfoAsync();
            if (own.Result.Owner.Id == context.User.Id)
                return Task.FromResult(PreconditionResult.FromSuccess());

            var guser = (IGuildUser) context.User;
            var guild = GuildConfig.GetServer(context.Guild);
            if (_allowAdministrator && guser.GuildPermissions.Administrator)
                return Task.FromResult(PreconditionResult.FromSuccess());

            if (_allowAdministratorRole && guild.RoleConfigurations.AdminRoleList.Any(x => guser.RoleIds.Contains(x)))
                return Task.FromResult(PreconditionResult.FromSuccess());

            if (guild.RoleConfigurations.ModeratorRoleList.Any(x => guser.RoleIds.Contains(x)))
                return Task.FromResult(PreconditionResult.FromSuccess());

            return Task.FromResult(PreconditionResult.FromError("User is Not A Moderator or an Admin!"));
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireAdmin : PreconditionAttribute
    {
        private readonly bool _allowAdministrator;

        public RequireAdmin(bool AllowAdminPermission = true)
        {
            _allowAdministrator = AllowAdminPermission;
        }

        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command,
            IServiceProvider services)
        {
            if (context.Channel is IDMChannel) return Task.FromResult(PreconditionResult.FromSuccess());

            var own = context.Client.GetApplicationInfoAsync();
            if (own.Result.Owner.Id == context.User.Id)
                return Task.FromResult(PreconditionResult.FromSuccess());

            var guser = (IGuildUser) context.User;
            var guild = GuildConfig.GetServer(context.Guild);
            if (_allowAdministrator && guser.GuildPermissions.Administrator)
                return Task.FromResult(PreconditionResult.FromSuccess());


            if (guild.RoleConfigurations.AdminRoleList.Any(x => guser.RoleIds.Contains(x)))
                return Task.FromResult(PreconditionResult.FromSuccess());

            return Task.FromResult(PreconditionResult.FromError("User does not have an Administrator role!"));
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class BotModerator : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command,
            IServiceProvider services)
        {
            if (context.Channel is IDMChannel) return Task.FromResult(PreconditionResult.FromSuccess());

            var own = context.Client.GetApplicationInfoAsync();
            if (own.Result.Owner.Id == context.User.Id)
                return Task.FromResult(PreconditionResult.FromSuccess());

            var role = Homeserver.Load().BotModerator;
            if (role == 0) return Task.FromResult(PreconditionResult.FromError("User is not a BOT Moderator"));

            if (((IGuildUser) context.User).RoleIds.Contains(role))
                return Task.FromResult(PreconditionResult.FromSuccess());


            return Task.FromResult(PreconditionResult.FromError("User is not BOT Moderator"));
        }
    }
}