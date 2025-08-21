using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NoMercyBot.Database;
using NoMercyBot.Globals.SystemCalls;
using NoMercyBot.Services.Twitch;
using NoMercyBot.Services.Interfaces;
using NoMercyBot.Services.Twitch.Scripting;

public class Command: ICommand
{
    public string Name => "followage";
    public CommandPermission Permission => CommandPermission.Everyone;

    public async Task Init(CommandScriptContext ctx)
    {
    }

    public async Task Callback(CommandScriptContext ctx)
    {
        var follow = await ctx.TwitchApiService.GetChannelFollower(ctx.Message.Broadcaster.Id, ctx.Message.User.Id);

        if (follow != null)
        {
            var followDuration = DateTimeOffset.UtcNow - follow.FollowedAt;
            var durationText = FormatDuration(followDuration);
            var text = $"@{ctx.Message.User.DisplayName} You have been following for {durationText}!";
            await ctx.TwitchChatService.SendReplyAsBot(ctx.Message.Broadcaster.Username, text, ctx.Message.Id);
        }
        else
        {
            var text = $"@{ctx.Message.User.DisplayName} You are not following!";
            await ctx.TwitchChatService.SendReplyAsBot(ctx.Message.Broadcaster.Username, text, ctx.Message.Id);
        }
    }
    
    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalDays >= 365)
        {
            var years = (int)(duration.TotalDays / 365);
            var remainingDays = (int)(duration.TotalDays % 365);
            return years == 1 
                ? $"{years} year" + (remainingDays > 0 ? $" and {remainingDays} days" : "")
                : $"{years} years" + (remainingDays > 0 ? $" and {remainingDays} days" : "");
        }
        else if (duration.TotalDays >= 30)
        {
            var months = (int)(duration.TotalDays / 30);
            var remainingDays = (int)(duration.TotalDays % 30);
            return months == 1 
                ? $"{months} month" + (remainingDays > 0 ? $" and {remainingDays} days" : "")
                : $"{months} months" + (remainingDays > 0 ? $" and {remainingDays} days" : "");
        }
        else if (duration.TotalDays >= 1)
        {
            var days = (int)duration.TotalDays;
            return days == 1 ? $"{days} day" : $"{days} days";
        }
        else if (duration.TotalHours >= 1)
        {
            var hours = (int)duration.TotalHours;
            return hours == 1 ? $"{hours} hour" : $"{hours} hours";
        }
        else
        {
            var minutes = Math.Max(1, (int)duration.TotalMinutes);
            return minutes == 1 ? $"{minutes} minute" : $"{minutes} minutes";
        }
    }
}

return new Command();