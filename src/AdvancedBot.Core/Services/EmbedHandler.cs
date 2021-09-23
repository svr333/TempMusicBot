using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedBot.Core.Services
{
    public static class EmbedHandler
    {      
        public static Embed CreateBasicEmbed(string title, string description, SocketGuildUser user)
        {
            Color color = SetColor();

            var embed = new EmbedBuilder()
                .WithTitle(title)
                .WithDescription(description)
                .WithColor(color)
                .WithFooter(user.Username + "#" + user.Discriminator, user.GetAvatarUrl()).Build();
            return embed;
        }

        public static Embed CreateBasicEmbed(string title, string description)
        {
            Color color = SetColor();

            var embed =  new EmbedBuilder()
                .WithTitle(title)
                .WithDescription(description)
                .WithColor(color).Build();
            return embed;
        }

        public static async Task<Embed> CreateLyricsEmbed(string title, string description, SocketGuildUser user, string thumburl)
        {
            Color color = SetColor();

            var embed = await Task.Run(() => new EmbedBuilder()
                .WithTitle(title)
                .WithDescription(description)
                .WithColor(color)
                .WithThumbnailUrl(thumburl)
                .WithFooter("Request by " + user.Username + "#" + user.Discriminator, user.GetAvatarUrl()).Build());
            return embed;
        }

        public static Embed CreateErrorEmbed(string source, string error)
            => new EmbedBuilder()
                .WithTitle($"{source}")
                .WithDescription($"{error}")
                .WithColor(Color.Red).Build();

        static readonly Random n = new Random();

        public static async Task<Embed> CreateUserEmbed(SocketGuildUser user)
        {
            var roles = new StringBuilder();

            foreach (var socketRole in user.Roles)
            {
                if (socketRole.Name != "@everyone")
                    roles.Append($"{socketRole.Mention}\n");
            }

            var role = roles.ToString();
            role = role == "" ? "`None`" : role;

            string status = user.Status switch
            {
                UserStatus.Offline => "\\⚫️",
                UserStatus.Online => "\\🟢",
                UserStatus.Idle => "\\💤",
                UserStatus.AFK => "\\💤",
                UserStatus.DoNotDisturb => "\\⛔",
                UserStatus.Invisible => "\\⚫️",
                _ => "`None`",
            };

            Color color = SetColor();

            List<EmbedFieldBuilder> fields = new List<EmbedFieldBuilder>
            {
                new EmbedFieldBuilder
                {
                    Name = "Joined Discord",
                    Value = $"`{user.CreatedAt.DateTime.ToString("dd MMM yyyy HH:mm", CultureInfo.CreateSpecificCulture("en-US"))}`",
                    IsInline = true
                },
                new EmbedFieldBuilder
                {
                    Name = "Joined Server",
                    Value = $"`{user.JoinedAt.Value.DateTime.ToString("dd MMM yyyy HH:mm", CultureInfo.CreateSpecificCulture("en-US"))}`",
                    IsInline = true
                },
                new EmbedFieldBuilder
                {
                    Name = "Roles",
                    Value = roles,
                    IsInline = true
                },
                new EmbedFieldBuilder
                {
                    Name = "Status",
                    Value = status,
                    IsInline = true
                }
            };

            var embed = await Task.Run(() => new EmbedBuilder
            {
                Fields = fields,
                ThumbnailUrl = user.GetAvatarUrl(ImageFormat.Auto, 512),
                Footer = new EmbedFooterBuilder { Text = $"{user.Username}#{user.Discriminator}", IconUrl = user.GetAvatarUrl() },
                Color = color
            });

            return embed.Build();
        }

        public static Color SetColor()
        {
            int r = n.Next(0, 256);
            int g = n.Next(0, 256);
            int b = n.Next(0, 256);

            return new Color(r, g, b);
            
        }
    }
}
