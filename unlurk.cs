using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NoMercyBot.Database.Models;
using NoMercyBot.Database;
using NoMercyBot.Globals.SystemCalls;
using NoMercyBot.Services.Interfaces;
using NoMercyBot.Services.Twitch;
using NoMercyBot.Services.Twitch.Scripting;
using NoMercyBot.Globals.NewtonSoftConverters;

public class Command: ICommand
{ 
   public string Name =>  "unlurk";
   public CommandPermission Permission => CommandPermission.Everyone;

   private static Storage? _lurkerStorage;

   private static readonly string[] _snarkyReplies =
   {
       "Look alive, everyone! @{name} has emerged from the lurk zone. We almost forgot about you!",
       "Well, well, well, if it isn't @{name}, gracing us with their active presence once more. Did you miss us?",
       "The legend, @{name}, has returned! We've been holding your pixelated seat. Don't touch anything, it's still warm.",
       "Breaking news: @{name} has successfully completed their top-secret lurker mission. Welcome back to the land of the living!",
       "@{name} has bravely decided to rejoin the chaos. We're shocked, honestly. What took you so long?",
       "Hark! Is that... chatter? It is! @{name} has officially unlurked. Your silence was deafening, just sayin'.",
       "Welcome back, @{name}! We hope your lurking was productive. Now get to work, there's chat to be had!",
       "@{name} has resurfaced! Did you bring snacks from the shadows? No? Aw, well, welcome back anyway.",
       "The prophecy is true! @{name} has shed their lurker skin. Prepare for... well, whatever you do when you're not lurking.",
       "It's true, @{name} is no longer a ghost in the machine. Your keyboard must be so lonely no more. Welcome back!",
       "Hold the phone! @{name} has decided to grace us with their voice again. We thought you'd joined a silent monastery!",
       "A wild @{name} appeared! {Subject} used !unlurk. It was super effective. Welcome back, we guess.",
       "Did anyone else hear that? Oh, it's just @{name} finally rejoining the chat. The lurk spell has been broken!",
       "Look what the cat dragged in! It's @{name}, back from the digital wilderness. Don't worry, we saved you some crumbs.",
       "Well, hello there, @{name}! Decided to abandon your lurking duties, have we? Good to see your pixels again."
   };

   private static readonly string[] _notLurkingUnlurkReplies =
   {
       "@{name}, you can't unlurk if you weren't even lurking! Were you trying to escape from something else?",
       "Did we miss something, @{name}? You just tried to unlurk, but we didn't even know you were gone. What's your secret?",
       "@{name} just used unlurk. My dude, you were never in the lurk zone to begin with! Are you okay?",
       "Hold on, @{name}. unlurk? Were you secretly lurking under a rock this whole time? We definitely saw you chatting!",
       "Is this a magic trick, @{name}? You can't unlurk from a state you weren't in! Stay hydrated, buddy.",
       "@{name} is trying to reverse lurk. Fascinating. But you haven't been lurking! Get back to your active chat duties!",
       "Uh, @{name}? Your unlurk command seems to have glitched. You've been here the whole time! Try again when you actually vanish.",
       "Welcome back from... not lurking, @{name}! We're thrilled you're still here, even if your command is confused.",
       "@{name} just attempted an unlurk. Newsflash: You've been chatting away! No escape for you!",
       "My systems indicate @{name} has always been here. No need to unlurk from the land of the actively chatting. What gives?"
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

           if (!_lurkers.Contains(username))
           {
               var randomTemplate = _notLurkingUnlurkReplies[Random.Shared.Next(_notLurkingUnlurkReplies.Length)];
               var text = TemplateHelper.ReplaceTemplatePlaceholders(randomTemplate, ctx);
               await ctx.ChatService.SendReplyAsBot(ctx.Message.Broadcaster.Username, text, ctx.Message.Id);
               return;
           }

           _lurkers.Remove(username);
           _lurkerStorage.Value = _lurkers.ToJson();
           ctx.DatabaseContext.Storages.Update(_lurkerStorage);
           await ctx.DatabaseContext.SaveChangesAsync();

           var randomUnlurkTemplate = _snarkyReplies[Random.Shared.Next(_snarkyReplies.Length)];
           var unlurkText = TemplateHelper.ReplaceTemplatePlaceholders(randomUnlurkTemplate, ctx);
           await ctx.ChatService.SendReplyAsBot(ctx.Message.Broadcaster.Username, unlurkText, ctx.Message.Id);
       }
       catch (Exception ex)
       {
           await ctx.ChatService.SendReplyAsBot(ctx.Message.Broadcaster.Username, $"@{ctx.Message.User.Username} This is the unlurk command", ctx.Message.Id);
       }
   }
}

return new Command();