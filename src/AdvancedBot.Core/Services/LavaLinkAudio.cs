using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;
using Interactivity;
using Interactivity.Pagination;
using System.Collections.Concurrent;
using AdvancedBot.Core.Services.DataStorage;
using Victoria.Responses.Search;

namespace AdvancedBot.Core.Services
{
    public sealed class LavaLinkAudio
    {
        private readonly LavaNode _lavaNode;
        private GuildAccountService _guilds;
        private InteractivityService _interactivity;
        private readonly ConcurrentDictionary<ulong, CancellationTokenSource> _disconnectTokens;

        public LavaLinkAudio(LavaNode lavaNode, GuildAccountService guilds, InteractivityService service)
        {
            _lavaNode = lavaNode;
            _guilds = guilds;
            _interactivity = service;
            _disconnectTokens = new ConcurrentDictionary<ulong, CancellationTokenSource>();
        }


        private bool GetDjRole(IGuild guild, SocketGuildUser user)
        {
            var g = _guilds.GetOrCreateGuildAccount(guild.Id);

            if (g.DjRoleId != 0)
            {
                if (user.Roles.FirstOrDefault(x => x.Id == g.DjRoleId) == null)
                {
                    if (user.GuildPermissions.ManageRoles)
                    {
                        return true;
                    }

                    return false;
                }
            }

            return true;
        }

        private async Task<Embed> DjRoleErrorMessage()
            => EmbedHandler.CreateErrorEmbed("Dj Role", "You must have dj role or manage roles permission to use this command\nDj Role: " + djrole.Mention);

