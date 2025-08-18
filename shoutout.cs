using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NoMercyBot.Database;
using NoMercyBot.Database.Models;
using NoMercyBot.Database.Models.ChatMessage;
using NoMercyBot.Globals.SystemCalls;
using NoMercyBot.Services.Twitch;
using NoMercyBot.Services.Twitch.Scripting;
using NoMercyBot.Services.Interfaces;
using Serilog.Events;

public class Command: ICommand
{
    public string Name => "so";
    public CommandPermission Permission => CommandPermission.Moderator;

    private static readonly string[] _snarkyShoutoutReplies =
    {
        "Check out @{displayname}! {Subject} has some great {game} content. Go give {object} a follow! {Subject} {tense} practically a pro, or at least {Subject} play one on Twitch.",
        "Yo, peep this! @{displayname} {tense} rocking some {game} stuff. Go give {object} a follow! {Subject} {tense} so good, it's almost annoying.",
        "Attention, earthlings! @{displayname} has {game} videos you need to see. Go give {object} a follow! {Subject} {tense} probably putting on a masterclass, or a clown show – either way, it's entertaining.",
        "Incoming awesome! @{displayname} has some {game} action for you. Go give {object} a follow! {Subject} {tense} crushing it, or at least {Subject} looks like {Subject} is.",
        "Don't walk, run! @{displayname} has more {game} than you can handle. Go give {object} a follow! {Subject} {tense} definitely worth interrupting your snack for.",
        "Our resident legend, @{displayname}, has awesome {game}! Go give {object} a follow! {Subject} {tense} probably about to pull off something epic, or face-plant gloriously.",
        "Heads up, buttercups! @{displayname} has some {game} for you. Go give {object} a follow! {Subject} {tense} proving once again that {Subject} {tense} awesome (don't tell {object} I said that).",
        "Guess who's got content? @{displayname}! {Subject} {tense} rocking {game}. Go give {object} a follow! {Subject} {tense} bringing the vibes, whether {Subject} likes it or not.",
        "Behold! @{displayname} has some solid {game} for you. Go give {object} a follow! {Subject} {tense} gracing us with {object} presence and questionable decision-making in {game}."
    };

    public async Task Init(CommandScriptContext ctx)
    {
    }

    public async Task Callback(CommandScriptContext ctx)
    {
        if (ctx.Arguments.Length == 0)
        {
            await ctx.ChatService.SendReplyAsBot(ctx.Message.Broadcaster.Username, $"@{ctx.Message.User.DisplayName} You need to specify a user to shoutout!", ctx.Message.Id);
            return;
        }
        
        var name = ctx.Arguments[0].Replace("@", "").ToLower();

        try
        {
            var user = await ctx.DatabaseContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username.ToLower() == name);

            user ??= await ctx.TwitchApiService.FetchUser(login: name);

            if (user == null)
            {
                await ctx.ChatService.SendReplyAsBot(ctx.Message.Broadcaster.Username, $"@{ctx.Message.User.DisplayName} User '{name}' not found!", ctx.Message.Id);
                return;
            }

            var channelInfo = await ctx.DatabaseContext.ChannelInfo
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == user.Id);

            string gameName = "Something awesome";
            string title = "";
            bool isLive = false;

            if (channelInfo != null)
            {
                gameName = channelInfo.GameName ?? "something awesome";
                title = channelInfo.Title ?? "";
                isLive = channelInfo.IsLive;
            }
            else
            {
                var apiChannelInfo = await ctx.TwitchApiService.GetChannelInfo(user.Id);
                var streamInfo = await ctx.TwitchApiService.GetStreamInfo(user.Id);
                
                gameName = apiChannelInfo?.GameName ?? "something awesome";
                title = apiChannelInfo?.Title ?? "";
                isLive = streamInfo.Type == "live";
            }

            // Create modified context for template replacement
            var modifiedCtx = new CommandScriptContext
            {
                Message = new ChatMessage
                {
                    UserId = user.Id,
                    Username = user.Username,
                    DisplayName = user.DisplayName,
                    User = user,
                },
                Channel = ctx.Message.Broadcaster.Username,
                BroadcasterId = ctx.BroadcasterId,
                CommandName = ctx.CommandName,
                Arguments = ctx.Arguments,
                ReplyAsync = ctx.ReplyAsync,
                CancellationToken = ctx.CancellationToken,
                ServiceProvider = ctx.ServiceProvider,
                ChatService = ctx.ChatService,
                TwitchApiService = ctx.TwitchApiService,
                DatabaseContext = ctx.DatabaseContext, 
            };
            
            var channel = await ctx.DatabaseContext.Channels
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Name == name);

            var randomTemplate = channel?.ShoutoutTemplate ?? _snarkyShoutoutReplies[Random.Shared.Next(_snarkyShoutoutReplies.Length)];
            var text = TemplateHelper.ReplaceTemplatePlaceholders(randomTemplate, modifiedCtx, isLive, gameName, title);

            try
            {
                await ctx.TwitchApiService.SendAnnouncement(
                    ctx.Message.BroadcasterId, 
                    ctx.Message.BroadcasterId,
                    text);
            }
            catch (Exception e)
            {
                // Silently handle API errors - announcement was already sent
                Logger.Twitch($"Failed to send announcement for shoutout: {e.Message}", LogEventLevel.Error);
            }

            try
            {
                await ctx.TwitchApiService.SendShoutoutAsync(
                    ctx.Message.BroadcasterId, 
                    ctx.Message.BroadcasterId,
                    user.Id);
            }
            catch (Exception e)
            {
                // Silently handle API errors - announcement was already sent
                Logger.Twitch($"Failed to send shoutout for user {user.Username}: {e.Message}", LogEventLevel.Error);
            }
        }
        catch (Exception ex)
        {
            await ctx.ChatService.SendReplyAsBot(ctx.Message.Broadcaster.Username, $"@{ctx.Message.DisplayName} An error occurred while processing the shoutout.", ctx.Message.Id);
        }
    }
}

return new Command();