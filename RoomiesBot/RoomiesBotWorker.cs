using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RoomiesBot.Services;
using Telegram.Bot;
using Telegram.Bot.Args;
using TelegramSettings = RoomiesBot.Settings.Telegram;

namespace RoomiesBot
{
    public class RoomiesBotWorker : BackgroundService
    {
        private readonly ILogger<RoomiesBotWorker> _logger;
        private readonly FlypackService _flypackService;
        private readonly TelegramSettings _settings;
        private readonly TelegramBotClient _client;

        public RoomiesBotWorker(ILogger<RoomiesBotWorker> logger, IOptions<TelegramSettings> settings, FlypackService flypackService)
        {
            _logger = logger;
            _flypackService = flypackService;
            _settings = settings.Value;
            _client = new TelegramBotClient(_settings.AccessToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            //_client.OnMessage += OnMessage;
            //_client.StartReceiving(cancellationToken: stoppingToken);
            await _flypackService.StartAsync(_client, _settings.ChannelIdentifier, stoppingToken);

            //return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _client.StopReceiving();

            return base.StopAsync(cancellationToken);
        }

        #region Bot Event Handlers
        private async void OnMessage(object sender, MessageEventArgs e)
        {
            if (e.Message.Text != null)
            {
                _logger.LogDebug("Received a text message in chat {ChatId}", e.Message.Chat.Id);

                await _client.SendTextMessageAsync(
                  chatId: e.Message.Chat,
                  text: "You said:\n" + e.Message.Text
                );
            }
        }
        #endregion
    }
}