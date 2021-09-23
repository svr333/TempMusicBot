using Discord;
using Discord.WebSocket;
using AdvancedBot.Core.Services.Commands;
using AdvancedBot.Core.Services.DataStorage;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using AdvancedBot.Core.Commands;
using AdvancedBot.Core.Services;
using Victoria;
using Interactivity;

namespace AdvancedBot.Core
{
    public class BotClient
    {
        private DiscordSocketClient _client;
        private CustomCommandService _commands;
        private IServiceProvider _services;
        private LavaNode _lavaNode;
        private LavaLinkAudio _audioService;

        public BotClient(CustomCommandService commands = null, DiscordSocketClient client = null)
        {
            _client = client ?? new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                AlwaysDownloadUsers = true,
                MessageCacheSize = 1000
            });

            _commands = commands ?? new CustomCommandService(new CustomCommandServiceConfig
            {
                CaseSensitiveCommands = false,
                LogLevel = LogSeverity.Info,
                BotInviteIsPrivate = true,
                RepositoryUrl = "https://github.com/svr333/TempMusicBot"
            });
        }

        public async Task InitializeAsync()
        {
            _services = ConfigureServices();
            _lavaNode = _services.GetRequiredService<LavaNode>();
            _audioService = _services.GetRequiredService<LavaLinkAudio>();

            _client.Ready += OnReadyAsync;
            _lavaNode.OnTrackException += _audioService.TrackException;
            _lavaNode.OnTrackEnded += _audioService.TrackEnded;
            _lavaNode.OnTrackStarted += _audioService.TrackStarted;

            _lavaNode.OnLog += LogAsync;
            _client.Log += LogAsync;
            _commands.Log += LogAsync;

            var token = Environment.GetEnvironmentVariable("MusicToken");

            await Task.Delay(10).ContinueWith(t => _client.LoginAsync(TokenType.Bot, "ODg3MzUxMzE0NTU4ODk0MDky.YUC4Tw.Ad7aAuceCLDDpZv7WRLhO2LWhqQ"));
            await _client.StartAsync();

            await _services.GetRequiredService<CommandHandlerService>().InitializeAsync();
            await Task.Delay(-1);
        }

        private async Task LogAsync(LogMessage msg)
            => Console.WriteLine($"{msg.Source}: {msg.Message}");

        private async Task OnReadyAsync()
        {
            await _lavaNode.ConnectAsync();
            await _client.SetGameAsync("Being a bot.");
        }

        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .AddSingleton<CommandHandlerService>()
                .AddSingleton<LiteDBHandler>()
                .AddSingleton<GuildAccountService>()
                .AddSingleton<PaginatorService>()
                .AddSingleton<CommandPermissionService>()
                .AddSingleton<LavaNode>()
                .AddSingleton(new LavaConfig())
                .AddSingleton<LavaLinkAudio>()
                .AddSingleton<InteractivityService>()
                .AddSingleton(new InteractivityConfig { DefaultTimeout = TimeSpan.FromSeconds(45) })
                .BuildServiceProvider();
        }
    }
}
