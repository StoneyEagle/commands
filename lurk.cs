using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NoMercyBot.Database.Models;
using NoMercyBot.Database;
using NoMercyBot.Globals.SystemCalls;
using NoMercyBot.Services.Twitch;
using NoMercyBot.Services.Twitch.Scripting;
using NoMercyBot.Services.Interfaces;
using NoMercyBot.Globals.NewtonSoftConverters;

public class Command: ICommand
{
    public string Name => "lurk";
    public CommandPermission Permission => CommandPermission.Everyone;

    private static Storage? _lurkerStorage;

    private static readonly string[] _snarkyLurkReplies = 
    {
        "Oh, @{name} is going to !lurk? Don't strain yourself with all that... not chatting. We'll try to have fun without you.",
        "@{name} has bravely entered the lurk zone. We'll miss your... well, you know. Your active participation. Maybe.",
        "Another one bites the dust! @{name} is now a professional lurker. Enjoy your silent observations!",
        "Farewell, @{name}! May your lurk be ever watchful and your keyboard ever silent. We'll save you a pixelated seat.",
        "@{name} is off to their top-secret lurking mission. Don't worry, we'll try not to have *too* much fun without you.",
        "And just like that, @{name} vanishes into the shadows of !lurk. Try not to get too comfortable back there!",
        "It's true, @{name} is now officially in stealth mode. We appreciate your dedication to... being here, but not really.",
        "Well, look at @{name}, pulling a Houdini with the !lurk command. Don't forget to blink once in a while!",
        "Lurk initiated for @{name}. We'll assume you're busy with super important lurker business. Don't mind us!",
        "Confirmed: @{name} has successfully executed !lurk. Your silence is now deafening. Just kidding... mostly."
    };

    private static readonly string[] _alreadyLurkingReplies = 
    {
        "@{name}, you're trying to lurk again? We thought you were already in the shadows! Did you forget to bring snacks?",
        "Wait, @{name}, you're still here? And trying to lurk? Get lost! (Just kidding... mostly).",
        "@{name} is attempting to lurk... *again*. Didn't you already have your vanishing act? Go on, shoo!",
        "Are you new to this, @{name}? You're already lurking! The 'disappear' button only works once. Now scram!",
        "Uh, @{name}? You just tried to lurk, but you've been a ghost for ages. Did you briefly consider rejoining chat?",
        "@{name}, you can't lurk if you're already successfully lurking. Get back to your silent duties!",
        "Is this a joke, @{name}? You're already lurking. Don't make me send you to the *super* lurk zone.",
        "Someone tell @{name} the lurk command isn't a continuous loop. We already wrote you off! (In a loving way, of course).",
        "@{name} is trying to double-lurk. Impressive, but unnecessary. You're already invisible to us!",
        "Ah, @{name}, back for round two of lurking? You never truly left our hearts... or our 'currently lurking' list."
    };

    public async Task Init(CommandScriptContext ctx)
    {
        _lurkerStorage = await ctx.DatabaseContext.Storages
            .Where(p => p.Key == "LurkersList")
            .FirstOrDefaultAsync(ctx.CancellationToken);
        
        if (_lurkerStorage == null)
        {
            var newLurkerStorage = new Storage
            {
                Key = "LurkersList",
                Value = "[]"
            };
            
            ctx.DatabaseContext.Storages.Add(newLurkerStorage);
            await ctx.DatabaseContext.SaveChangesAsync();
        }
    }

    public async Task Callback(CommandScriptContext ctx)
    {
        List<string> _lurkers = await ctx.DatabaseContext.Storages
            .AsNoTracking()
            .Where(p => p.Key == "LurkersList")
            .Select(p => p.Value.FromJson<List<string>>())
            .FirstOrDefaultAsync(ctx.CancellationToken);
        
        try
        {
            var username = ctx.Message.User.Username;

            if (_lurkers.Contains(username))
            {
                var randomTemplate = _alreadyLurkingReplies[Random.Shared.Next(_alreadyLurkingReplies.Length)];
                var text = TemplateHelper.ReplaceTemplatePlaceholders(randomTemplate, ctx);
                await ctx.ChatService.SendReplyAsBot(ctx.Message.Broadcaster.Username, text, ctx.Message.Id);
                return;
            }

            _lurkers.Add(username);
            
            _lurkerStorage.Value = _lurkers.ToJson();
            Logger.App(_lurkerStorage.Value);
            await ctx.DatabaseContext.SaveChangesAsync();

            var randomLurkTemplate = _snarkyLurkReplies[Random.Shared.Next(_snarkyLurkReplies.Length)];
            var lurkText = TemplateHelper.ReplaceTemplatePlaceholders(randomLurkTemplate, ctx);
            await ctx.ChatService.SendReplyAsBot(ctx.Message.Broadcaster.Username, lurkText, ctx.Message.Id);
        }
        catch (Exception ex)
        {
            await ctx.ChatService.SendReplyAsBot(ctx.Message.Broadcaster.Username, $"@{ctx.Message.User.DisplayName} This is the lurk command", ctx.Message.Id);
        }
    }
}

return new Command();