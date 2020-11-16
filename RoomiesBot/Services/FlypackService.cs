using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flypack;
using Flypack.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using FlypackSettings = RoomiesBot.Settings.Flypack;

namespace RoomiesBot.Services
{
    public class FlypackService
    {
        private readonly ILogger<FlypackService> _logger;
        private readonly FlypackScrapper _flypack;
        private readonly FlypackSettings _settings;
        private List<Package> _currentPackages = new List<Package>();
        private Dictionary<string, Package> _previousPackages = new Dictionary<string, Package>();

        public FlypackService(ILogger<FlypackService> logger, FlypackScrapper flypack, IOptions<FlypackSettings> options)
        {
            _logger = logger;
            _flypack = flypack;
            _settings = options.Value;
        }

        public async Task StartAsync(TelegramBotClient client, string channelIdentifier, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Login into Flypack with account: {account}", _settings.Username);
            var path = _flypack.Login(_settings.Username, _settings.Password);

            if (string.IsNullOrEmpty(path))
            {
                _logger.LogWarning("Packages path is empty for account: {account}", _settings.Username);
                await client.SendTextMessageAsync(
                  chatId: channelIdentifier,
                  text: $"Packages path is empty for account: *{_settings.Username}*",
                  parseMode: ParseMode.MarkdownV2
                );
                return;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Fetch running at: {time}", DateTimeOffset.Now);
                var packages = ListPackages(path);
                if (packages.Any())
                {
                    var message = ParseMessageFor(packages);
                    await client.SendTextMessageAsync(
                      chatId: channelIdentifier,
                      text: message,
                      parseMode: ParseMode.Markdown
                    );
                }
                await Task.Delay(TimeSpan.FromMinutes(_settings.FetchInterval), cancellationToken);
            }
        }

        private List<Package> ListPackages(string path)
        {
            var newPackages = _flypack.GetPackages(path);
            var updatedPackages = newPackages.Except(_currentPackages).ToList();
            _previousPackages = _currentPackages.ToDictionary(x => x.Identifier);
            _currentPackages = newPackages.ToList();

            if (updatedPackages.Any())
                _logger.LogInformation("Found {PackagesCount} new packages", updatedPackages.Count);
            else
                _logger.LogInformation("No new packages were found");

            return updatedPackages;
        }

        private string ParseMessageFor(List<Package> packages)
        {
            List<string> messages = new List<string>();
            messages.Add("*Packages Status*\n");

            foreach (var package in packages)
            {
                var description = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(package.Description.ToLower());
                messages.Add($"*Id*: {package.Identifier}");
                messages.Add($"*Description*: {description}");
                messages.Add($"*Tracking*: {package.TrackingInformation}");

                var previousStatus = _previousPackages.ContainsKey(package.Identifier)
                    ? _previousPackages[package.Identifier].Status
                    : package.Status;
                if (previousStatus != package.Status)
                    messages.Add($"*Status*: {previousStatus.Description} → {package.Status.Description}, _{package.Status.Percentage}_");
                else
                    messages.Add($"*Status*: {package.Status.Description}, _{package.Status.Percentage}_");
                messages.Add("");
            }

            messages.RemoveAt(messages.Count - 1);
            return string.Join('\n', messages);
        }
    }
}
