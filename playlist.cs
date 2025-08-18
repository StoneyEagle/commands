using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NoMercyBot.Database;
using NoMercyBot.Globals.SystemCalls;
using NoMercyBot.Services.Twitch;
using NoMercyBot.Services.Twitch.Scripting;
using NoMercyBot.Services.Spotify;
using NoMercyBot.Services.Interfaces;

public class Command: ICommand
{
    public string Name => "playlist";
    public CommandPermission Permission => CommandPermission.Everyone;

    public async Task Init(CommandScriptContext ctx)
    {
    }

    public async Task Callback(CommandScriptContext ctx)
    {
        var spotifyService = (SpotifyApiService)ctx.ServiceProvider.GetService(typeof(SpotifyApiService));
        var currentPlaylist = await spotifyService.GetCurrentlyPlaying();
        
        if (currentPlaylist == null)
        {
            await ctx.ChatService.SendReplyAsBot(ctx.Message.Broadcaster.Username, "No song is currently playing!", ctx.Message.Id);
            return;
        }

        string text = $"The current song is: {currentPlaylist.Item.Name} by {currentPlaylist.Item.Artists[0]?.Name} {currentPlaylist.Item.Href}";
        await ctx.ChatService.SendReplyAsBot(ctx.Message.Broadcaster.Username, text, ctx.Message.Id);
    }
}

return new Command();