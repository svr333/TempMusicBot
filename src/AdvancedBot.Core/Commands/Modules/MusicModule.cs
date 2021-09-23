using AdvancedBot.Core.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace AdvancedBot.Core.Commands.Modules
{
    [Name("Music Commands")]
    public class MusicModule : TopModule
    {
        public LavaLinkAudio AudioService { get; set; }        

        [Command("join")]
        [Alias("j")]
        [Summary("Joins the voice channel you're on.")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task JoinAndPlay()
            => await ReplyAsync(embed: await AudioService.JoinAsync(Context.Guild, Context.User as IVoiceState, Context.Channel as ITextChannel));

        [Command("leave")]
        [Alias("disconnect", "dc", "fuckoff")]
        [Summary("Leaving the voice channel.")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task Leave()
           => await AudioService.LeaveAsync(Context.Guild, Context.Message, Context.User as IVoiceState);

        [Command("replay")]
        [Alias("r")]
        [Summary("Repeats the current track.")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task Replay()        
            => await AudioService.ReplayAsync(Context.Guild, Context.Channel as ITextChannel, Context.Message, Context.User as IVoiceState);

        [Command("playlist")]
        [Alias("pl")]
        [Summary("Adds search results to the queue.")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task Playlist([Remainder] string query = null)
            => await ReplyAsync(embed: await AudioService.PlaylistAsync(Context.User as IVoiceState, Context.Channel as ITextChannel,
                Context.User as SocketGuildUser, Context.Guild, query));

        [Command("queue")]
        [Alias("q", "list")]
        [Summary("View the playlist.")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task ListAsync()
            => await AudioService.ListAsync(Context.Guild, Context.Channel as ITextChannel, Context.User);

        [Command("shuffle")]
        [Summary("Shuffle the queue.")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task Shuffle()
            => await AudioService.ShuffleAsync(Context.Guild, Context.Message, Context.User as IVoiceState);

        [Command("playskip")]
        [Alias("ps", "pskip")]
        [Summary("Plays a music with the given name or url.")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task PlaySkip([Remainder] string query = null)
            => await ReplyAsync(embed: await AudioService.PlaySkipAsync(Context.User as IVoiceState, Context.Channel as ITextChannel,
                Context.User as SocketGuildUser, Context.Guild, query));

        [Command("play")]
        [Alias("p")]
        [Summary("Plays a music with the given name or url. (If the music is playing, it is added to the queue.)")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task Play([Remainder] string query = null)
           => await ReplyAsync(embed: await AudioService.PlayAsync(Context.User as IVoiceState, Context.Channel as ITextChannel, 
               Context.User as SocketGuildUser, Context.Guild, query));

        [Command("Soundcloud")]
        [Alias("Sc")]
        [Summary("Searches soundcloud for a music. (If the music is playing, it is added to the queue.)")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task PlayCloud([Remainder] string query = null)
           => await ReplyAsync(embed: await AudioService.PlayCloudAsync(Context.User as IVoiceState, Context.Channel as ITextChannel,
               Context.User as SocketGuildUser, Context.Guild, query));

        [Command("Lyrics")]
        [Alias("Lyric", "Ly")]
        [Summary("Retrieves the lyrics of the song mentioned or the current song.")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task Lyrics([Remainder] string query = null)
            => await AudioService.LyricsAsync(Context.Guild, Context.Channel as ITextChannel, Context.User as SocketGuildUser,query);

        [Command("Now playing")]
        [Alias("Np")]
        [Summary("Shows the music that is playing.")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task Song()
            => await ReplyAsync(embed: await AudioService.SongAsync(Context.Guild));        

        [Command("Search")]
        [Summary("Searches the results of a URL on YouTube.")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task SearchAsync([Remainder] string query = null)
            => await AudioService.SearchAsync(Context.Channel as ITextChannel, Context.User as SocketGuildUser, query, Context.Guild);

        [Command("Jump")]
        [Summary("Play music from the queue.")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task Jump(string position = null)
            => await ReplyAsync(embed: await AudioService.JumpAsync(Context.Guild, position, Context.User as SocketGuildUser));

        [Command("Move")]
        [Summary("Move the selected song to the provided position.")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task Move(int moveTrack, int position)
            => await AudioService.MoveAsync(Context.Guild, Context.Channel as ITextChannel, Context.Message, Context.User as SocketGuildUser, moveTrack, position);

        [Command("Swap")]
        [Summary("Swap track positions in the queue.")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task Swap(int trackNum1, int trackNum2)
            => await AudioService.SwapAsync(Context.Guild, Context.Channel as ITextChannel, Context.Message, Context.User as SocketGuildUser, trackNum1, trackNum2);

        [Command("Seek")]
        [Summary("Adjust the position of the music.")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task Seek(string position = null)
           => await ReplyAsync(embed: await AudioService.SeekAsync(position, Context.Guild, Context.User as SocketGuildUser));

        [Command("Forward")]
        [Alias("Fw")]
        [Summary("Forward a certain amount in the current track.")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task Vseek(string time = null)
            => await ReplyAsync(embed: await AudioService.Forward(Context.Guild, time, Context.User as SocketGuildUser));

        [Command("Rewind")]
        [Alias("Rw")]
        [Summary("Rewinds by a certain amount in the current track.")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task Rewind(string time = null)
            => await ReplyAsync(embed: await AudioService.Rewind(Context.Guild, time, Context.User as SocketGuildUser)); 
      
        [Command("Loop")]
        [Alias("L", "Repeat")]
        [Summary("Loops the whole queue.")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task Loop()
              => await AudioService.LoopAsync(Context.Guild, Context.Channel as ITextChannel, Context.User as SocketGuildUser);

        [Command("Remove")]
        [Alias("Rm")]
        [Summary("Removes a certain entry from the queue.")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task Remove([Remainder] string track = null)
           => await ReplyAsync(embed: await AudioService.RemoveAsync(track, Context.Guild, Context.User as SocketGuildUser));

        [Command("Remove Range")]
        [Alias("Rr")]
        [Summary("Remove musics in range.")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task RemoveRange(int trackNum1 = 0, int trackNum2 = 0)
        {
            if (trackNum1 > trackNum2)            
                await ReplyAsync(embed: await AudioService.RemoveRange(Context.Guild, trackNum2, trackNum1, Context.User as SocketGuildUser));            
            else
                await ReplyAsync(embed: await AudioService.RemoveRange(Context.Guild, trackNum1, trackNum2, Context.User as SocketGuildUser));
        }

        [Command("Stop")]
        [Alias("St")]
        [Summary("Stops the music and sets the bot to default position (does not exit the voice channel).")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task Stop()            
               => await ReplyAsync(embed: await AudioService.StopAsync(Context.Guild, Context.User as SocketGuildUser));
       
        [Command("Clear")]
        [Summary("Clear playlist.")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task Clear()
            => await AudioService.ClearAsync(Context.Guild, Context.Channel as ITextChannel, Context.User as IVoiceState);

        [Command("Skip")]
        [Alias("Next", "N", "S")]
        [Summary("Skip the music playing.")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task Skip()
            => await ReplyAsync(embed: await AudioService.SkipTrackAsync(Context.Guild, Context.User as SocketGuildUser));

        [Command("Volume")]
        [Alias("Vol")]
        [Summary("Adjusts the sound level.")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task Volume(int volume)
            => await ReplyAsync(embed: await AudioService.SetVolumeAsync(Context.Guild, volume, Context.User as SocketGuildUser));

        [Command("Pause")]
        [Summary("Pauses the music.")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task Pause()
            => await ReplyAsync(embed: await AudioService.PauseAsync(Context.Guild, Context.User as SocketGuildUser));

        [Command("Resume")]
        [Summary("Resumes paused music.")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task Resume()
            => await ReplyAsync(embed: await AudioService.ResumeAsync(Context.Guild, Context.User as SocketGuildUser));
    }
}