        bool play = false, pause = false;
        IRole djrole;
        public async Task<Embed> JoinAsync(IGuild guild, IVoiceState voiceState, ITextChannel textChannel)
        {

            if (!GetDjRole(guild, voiceState as SocketGuildUser))
            {
                return await DjRoleErrorMessage();
            }

            if (_lavaNode.HasPlayer(guild))
            {
                if (voiceState.VoiceChannel is null)
                    return EmbedHandler.CreateErrorEmbed("Join", "You must join a voice channel!");

                var player = _lavaNode.GetPlayer(guild);

                int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

                if (voiceState.VoiceChannel.Id == player.VoiceChannel.Id)
                    return EmbedHandler.CreateErrorEmbed("Join", "I'm connected to the voice channel you're on");

                if (voiceState.VoiceChannel == player.VoiceChannel || ((voiceState.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
                {
                    if (player.Track != null)
                    {
                        var track = player.Track;
                        var position = player.Track.Position;
                        var queue = player.Queue;

                        play = player.PlayerState == PlayerState.Playing;
                        pause = player.PlayerState == PlayerState.Paused;

                        await player.StopAsync();
                        player.Queue.Clear();

                        await _lavaNode.LeaveAsync(voiceState.VoiceChannel);
                        await _lavaNode.JoinAsync(voiceState.VoiceChannel, textChannel);

                        var _player = _lavaNode.GetPlayer(guild);

                        if (pause)
                        {
                            await _player.PlayAsync(track);
                            await _player.SeekAsync(position);
                            await _player.PauseAsync();

                            foreach (var item in queue)
                            {
                                _player.Queue.Enqueue(item);
                            }
                        }
                        else if (play)
                        {
                            await _player.PlayAsync(track);
                            await _player.SeekAsync(position);

                            foreach (var item in queue)
                            {
                                _player.Queue.Enqueue(item);
                            }
                        }
                        leavejoin = false;
                        return EmbedHandler.CreateBasicEmbed("Join", $"I joined to the channel \"{voiceState.VoiceChannel.Name}\" ");
                    }


                    await _lavaNode.LeaveAsync(voiceState.VoiceChannel);
                    await _lavaNode.JoinAsync(voiceState.VoiceChannel, textChannel);

                    return EmbedHandler.CreateBasicEmbed("Join", $"I joined to the channel \"{voiceState.VoiceChannel.Name}\" ");
                }

                return EmbedHandler.CreateErrorEmbed("Join", "Someone else is already listening to music on different channel!");
            }

            try
            {
                await _lavaNode.JoinAsync(voiceState.VoiceChannel, textChannel);
                return EmbedHandler.CreateBasicEmbed("Join", $"I joined to the channel \"{voiceState.VoiceChannel.Name}\" ");
            }
            catch (Exception ex)
            {
                return EmbedHandler.CreateErrorEmbed(null, "Something went wrong");
            }
        }

        public async Task ReplayAsync(IGuild guild, ITextChannel channel, IMessage message, IVoiceState voiceState)
        {
            if (!GetDjRole(guild, voiceState as SocketGuildUser))
            {
                await channel.SendMessageAsync(embed: await DjRoleErrorMessage());
                return;
            }

            if (!_lavaNode.HasPlayer(guild))
            {
                await channel.SendMessageAsync(embed: EmbedHandler.CreateErrorEmbed("Join", "I'm not joined to a voice channel"));
                return;
            }

            var player = _lavaNode.GetPlayer(guild);

            int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

            if (voiceState.VoiceChannel == player.VoiceChannel || ((voiceState.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
            {
                if (player.PlayerState is PlayerState.Playing || player.PlayerState is PlayerState.Paused)
                {
                    await player.SeekAsync(TimeSpan.FromSeconds(0));
                    var emoji = new Emoji("🔂");
                    await message.AddReactionAsync(emoji);
                    return;
                }

                await channel.SendMessageAsync(embed: EmbedHandler.CreateErrorEmbed("Play", "Music is not playing"));
                return;
            }
            else
            {
                await channel.SendMessageAsync(embed: EmbedHandler.CreateErrorEmbed("Join", "Someone else is already listening to music on different channel!"));
                return;
            }
        }

        public async Task<Embed> PlaylistAsync(IVoiceState voiceState, ITextChannel textChannel, SocketGuildUser user, IGuild guild, string query)
        {
            if (!GetDjRole(guild, voiceState as SocketGuildUser))
            {
                return EmbedHandler.CreateErrorEmbed("Dj Role", "You don't have a DJ rol\nDj Role : {djrole.Mention}e");
            }

            if (user.VoiceChannel is null)
                return EmbedHandler.CreateErrorEmbed("Join", "You must join a voice channel");

            if (query == null)
                return EmbedHandler.CreateErrorEmbed("Invalid Usage :x:", "Please enter the value you want to search\n\n.playlist [query]");

            await _lavaNode.JoinAsync(voiceState.VoiceChannel, textChannel);

            try
            {
                var player = _lavaNode.GetPlayer(guild);

                int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

                if (voiceState.VoiceChannel == player.VoiceChannel || ((voiceState.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
                {
                    LavaTrack track;
                    var search = await SearchQueryFromYoutube(query);

                    if (search.Status == SearchStatus.NoMatches)
                    {
                        return EmbedHandler.CreateErrorEmbed("No Matches", $"\"{query}\" I couldn't find anything about it ");
                    }

                    track = search.Tracks.FirstOrDefault();
                    player.Queue.Enqueue(search.Tracks);

                    if (player.Track != null && player.PlayerState is PlayerState.Playing || player.PlayerState is PlayerState.Paused)
                        return EmbedHandler.CreateBasicEmbed("Added to queue", $"playlist added to queue ", user);

                    await player.PlayAsync(track);
                    return EmbedHandler.CreateBasicEmbed("Playlist", $"Playing 🎶 [{track.Title}]({ track.Url}) ", user);
                }

                return EmbedHandler.CreateErrorEmbed("Join", "Someone else is already listening to music on different channel!");
            }
            catch (ArgumentNullException ex)
            {
                return EmbedHandler.CreateErrorEmbed(null, "Something went wrong");
            }
            catch (Exception ex)
            {
                return EmbedHandler.CreateErrorEmbed(null, "Something went wrong");
            }
        }

        public async Task ShuffleAsync(IGuild guild, SocketUserMessage userMessage, IVoiceState voiceState)
        {
            if (!GetDjRole(guild, voiceState as SocketGuildUser))
            {
                await userMessage.Channel.SendMessageAsync(embed: await DjRoleErrorMessage());
                return;
            }

            if (!_lavaNode.HasPlayer(guild))
            {
                await userMessage.Channel.SendMessageAsync(embed: EmbedHandler.CreateErrorEmbed("Join", "I'm not joined to a voice channel"));
                return;
            }

            var player = _lavaNode.GetPlayer(guild);


            if (player.Queue.Count == 0 || player.Queue.Count == 1)
            {
                await userMessage.Channel.SendMessageAsync(embed: EmbedHandler.CreateErrorEmbed("Queue",
                    "Not enough music to shuffle"));

                return;
            }

            int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

            if (voiceState.VoiceChannel == player.VoiceChannel || ((voiceState.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
            {
                player.Queue.Shuffle();
                var emoji = new Emoji("👌");
                await userMessage.AddReactionAsync(emoji);
                return;
            }

            await userMessage.Channel.SendMessageAsync(embed: EmbedHandler.CreateErrorEmbed("Join", "Someone else is already listening to music on different channel!"));
        }

        public async Task ListAsync(IGuild guild, ITextChannel channel, SocketUser user)
        {
            if (!_lavaNode.HasPlayer(guild))
            {
                await channel.SendMessageAsync(embed: EmbedHandler.CreateErrorEmbed("Join", "I'm not joined to a voice channel"));
                return;
            }

            var player = _lavaNode.GetPlayer(guild);

            List<string> tracks = new List<string>();
            StringBuilder builder = new StringBuilder();

            if (player.PlayerState is PlayerState.Playing)
            {
                if (player.Track.Duration.Hours == 0)
                {
                    builder.Append($"Playing 🎶 [{player.Track.Title}]({player.Track.Url}) | `{player.Track.Position:mm\\:ss}/{player.Track.Duration:mm\\:ss}`\n\n");
                }
                else
                {
                    builder.Append($"Playing 🎶 [{player.Track.Title}]({player.Track.Url}) | `{player.Track.Position:hh\\:mm\\:ss}/{player.Track.Duration:hh\\:mm\\:ss}`\n\n");
                }
            }
            else if (player.PlayerState is PlayerState.Paused)
            {
                if (player.Track.Duration.Hours == 0)
                {
                    builder.Append($"Paused ⏸️ [{player.Track.Title}]({player.Track.Url}) | `{player.Track.Position:mm\\:ss}/{player.Track.Duration:mm\\:ss}`\n\n");
                }
                else
                {
                    builder.Append($"Paused ⏸️ [{player.Track.Title}]({player.Track.Url}) | `{player.Track.Position:hh\\:mm\\:ss}/{player.Track.Duration:hh\\:mm\\:ss}`\n\n");
                }
            }

            string emoji;
            var loop = _guilds.GetOrCreateGuildAccount(guild.Id).PlayerShouldLoop;

            try
            {
                emoji = GetEmoji(loop);

                string time = GetTotalLength(player);

                int trackNum = 1;
                foreach (var track in player.Queue)
                {
                    if (track.Duration.Hours == 0)
                    {
                        builder.Append($"`{trackNum}.` [{track.Title}]({track.Url}) | `{track.Duration:mm\\:ss}`\n\n");
                    }
                    else
                    {
                        builder.Append($"`{trackNum}.` [{track.Title}]({track.Url}) | `{track.Duration:hh\\:mm\\:ss}`\n\n");
                    }

                    if (trackNum % 10 == 0)
                    {
                        tracks.Add(builder.ToString());
                        builder.Clear();
                    }

                    trackNum++;
                }

                if (builder.Length != 0)
                    tracks.Add(builder.ToString());

                if (tracks.Count == 1)
                {
                    _interactivity.DelayedDeleteMessageAsync(await channel.SendMessageAsync(embed: new EmbedBuilder().WithAuthor($"{player.Queue.Count} Tracks | Total Length : {time}")
                        .WithTitle("Playlist")
                        .WithDescription(tracks[0])
                        .WithFooter(user.Username + $" page 1/1 Loop : {emoji}", user.GetAvatarUrl()).Build()), TimeSpan.FromMinutes(2));

                    return;
                }

                var pages = new PageBuilder[tracks.Count];

                for (int i = 0; i < pages.Length; i++)
                {
                    pages[i] = new PageBuilder().WithAuthor($"{player.Queue.Count} Tracks | Total Length : {time}").WithTitle("Playlist")
                            .WithDescription(tracks[i])
                            .WithFooter(user.Username + $" page {i + 1}/{pages.Length} Loop : {emoji}", user.GetAvatarUrl());
                }

                var paginator = new StaticPaginatorBuilder()
                    .WithUsers(user)
                    .WithPages(pages)
                    .WithFooter(PaginatorFooter.None)
                    .Build();

                await _interactivity.SendPaginatorAsync(paginator, channel, TimeSpan.FromMinutes(2));
            }
            catch (Exception ex)
            {
                await channel.SendMessageAsync(embed: EmbedHandler.CreateErrorEmbed("List", "Queue is empty"));
                return;
            }
        }

        private string GetEmoji(bool loop)
        {
            if (loop)
                return "✅";
            else
                return "❌";
        }

        private string GetTotalLength(LavaPlayer player)
        {
            TimeSpan sum = new TimeSpan(0, 0, 0);

            sum += player.Track.Duration - player.Track.Position;

            foreach (var track in player.Queue)
                sum += track.Duration;

            if (sum.Hours == 0)
                return sum.ToString("mm\\:ss");

            else
                return sum.ToString("hh\\:mm\\:ss");

        }

        private async Task<SearchResponse> SearchQueryFromYoutube(string query)
            => Uri.IsWellFormedUriString(query, UriKind.Absolute) ? await _lavaNode.SearchAsync(SearchType.Direct, query) : await _lavaNode.SearchYouTubeAsync(query);

        public async Task<Embed> PlayAsync(IVoiceState voiceState, ITextChannel textChannel, SocketGuildUser user, IGuild guild, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return EmbedHandler.CreateErrorEmbed("play", "Please provide search terms.");
            }

            if (!_lavaNode.HasPlayer(guild))
            {
                return EmbedHandler.CreateErrorEmbed("play", "I'm not connected to a voice channel.");
            }

            var searchResponse = await SearchQueryFromYoutube(query);

            if (searchResponse.Status == SearchStatus.LoadFailed || searchResponse.Status == SearchStatus.NoMatches)
            {
                return EmbedHandler.CreateErrorEmbed("play", $"I wasn't able to find anything for `{query}`.");
            }

            var player = _lavaNode.GetPlayer(guild);

            if (player.PlayerState == PlayerState.Playing || player.PlayerState == PlayerState.Paused)
            {
                if (!string.IsNullOrWhiteSpace(searchResponse.Playlist.Name))
                {
                    foreach (var track in searchResponse.Tracks)
                    {
                        player.Queue.Enqueue(track);
                    }

                    return EmbedHandler.CreateBasicEmbed("play", $"Enqueued {searchResponse.Tracks.Count} tracks.", user);
                }
                else
                {
                    var track = searchResponse.Tracks.ElementAt(0);
                    player.Queue.Enqueue(track);
                    return EmbedHandler.CreateBasicEmbed("play", $"Enqueued {track.Title}.", user);
                }
            }
            else
            {
                var track = searchResponse.Tracks.ElementAt(0);

                if (!string.IsNullOrWhiteSpace(searchResponse.Playlist.Name))
                {
                    for (var i = 0; i < searchResponse.Tracks.Count; i++)
                    {
                        if (i == 0)
                        {
                            await player.PlayAsync(track);
                            return EmbedHandler.CreateBasicEmbed("play", $"Now Playing: {track.Title}", user);
                        }
                        else
                        {
                            player.Queue.Enqueue(searchResponse.Tracks.ElementAt(i));
                        }
                    }

                    return EmbedHandler.CreateBasicEmbed("play", $"Enqueued {searchResponse.Tracks.Count} tracks.", user);
                }
                else
                {
                    await player.PlayAsync(track);
                    return EmbedHandler.CreateBasicEmbed("play", $"Now Playing: {track.Title}", user);
                }
            }
        }

        public async Task<Embed> PlaySkipAsync(IVoiceState voiceState, ITextChannel textChannel, SocketGuildUser user, IGuild guild, string query)
        {
            if (!GetDjRole(guild, voiceState as SocketGuildUser))
            {
                return await DjRoleErrorMessage();
            }

            if (voiceState.VoiceChannel is null)
                return EmbedHandler.CreateErrorEmbed("Join", "You must join a voice channel");

            if (query is null)
                return EmbedHandler.CreateErrorEmbed("Invalid Usage :x:", "Please enter the value you want to search\n\n.play skip [query]");

            await _lavaNode.JoinAsync(voiceState.VoiceChannel, textChannel);

            var player = _lavaNode.GetPlayer(guild);

            int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

            try
            {
                if (voiceState.VoiceChannel == player.VoiceChannel || ((voiceState.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
                {
                    LavaTrack track;

                    var search = await SearchQueryFromYoutube(query);

                    if (search.Status == SearchStatus.NoMatches)
                        return EmbedHandler.CreateErrorEmbed("No Matches", $"\"{query}\" I couldn't find anything about it ");

                    track = search.Tracks.FirstOrDefault();

                    await player.PlayAsync(track);
                    return EmbedHandler.CreateBasicEmbed("Play Skip", $"Playing 🎶 [{track.Title}]({ track.Url}) ", user);
                }

                return EmbedHandler.CreateErrorEmbed("Join", "Someone else is already listening to music on different channel!");
            }
            catch (Exception ex)
            {
                return EmbedHandler.CreateErrorEmbed(null, "Something went wrong");
            }

        }

        public async Task<Embed> PlayCloudAsync(IVoiceState voiceState, ITextChannel textChannel, SocketGuildUser user, IGuild guild, string query)
        {
            if (!GetDjRole(guild, voiceState as SocketGuildUser))
            {
                return await DjRoleErrorMessage();
            }

            if (user.VoiceChannel == null)
                return EmbedHandler.CreateErrorEmbed("Join", "You must join a voice channel");

            if (query == null)
                return EmbedHandler.CreateErrorEmbed("Invalid Usage :x:", "Please enter the value you want to search\n\n.Soundcloud [query]");

            await _lavaNode.JoinAsync(voiceState.VoiceChannel, textChannel);

            var player = _lavaNode.GetPlayer(guild);

            int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

            try
            {
                if (voiceState.VoiceChannel == player.VoiceChannel || ((voiceState.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
                {
                    LavaTrack track;

                    var search = Uri.IsWellFormedUriString(query, UriKind.Absolute) ?
                        await _lavaNode.SearchAsync(SearchType.Direct, query)
                        : await _lavaNode.SearchSoundCloudAsync(query);

                    if (search.Status == SearchStatus.NoMatches)
                        return EmbedHandler.CreateErrorEmbed("No Matches", $"\"{query}\" I couldn't find anything about it ");

                    track = search.Tracks.FirstOrDefault();

                    await player.PlayAsync(track);
                    return EmbedHandler.CreateBasicEmbed("Play Skip", $"Playing 🎶 [{track.Title}]({ track.Url}) ", user);
                }

                return EmbedHandler.CreateErrorEmbed("Join", "Someone else is already listening to music on different channel!");
            }
            catch (Exception ex)
            {
                return EmbedHandler.CreateErrorEmbed(null, "Something went wrong");
            }

        }

        public async Task LyricsAsync(IGuild guild, ITextChannel channel, SocketGuildUser user, string query)
        {
            LavaPlayer player = null;

            if (_lavaNode.HasPlayer(guild))
            {
                player = _lavaNode.GetPlayer(guild);
            }

            if (query is null)
            {
                if (player.Track is null)
                {
                    var prefix = _guilds.GetOrCreateGuildAccount(guild.Id).DefaultDisplayPrefix;
                    await channel.SendMessageAsync(embed: EmbedHandler.CreateErrorEmbed("Invalid Usage :x:", $"Example : {prefix}lyrics <query>"));
                    return;
                }
                query = player.Track.Title;
            }
            try
            {
                await channel.SendMessageAsync($"<:genius:846266429657317407> **Searching** 🔎 `{query}`");
                string lyrics = await LyricsService.GetLyricsFromGenius(query);

                if (lyrics.Length > 2000)
                {
                    await channel.SendMessageAsync(embed: await EmbedHandler.CreateLyricsEmbed($"{LyricsService.Title} Lyrics", lyrics.Substring(0, 1900) + $"...\n\nFor the full lyrics, [click here]({LyricsService.TrackURL})", user, LyricsService.TrackImage));
                    return;
                }

                await channel.SendMessageAsync(embed: await EmbedHandler.CreateLyricsEmbed($"{LyricsService.Title} Lyrics", lyrics, user, LyricsService.TrackImage));
            }
            catch (Exception ex)
            {
                await channel.SendMessageAsync(embed: EmbedHandler.CreateErrorEmbed(null, "Something went wrong"));
            }
        }

        public async Task<Embed> SongAsync(IGuild guild)
        {
            if (!_lavaNode.HasPlayer(guild))
                return EmbedHandler.CreateErrorEmbed("Join", "I must to join a voice channel");

            var player = _lavaNode.GetPlayer(guild);

            if (player.Track is null)
            {
                return EmbedHandler.CreateErrorEmbed("Play", "Music is not playing");
            }

            if (player.Track.Duration.Hours == 0)
            {
                if (player.PlayerState is PlayerState.Paused)
                    return EmbedHandler.CreateBasicEmbed("Paused ⏸️", $"[{player.Track.Title}]({player.Track.Url}) " +
                        $"[`{player.Track.Position:mm\\:ss}\\" +
                        $"{player.Track.Duration:mm\\:ss}`]");

                return EmbedHandler.CreateBasicEmbed("Now Playing 🎶", $"[{player.Track.Title}]({player.Track.Url}) " +
                    $"[`{player.Track.Position:mm\\:ss}\\" +
                    $"{player.Track.Duration:mm\\:ss}`]");
            }
            else
            {
                if (player.PlayerState is PlayerState.Paused)
                    return EmbedHandler.CreateBasicEmbed("Paused ⏸️", $"[{player.Track.Title}]({player.Track.Url}) " +
                        $"[`{player.Track.Position:hh\\:mm\\:ss}\\" +
                        $"{player.Track.Duration:hh\\:mm\\:ss}`]");

                return EmbedHandler.CreateBasicEmbed("Now Playing 🎶", $"[{player.Track.Title}]({player.Track.Url}) " +
                    $"[`{player.Track.Position:hh\\:mm\\:ss}\\" +
                    $"{player.Track.Duration:hh\\:mm\\:ss}`]");
            }
        }

        readonly List<SocketUser> names = new List<SocketUser>();
        readonly List<LavaTrack[]> querys = new List<LavaTrack[]>();
        readonly List<IUserMessage> messages = new List<IUserMessage>();
        public async Task SearchAsync(ITextChannel textChannel, SocketGuildUser user, string query, IGuild guild)
        {
            if (!GetDjRole(guild, user))
            {
                await textChannel.SendMessageAsync(embed: await DjRoleErrorMessage());
                return;
            }

            if (user.VoiceChannel is null)
            {
                await textChannel.SendMessageAsync(embed: EmbedHandler.CreateErrorEmbed("Search", "You must join a voice channel"));
                return;
            }

            if (query == null)
            {
                await textChannel.SendMessageAsync(embed: EmbedHandler.CreateErrorEmbed("Invalid Usage :x:",
                    "Please enter the value you want to search\n\n.search [query]"));
                return;
            }

            await _lavaNode.JoinAsync(user.VoiceChannel, textChannel);
            var player = _lavaNode.GetPlayer(guild);
            int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

            try
            {
                if (user.VoiceChannel == player.VoiceChannel || ((user.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
                {
                    if (names.Contains(user))
                    {
                        await textChannel.SendMessageAsync(embed: EmbedHandler.CreateErrorEmbed("Cancel",
                                "Write \"cancel\" to exit the search that is already active"));
                        return;
                    }

                    StringBuilder TrackList = new StringBuilder();

                    var search = await SearchQueryFromYoutube(query);

                    if (search.Status == SearchStatus.NoMatches)
                    {
                        await textChannel.SendMessageAsync(embed: EmbedHandler.CreateErrorEmbed("Search",
                            $"\"{query}\" I couldn't find anything about it"));
                        return;
                    }
                    LavaTrack[] tracks = new LavaTrack[10];

                    if (search.Tracks.Count < 10)
                    {
                        int a = 0;
                        tracks = new LavaTrack[search.Tracks.Count];
                        foreach (var item in search.Tracks)
                        {
                            tracks[a] = item;
                            a++;
                        }
                    }

                    if (tracks.Length == 10)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            tracks[i] = search.Tracks.ElementAt(i);
                        }
                    }
                    for (int i = 0; i < tracks.Length; i++)
                    {
                        if (tracks[i].Duration.Hours == 0)
                            TrackList.Append($"`{i + 1}.` [{tracks[i].Title}]({tracks[i].Url}) [`{tracks[i].Duration:mm\\:ss}`]\n\n");
                        else
                            TrackList.Append($"`{i + 1}.` [{tracks[i].Title}]({tracks[i].Url}) [`{tracks[i].Duration:hh\\:mm\\:ss}`]\n\n");
                    }

                    var msg = await textChannel.SendMessageAsync("Please select one.\n",
                        embed: EmbedHandler.CreateBasicEmbed("Search", $"{TrackList}", user));

                    messages.Add(msg);
                    names.Add(user);
                    querys.Add(tracks);

                    await Task.Delay(TimeSpan.FromSeconds(40));

                    if (messages.Contains(msg))
                    {
                        messages.Remove(msg);
                        names.Remove(user);
                        querys.Remove(tracks);

                        await msg.DeleteAsync();

                        await textChannel.SendMessageAsync("**Timeout** ❌");
                    }

                    return;
                }

                await textChannel.SendMessageAsync(embed: EmbedHandler.CreateErrorEmbed("Join", "Someone else is already listening to music on different channel!"));
            }
            catch (Exception ex)
            {
                await textChannel.SendMessageAsync(embed: EmbedHandler.CreateErrorEmbed(null, "Something went wrong"));
            }

        }

        public async Task<Embed> JumpAsync(IGuild guild, string title, SocketGuildUser user)
        {
            if (!GetDjRole(guild, user))
            {
                return await DjRoleErrorMessage();
            }

            if (!_lavaNode.HasPlayer(guild))
                return EmbedHandler.CreateErrorEmbed("Join", "I must to join a voice channel");

            if (title is null || title == "0")
            {
                return EmbedHandler.CreateErrorEmbed("Invalid Usage ❌", "Example : .jump {number or title}");
            }

            var player = _lavaNode.GetPlayer(guild);

            int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

            if (player.Queue.Count == 0)
                return EmbedHandler.CreateErrorEmbed("Queue", "music queue is empty");

            LavaTrack track;

            if (int.TryParse(title, out int position))
            {
                if (user.VoiceChannel == player.VoiceChannel || ((user.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
                {
                    position--;
                    track = player.Queue.RemoveAt(position);
                    await player.PlayAsync(track);

                    return EmbedHandler.CreateBasicEmbed("Jump", $"Playing 🎶 [{track.Title}]({track.Url})", user);
                }

                return EmbedHandler.CreateErrorEmbed("Join", "Someone else is already listening to music on different channel!");
            }

            if (user.VoiceChannel == player.VoiceChannel || ((user.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
            {
                track = player.Queue.FirstOrDefault(x => x.Title == title);
                if (track is null)
                {
                    return EmbedHandler.CreateErrorEmbed(null, "there is no such part");
                }
                player.Queue.Remove(track);

                await player.PlayAsync(track);

                return EmbedHandler.CreateBasicEmbed("Jump", $"Playing 🎶 [{track.Title}]({track.Url})", user);
            }

            return EmbedHandler.CreateErrorEmbed("Join", "Someone else is already listening to music on different channel!");
        }

        public async Task MoveAsync(IGuild guild, ITextChannel channel, SocketMessage message, SocketGuildUser user, int move, int position)
        {
            if (!GetDjRole(guild, user))
            {
                await channel.SendMessageAsync(embed: await DjRoleErrorMessage());
                return;
            }

            if (!_lavaNode.HasPlayer(guild))
            {
                await channel.SendMessageAsync(embed: EmbedHandler.CreateErrorEmbed("Join", "I must to join a voice channel"));
                return;
            }

            if (user.VoiceChannel is null)
            {
                await channel.SendMessageAsync(embed: EmbedHandler.CreateErrorEmbed("Join", "You must to join a voice channel"));
                return;
            }

            if (position == 0 || move == 0)
            {
                await channel.SendMessageAsync(embed: EmbedHandler.CreateErrorEmbed("Invalid Usage ❌", "Example : .move 1 3"));
                return;
            }

            var player = _lavaNode.GetPlayer(guild);

            if (player.Queue.Count == 0 || player.Queue.Count == 1)
            {
                await channel.SendMessageAsync(embed: EmbedHandler.CreateErrorEmbed("Move", "Not enough tracks in the tail"));
                return;
            }

            int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

            try
            {
                if (user.VoiceChannel == player.VoiceChannel || ((user.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
                {
                    move--;
                    position--;

                    var que = player.Queue.ToList();
                    var track = que[move];
                    que.Remove(que[move]);

                    player.Queue.Clear();

                    for (int i = 0; i < que.Count; i++)
                    {
                        if (i == position)
                        {
                            player.Queue.Enqueue(track);
                        }
                        player.Queue.Enqueue(que[i]);
                    }

                    await message.AddReactionAsync(new Emoji("👌"));
                    return;
                }

                await channel.SendMessageAsync(embed: EmbedHandler.CreateErrorEmbed("Join", "Someone else is already listening to music on different channel!"));
            }
            catch (Exception ex)
            {
                EmbedHandler.CreateErrorEmbed(null, "Something went wrong");
            }
        }

        public async Task SwapAsync(IGuild guild, ITextChannel channel, SocketMessage message, SocketGuildUser user, int number1, int number2)
        {
            if (!GetDjRole(guild, user))
            {
                await channel.SendMessageAsync(embed: await DjRoleErrorMessage());
                return;
            }

            if (!_lavaNode.HasPlayer(guild))
            {
                await channel.SendMessageAsync(embed: EmbedHandler.CreateErrorEmbed("Join", "I must to join a voice channel"));
                return;
            }

            if (user.VoiceChannel is null)
            {
                await channel.SendMessageAsync(embed: EmbedHandler.CreateErrorEmbed("Join", "You must to join a voice channel"));
                return;
            }

            if (number1 == 0 || number2 == 0)
            {
                await channel.SendMessageAsync(embed: EmbedHandler.CreateErrorEmbed("Invalid Usage ❌", "Example : .swap 1 3"));
                return;
            }

            var player = _lavaNode.GetPlayer(guild);

            if (player.Queue.Count == 0 || player.Queue.Count == 1)
            {
                await channel.SendMessageAsync(embed: EmbedHandler.CreateErrorEmbed("Move", "Not enough tracks in the tail"));
                return;
            }

            int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

            try
            {
                if (user.VoiceChannel == player.VoiceChannel || ((user.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
                {
                    number1--;
                    number2--;
                    var que = player.Queue.ToList();

                    player.Queue.Clear();

                    LavaTrack swap = que[number1];
                    que[number1] = que[number2];
                    que[number2] = swap;

                    foreach (var item in que)
                    {
                        player.Queue.Enqueue(item);
                    }

                    await message.AddReactionAsync(new Emoji("👌"));
                    return;
                }

                await channel.SendMessageAsync(embed: EmbedHandler.CreateErrorEmbed("Join", "Someone else is already listening to music on different channel!"));
            }
            catch (Exception ex)
            {
                EmbedHandler.CreateErrorEmbed(null, "Something went wrong");
            }
        }

        public async Task<Embed> SeekAsync(string position, IGuild guild, SocketGuildUser user)
        {
            if (!GetDjRole(guild, user))
            {
                return await DjRoleErrorMessage();
            }

            if (position is null)
                return EmbedHandler.CreateErrorEmbed("Invalid Usage :x:", "Example: `1:20` `1:30` `30` `40`");

            if (user.VoiceChannel is null)
                return EmbedHandler.CreateErrorEmbed("Join", "You must to join a voice channel");

            if (!_lavaNode.HasPlayer(guild))
                return EmbedHandler.CreateErrorEmbed("Join", "I must to join a voice channel");

            var player = _lavaNode.GetPlayer(guild);

            int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

            if (user.VoiceChannel == player.VoiceChannel || ((user.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
            {
                string[] times = new string[0];
                int h = 0, m = 0, s;

                try
                {
                    if (position.Contains(':'))
                        times = position.Split(':');

                    if (times.Length == 2)
                    {
                        m = int.Parse(times[0]);
                        s = int.Parse(times[1]);
                    }
                    else if (times.Length == 3)
                    {
                        h = int.Parse(times[0]);
                        m = int.Parse(times[1]);
                        s = int.Parse(times[2]);
                    }
                    else
                    {
                        s = int.Parse(position);
                    }
                    if (s < 0 || m < 0 || h < 0)
                    {
                        return EmbedHandler.CreateErrorEmbed("Seek", "Please enter in positive value");
                    }
                    TimeSpan seek = new TimeSpan(h, m, s);

                    if (player.Track.Duration < seek)
                    {
                        return EmbedHandler.CreateErrorEmbed("Seek", "Value must not be greater than current track duration");
                    }

                    await player.SeekAsync(seek);

                    if (seek.Hours == 0)
                        return EmbedHandler.CreateBasicEmbed("Seek", $"Set position to `{seek:mm\\:ss}`", user);
                    else
                        return EmbedHandler.CreateBasicEmbed("Seek", $"Set position to `{seek:hh\\:mm\\:ss}`");
                }


                catch (Exception ex)
                {
                    if (ex.Message == "Input string was not in a correct format.")
                        return EmbedHandler.CreateErrorEmbed("Invalid Usage :x:", "Example: `1:20` `1:30` `30` `40`");

                    return EmbedHandler.CreateErrorEmbed("Seek", ex.Message);
                }
            }

            return EmbedHandler.CreateErrorEmbed("Join", "Someone else is already listening to music on different channel!");
        }

        public async Task<Embed> Forward(IGuild guild, string position, SocketGuildUser user)
        {
            if (!GetDjRole(guild, user))
            {
                return await DjRoleErrorMessage();
            }

            if (position is null)
                return EmbedHandler.CreateErrorEmbed("Invalid Usage :x:", "Example: `1:20` `1:30` `30` `40`");

            if (user.VoiceChannel is null)
                return EmbedHandler.CreateErrorEmbed("Join", "You must to join a voice channel");

            if (!_lavaNode.HasPlayer(guild))
                return EmbedHandler.CreateErrorEmbed("Join", "I must to join a voice channel");

            var player = _lavaNode.GetPlayer(guild);

            int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

            if (user.VoiceChannel == player.VoiceChannel || ((user.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
            {
                string[] times = new string[0];
                int h = 0, m = 0, s;

                try
                {
                    if (position.Contains(':'))
                        times = position.Split(':');

                    if (times.Length == 2)
                    {
                        m = int.Parse(times[0]);
                        s = int.Parse(times[1]);
                    }
                    else if (times.Length == 3)
                    {
                        h = int.Parse(times[0]);
                        m = int.Parse(times[1]);
                        s = int.Parse(times[2]);
                    }
                    else
                    {
                        s = int.Parse(position);
                    }
                    if (s < 0 || m < 0 || h < 0)
                    {
                        return EmbedHandler.CreateErrorEmbed("Forward", "Please enter in positive value");
                    }
                    TimeSpan seek = new TimeSpan(h, m, s);
                    seek += player.Track.Position;

                    if (player.Track.Duration < seek)
                    {
                        return EmbedHandler.CreateErrorEmbed("Forward", "Value must not be greater than current track duration");
                    }

                    await player.SeekAsync(seek);

                    if (seek.Hours == 0)
                        return EmbedHandler.CreateBasicEmbed("Forward", $"Set position to `{seek:mm\\:ss}`", user);
                    else
                        return EmbedHandler.CreateBasicEmbed("Forward", $"Set position to `{seek:hh\\:mm\\:ss}`");
                }
                catch (Exception ex)
                {
                    if (ex.Message == "Input string was not in a correct format.")
                        return EmbedHandler.CreateErrorEmbed("Invalid Usage :x:", "Example: `1:20` `1:30` `30` `40`");

                    return EmbedHandler.CreateErrorEmbed("Forward", ex.Message);
                }
            }

            return EmbedHandler.CreateErrorEmbed("Join", "Someone else is already listening to music on different channel!");

        }

        public async Task<Embed> Rewind(IGuild guild, string position, SocketGuildUser user)
        {
            if (!GetDjRole(guild, user))
            {
                return await DjRoleErrorMessage();
            }

            if (position is null)
                return EmbedHandler.CreateErrorEmbed("Invalid Usage :x:", "Example: `1:20` `1:30` `30` `40`");

            if (user.VoiceChannel is null)
                return EmbedHandler.CreateErrorEmbed("Join", "You must to join a voice channel");

            if (!_lavaNode.HasPlayer(guild))
                return EmbedHandler.CreateErrorEmbed("Join", "I must to join a voice channel");

            var player = _lavaNode.GetPlayer(guild);

            int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

            if (user.VoiceChannel == player.VoiceChannel || ((user.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
            {
                string[] times = new string[0];
                int h = 0, m = 0, s;

                try
                {
                    if (position.Contains(':'))
                        times = position.Split(':');

                    if (times.Length == 2)
                    {
                        m = int.Parse(times[0]);
                        s = int.Parse(times[1]);
                    }
                    else if (times.Length == 3)
                    {
                        h = int.Parse(times[0]);
                        m = int.Parse(times[1]);
                        s = int.Parse(times[2]);
                    }
                    else
                    {
                        s = int.Parse(position);
                    }
                    if (s < 0 || m < 0 || h < 0)
                    {
                        return EmbedHandler.CreateErrorEmbed("Rewind", "Please enter in positive value");
                    }
                    TimeSpan seek = new TimeSpan(h, m, s);

                    if (player.Track.Duration < seek)
                    {
                        return EmbedHandler.CreateErrorEmbed("Rewind", "Value must not be greater than current track duration");
                    }

                    seek = player.Track.Position - seek;

                    await player.SeekAsync(seek);

                    if (seek.Hours == 0)
                        return EmbedHandler.CreateBasicEmbed("Rewind", $"Set position to `{seek:mm\\:ss}`", user);
                    else
                        return EmbedHandler.CreateBasicEmbed("Rewind", $"Set position to `{seek:hh\\:mm\\:ss}`");
                }
                catch (Exception ex)
                {
                    if (ex.Message == "Input string was not in a correct format.")
                        return EmbedHandler.CreateErrorEmbed("Invalid Usage :x:", "Example: `1:20` `1:30` `30` `40`");

                    return EmbedHandler.CreateErrorEmbed("Rewind", ex.Message);
                }
            }

            return EmbedHandler.CreateErrorEmbed("Join", "Someone else is already listening to music on different channel!");

        }

        bool loop = true;
        public async Task LoopAsync(IGuild guild, ITextChannel channel, SocketGuildUser user)
        {
            if (!GetDjRole(guild, user))
            {
                await channel.SendMessageAsync(embed: await DjRoleErrorMessage());
                return;
            }

            if (!_lavaNode.HasPlayer(guild))
            {
                await channel.SendMessageAsync(embed: EmbedHandler.CreateErrorEmbed("Join", "I must to join a voice channel"));
                return;
            }

            var g = _guilds.GetOrCreateGuildAccount(guild.Id);
            loop = g.PlayerShouldLoop;

            if (loop)
            {
                g.PlayerShouldLoop = false;

                await channel.SendMessageAsync(":repeat: *Loop Disabled*");
                return;
            }

            g.PlayerShouldLoop = true;

            _guilds.SaveGuildAccount(g);
            await channel.SendMessageAsync(":repeat: *Loop Enabled*");
        }

        public async Task<Embed> RemoveAsync(string title, IGuild guild, SocketGuildUser user)
        {
            if (!GetDjRole(guild, user))
            {
                return await DjRoleErrorMessage();
            }

            if (!_lavaNode.HasPlayer(guild))
                return EmbedHandler.CreateErrorEmbed("Join", "I must to join a voice channel");

            var player = _lavaNode.GetPlayer(guild);

            int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

            if (player.Queue.Count == 0)
                return EmbedHandler.CreateErrorEmbed("Queue", "music queue is empty");

            if (title is null || title == "0")
            {
                return EmbedHandler.CreateErrorEmbed("Invalid Usage ❌", "Example : .remove {number or title}");
            }

            if (int.TryParse(title, out int indis))
            {
                if (user.VoiceChannel == player.VoiceChannel || ((user.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
                {
                    LavaTrack track;
                    indis--;

                    track = player.Queue.RemoveAt(indis);
                    return EmbedHandler.CreateBasicEmbed("Remove", $"Removed 📑 [{track.Title}]({track.Url})", user);
                }

                return EmbedHandler.CreateErrorEmbed("Join", "Someone else is already listening to music on different channel!");
            }

            if (user.VoiceChannel == player.VoiceChannel || ((user.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
            {
                LavaTrack track;

                track = player.Queue.FirstOrDefault(x => x.Title == title);

                if (track is null)
                {
                    return EmbedHandler.CreateErrorEmbed(null, "there is no such track ❌");
                }

                player.Queue.Remove(track);
                return EmbedHandler.CreateBasicEmbed("Remove", $"Removed 📑 [{track.Title}]({track.Url})", user);
            }

            return EmbedHandler.CreateErrorEmbed("Join", "Someone else is already listening to music on different channel!");

        }

        public async Task<Embed> RemoveRange(IGuild guild, int trackNum1, int trackNum2, SocketGuildUser user)
        {
            if (!GetDjRole(guild, user))
            {
                return await DjRoleErrorMessage();
            }

            if (!_lavaNode.HasPlayer(guild))
                return EmbedHandler.CreateErrorEmbed("Join", $"I must to join a voice channel");

            if (trackNum1 == 0 || trackNum2 == 0)
                return EmbedHandler.CreateErrorEmbed("Invalid Usage ❌", "Value cannot be zero");

            var player = _lavaNode.GetPlayer(guild);

            int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

            if (player.Queue.Count == 0)
                return EmbedHandler.CreateErrorEmbed("Queue", "Queue is empty");

            if (user.VoiceChannel == player.VoiceChannel || ((user.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
            {
                int a = trackNum1, b = trackNum2;
                trackNum1--;
                trackNum2--;
                if (trackNum1 > player.Queue.Count || trackNum2 > player.Queue.Count)
                    return EmbedHandler.CreateErrorEmbed("Remove Range", "you cannot enter a value greater than the music queue");

                for (int i = trackNum1; i <= trackNum2; i++)
                    player.Queue.RemoveAt(trackNum1);

                return EmbedHandler.CreateBasicEmbed("Remove Range", $"Range {a} and {b} removed 📑", user);
            }

            return EmbedHandler.CreateErrorEmbed("Join", "Someone else is already listening to music on different channel!");
        }

        public async Task LeaveAsync(IGuild guild, SocketUserMessage message, IVoiceState voiceState)
        {
            if (!GetDjRole(guild, voiceState as SocketGuildUser))
            {
                await message.Channel.SendMessageAsync(embed: await DjRoleErrorMessage());
                return;
            }

            if (!_lavaNode.HasPlayer(guild))
            {
                await message.Channel.SendMessageAsync(embed: EmbedHandler.CreateErrorEmbed("Leave", "I'm not joined to a voice channel"));
                return;
            }

            var player = _lavaNode.GetPlayer(guild);

            int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

            if (voiceState.VoiceChannel == player.VoiceChannel || ((voiceState.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
            {
                player.Queue.Clear();
                var g = _guilds.GetOrCreateGuildAccount(guild.Id);
                g.PlayerShouldLoop = false;
                _guilds.SaveGuildAccount(g);

                await _lavaNode.LeaveAsync(player.VoiceChannel);
                var emoji = new Emoji("👋");
                await message.AddReactionAsync(emoji);

                return;
            }

            await message.Channel.SendMessageAsync(embed: EmbedHandler.CreateErrorEmbed("Leave", "Someone else is already listening to music on different channel!"));
        }

        public async Task ClearAsync(IGuild guild, ITextChannel channel, IVoiceState voiceState)
        {
            if (!GetDjRole(guild, voiceState as SocketGuildUser))
            {
                await channel.SendMessageAsync(embed: await DjRoleErrorMessage());
                return;
            }

            try
            {
                if (!_lavaNode.HasPlayer(guild))
                {
                    await channel.SendMessageAsync(embed: EmbedHandler.CreateErrorEmbed("Join", "I must to join a voice channel"));
                    return;
                }

                var player = _lavaNode.GetPlayer(guild);

                int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

                if (player.Queue.Count == 0)
                {
                    await channel.SendMessageAsync(embed: EmbedHandler.CreateErrorEmbed("Queue", "Queue is empty"));
                    return;
                }

                if (voiceState.VoiceChannel == player.VoiceChannel || ((voiceState.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
                {
                    player.Queue.Clear();
                    await channel.SendMessageAsync(":bookmark_tabs: *Cleared queue*");
                }
            }
            catch (Exception e)
            {
                await channel.SendMessageAsync(embed: EmbedHandler.CreateErrorEmbed(null, "Something went wrong"));
            }

        }

        public async Task<Embed> SkipTrackAsync(IGuild guild, SocketGuildUser user)
        {
            if (!GetDjRole(guild, user))
            {
                return await DjRoleErrorMessage();
            }

            try
            {
                var player = _lavaNode.GetPlayer(guild);

                if (player == null)
                    return EmbedHandler.CreateErrorEmbed("Play", $"Music is not playing");

                if (player.Queue.Count < 1)
                {
                    return EmbedHandler.CreateErrorEmbed("Queue", "Queue is empty!");
                }

                int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

                try
                {
                    if (user.VoiceChannel == player.VoiceChannel || ((user.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
                    {
                        var currentTrack = player.Track;

                        await player.SkipAsync();
                        return EmbedHandler.CreateBasicEmbed("Skip", $"Skipped :track_next: `{currentTrack.Title}`\n\n" +
                            $"Playing 🎶 [{player.Track.Title}]({player.Track.Url}) ", user);
                    }

                    return EmbedHandler.CreateErrorEmbed("Skip", "Someone else is already listening to music on different channel!");
                }
                catch (Exception ex)
                {
                    return EmbedHandler.CreateErrorEmbed(null, "Something went wrong");
                }

            }
            catch (Exception ex)
            {
                return EmbedHandler.CreateErrorEmbed(null, "Something went wrong");
            }
        }

        public async Task<Embed> StopAsync(IGuild guild, SocketGuildUser user)
        {
            if (!GetDjRole(guild, user))
            {
                return await DjRoleErrorMessage();
            }

            try
            {
                var player = _lavaNode.GetPlayer(guild);

                int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

                if (!(player.PlayerState is PlayerState.Playing) || (player == null))
                    return EmbedHandler.CreateErrorEmbed("Play", "Music is not playing");

                if (user.VoiceChannel == player.VoiceChannel || ((user.VoiceChannel != player.VoiceChannel) && (users == 0)))
                {
                    if (player.PlayerState is PlayerState.Playing || player.PlayerState is PlayerState.Paused)
                    {
                        player.Queue.Clear();
                        await player.StopAsync();
                        return EmbedHandler.CreateBasicEmbed("Stop :stop_button:", $"Stopped and queue cleared.", user);
                    }
                }
                else
                    return EmbedHandler.CreateErrorEmbed("Stop", "Someone else is already listening to music on different channel!");

                return EmbedHandler.CreateErrorEmbed("Play", "Music is not playing");
            }
            catch (Exception e)
            {
                return EmbedHandler.CreateErrorEmbed(null, "Something went wrong");
            }
        }

        public async Task<Embed> SetVolumeAsync(IGuild guild, int volume, SocketGuildUser user)
        {
            if (!_lavaNode.HasPlayer(guild))
                return EmbedHandler.CreateErrorEmbed("Join", $"I must join to a voice channel");

            if (volume > 150 || volume <= 0)
                return EmbedHandler.CreateErrorEmbed("Volume", "Please enter value in the range 1 and 150 inclusive");

            var player = _lavaNode.GetPlayer(guild);
            var g = _guilds.GetOrCreateGuildAccount(guild.Id);

            g.ServerVolume = volume;

            _guilds.SaveGuildAccount(g);

            try
            {
                await player.UpdateVolumeAsync((ushort)volume);
                return EmbedHandler.CreateBasicEmbed("Volume", $"Volume: {volume}", user);
            }
            catch (InvalidOperationException)
            {
                return EmbedHandler.CreateBasicEmbed("Play", "There is no music playing", user);
            }
        }

        public async Task<Embed> PauseAsync(IGuild guild, SocketGuildUser user)
        {
            if (!GetDjRole(guild, user))
            {
                return await DjRoleErrorMessage();
            }

            try
            {
                if (!_lavaNode.HasPlayer(guild))
                    return EmbedHandler.CreateErrorEmbed("Join", $"I must to join a voice channel");

                var player = _lavaNode.GetPlayer(guild);

                if (player.Track is null)
                {
                    return EmbedHandler.CreateErrorEmbed("Play", "Music is not playing");
                }

                if (player.PlayerState is PlayerState.Paused)
                {
                    return EmbedHandler.CreateErrorEmbed("Resume", "Music has already been paused");
                }

                int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

                if (user.VoiceChannel == player.VoiceChannel || ((user.VoiceChannel != player.VoiceChannel) && (users == 0)))
                {
                    await player.PauseAsync();
                    return EmbedHandler.CreateBasicEmbed("Pause ⏸️", $"[{player.Track.Title}]({player.Track.Url})", user);
                }

                return EmbedHandler.CreateErrorEmbed("Pause", "Someone else is already listening to music on different channel!");
            }
            catch (InvalidOperationException ex)
            {
                return EmbedHandler.CreateErrorEmbed(null, "Something went wrong");
            }
            catch (Exception ex)
            {
                return EmbedHandler.CreateErrorEmbed(null, "Something went wrong");
            }
        }

        public async Task<Embed> ResumeAsync(IGuild guild, SocketGuildUser user)
        {
            if (!GetDjRole(guild, user))
            {
                return await DjRoleErrorMessage();
            }

            try
            {
                if (!_lavaNode.HasPlayer(guild))
                    return EmbedHandler.CreateErrorEmbed("Join", $"I must to join a voice channel");

                var player = _lavaNode.GetPlayer(guild);

                int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

                if (user.VoiceChannel == player.VoiceChannel || ((user.VoiceChannel != player.VoiceChannel) && (users == 0)))
                {
                    if (player.PlayerState is PlayerState.Paused)
                    {
                        await player.ResumeAsync();
                        return EmbedHandler.CreateBasicEmbed("Resume :ok_hand:", $"[{player.Track.Title}]({player.Track.Url})", user);
                    }
                }
                else
                    return EmbedHandler.CreateErrorEmbed("Resume", "You must join the voice channel where the bot is located.");

                if (player.Track is null)
                    return EmbedHandler.CreateErrorEmbed("Play", "Music is not playing");

                return EmbedHandler.CreateErrorEmbed("Pause", "Music has already been playing");
            }
            catch (InvalidOperationException ex)
            {
                return EmbedHandler.CreateErrorEmbed(null, "Something went wrong");
            }
            catch (Exception ex)
            {
                return EmbedHandler.CreateErrorEmbed(null, "Something went wrong");
            }
        }

        public async Task TrackException(TrackExceptionEventArgs args)
        {
            await args.Player.TextChannel.SendMessageAsync(embed: EmbedHandler.CreateErrorEmbed(args.Player.Track.Title, args.ErrorMessage));
        }

        public async Task TrackStarted(TrackStartEventArgs args)
        {
            if (!_disconnectTokens.TryGetValue(args.Player.VoiceChannel.Id, out var value))
                return;

            if (value.IsCancellationRequested)
                return;

            value.Cancel(true);
        }

        private async Task InitiateDisconnectAsync(LavaPlayer player, TimeSpan timeSpan)
        {
            return;
            if (!_disconnectTokens.TryGetValue(player.VoiceChannel.Id, out var value))
            {
                value = new CancellationTokenSource();
                _disconnectTokens.TryAdd(player.VoiceChannel.Id, value);
            }

            else if (value.IsCancellationRequested)
            {
                _disconnectTokens.TryUpdate(player.VoiceChannel.Id, new CancellationTokenSource(), value);
                value = _disconnectTokens[player.VoiceChannel.Id];
            }


            var isCancelled = SpinWait.SpinUntil(() => value.IsCancellationRequested, timeSpan);
            if (isCancelled)
                return;

            await _lavaNode.LeaveAsync(player.VoiceChannel);
            await player.TextChannel.SendMessageAsync("Invite me again sometime, sugar 😉");
        }

        bool leavejoin = true;
        public async Task TrackEnded(TrackEndedEventArgs args)
        {
            loop = _guilds.GetOrCreateGuildAccount(args.Player.TextChannel.GuildId).PlayerShouldLoop;

            if (loop)
            {
                if (args.Track is null || args.Player.Queue is null) return;
                args.Player.Queue.Enqueue(args.Track);
            }

            if (leavejoin)
            {
                if (args.Reason is TrackEndReason.Stopped)
                {
                    if (args.Player.VoiceChannel != null) _ = InitiateDisconnectAsync(args.Player, TimeSpan.FromSeconds(90));
                }
                leavejoin = true;
            }


            if (args.Reason != TrackEndReason.Finished)
                return;

            if (!args.Player.Queue.TryDequeue(out var queueable))
            {
                await args.Player.TextChannel.SendMessageAsync("Queue completed! Please add more tracks for me to play! 🎧");
                _ = InitiateDisconnectAsync(args.Player, TimeSpan.FromSeconds(90));
                return;
            }

            if (!(queueable is LavaTrack track))
                return;

            await args.Player.PlayAsync(track);
        }

        public async Task MessageUpdate(SocketMessage socketMessage)
        {
            if (!(socketMessage is SocketUserMessage message) || message.Author.IsBot || message.Author.IsWebhook || message.Channel is IPrivateChannel)
                return;

            var guild = (message.Channel as SocketGuildChannel).Guild;

            if (message.Content.ToLower() == "mira" || message.Content == guild.CurrentUser.Mention)
            {
                string prefix = _guilds.GetOrCreateGuildAccount(guild.Id).DefaultDisplayPrefix;

                await message.Channel.SendMessageAsync($"My prefix in server: `{prefix}`", embed: EmbedHandler.CreateBasicEmbed(null,
                       $"\nUse the `{prefix}play` command to play music.\n`{prefix}help` for help"));
                return;
            }

            if (names.Contains(message.Author))
            {
                int i = names.IndexOf(message.Author);

                if (message.Content == "1" || message.Content == "2" || message.Content == "3" || message.Content == "4"
                    || message.Content == "5" || message.Content == "6" || message.Content == "7" || message.Content == "8"
                    || message.Content == "9" || message.Content == "10")
                {
                    await Picked((int.Parse(message.Content) - 1), i, message);

                    await messages[i].DeleteAsync();
                    names.RemoveAt(i);
                    querys.RemoveAt(i);
                    messages.RemoveAt(i);

                    return;
                }
                if (message.Content.ToLower() == "cancel")
                {
                    names.Remove(message.Author);
                    querys.RemoveAt(i);
                    await messages[i].DeleteAsync();
                    messages.RemoveAt(i);

                    await message.Channel.SendMessageAsync("✅");
                }
            }
        }

        public async Task Picked(int a, int i, SocketUserMessage userMessage)
        {
            if (a >= querys[i].Count())
                return;

            try
            {
                var guild = (userMessage.Channel as SocketGuildChannel).Guild;
                var user = userMessage.Author as SocketGuildUser;

                if (user.VoiceChannel is null)
                {
                    await userMessage.Channel.SendMessageAsync(embed:
                          EmbedHandler.CreateErrorEmbed("Join", "You must join a voice channel.")); return;
                }

                await _lavaNode.JoinAsync(user.VoiceChannel, userMessage.Channel as ITextChannel);

                var player = _lavaNode.GetPlayer(guild);
                var track = querys[i][a];

                if (player.PlayerState is PlayerState.Playing)
                {
                    player.Queue.Enqueue(track);
                    await userMessage.Channel.SendMessageAsync(embed: EmbedHandler.CreateBasicEmbed("Added to queue", $"[{track.Title}]({track.Url})", user));

                    return;
                }

                await player.PlayAsync(track);
                await userMessage.Channel.SendMessageAsync(embed: EmbedHandler.CreateBasicEmbed("Play",
                   $"Playing 🎶 [{track.Title}]({track.Url})", user));

                return;
            }
            catch (Exception ex)
            {
                await userMessage.Channel.SendMessageAsync(embed: EmbedHandler.CreateErrorEmbed("Search", ex.Message));
                return;
            }

        }
    }
}
