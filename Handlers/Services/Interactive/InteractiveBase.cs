﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using PassiveBOT.Handlers.Services.Interactive.Criteria;
using PassiveBOT.Handlers.Services.Interactive.Paginator;
using PassiveBOT.Handlers.Services.Interactive.Results;

namespace PassiveBOT.Handlers.Services.Interactive
{
    public class InteractiveBase : InteractiveBase<SocketCommandContext>
    {
    }

    public class InteractiveBase<T> : ModuleBase<T>
        where T : SocketCommandContext
    {
        public InteractiveService Interactive { get; set; }

        public Task<SocketMessage> NextMessageAsync(ICriterion<SocketMessage> criterion, TimeSpan? timeout = null)
        {
            return Interactive.NextMessageAsync(Context, criterion, timeout);
        }

        public Task<SocketMessage> NextMessageAsync(bool fromSourceUser = true, bool inSourceChannel = true,
            TimeSpan? timeout = null)
        {
            return Interactive.NextMessageAsync(Context, fromSourceUser, inSourceChannel, timeout);
        }

        public Task<IUserMessage> ReplyAndDeleteAsync(string content, bool isTTS = false, Embed embed = null,
            TimeSpan? timeout = null, RequestOptions options = null)
        {
            return Interactive.ReplyAndDeleteAsync(Context, content, isTTS, embed, timeout, options);
        }

        public Task<IUserMessage> PagedReplyAsync(IEnumerable<PaginatedMessage.Page> pages, bool fromSourceUser = true)
        {
            var pager = new PaginatedMessage
            {
                Pages = pages
            };
            return PagedReplyAsync(pager, fromSourceUser);
        }

        public Task<IUserMessage> PagedReplyAsync(PaginatedMessage pager, bool fromSourceUser = true,
            bool showall = false, bool showindex = false)
        {
            var criterion = new Criteria<SocketReaction>();
            if (fromSourceUser)
                criterion.AddCriterion(new EnsureReactionFromSourceUserCriterion());
            return PagedReplyAsync(pager, criterion, showall, showindex);
        }

        public Task<IUserMessage> PagedReplyAsync(PaginatedMessage pager, ICriterion<SocketReaction> criterion,
            bool showall = false, bool showindex = false)
        {
            return Interactive.SendPaginatedMessageAsync(Context, pager, criterion, showall, showindex);
        }

        public RuntimeResult Ok(string reason = null)
        {
            return new OkResult(reason);
        }
    }
}