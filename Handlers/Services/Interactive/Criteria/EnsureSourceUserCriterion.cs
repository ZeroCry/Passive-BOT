﻿using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace PassiveBOT.Handlers.Services.Interactive.Criteria
{
    public class EnsureSourceUserCriterion : ICriterion<SocketMessage>
    {
        public Task<bool> JudgeAsync(SocketCommandContext sourceContext, SocketMessage parameter)
        {
            var ok = sourceContext.User.Id == parameter.Author.Id;
            return Task.FromResult(ok);
        }
    }
}