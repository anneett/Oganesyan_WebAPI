using Oganesyan_WebAPI.TgBot.Handlers;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Oganesyan_WebAPI.TgBot
{
    public class SQLBotService : BackgroundService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SQLBotService> _logger;

        public SQLBotService(ITelegramBotClient botClient, IServiceProvider serviceProvider, ILogger<SQLBotService> logger)
        {
            _botClient = botClient;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            _botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );

            var me = await _botClient.GetMe(cancellationToken);
            _logger.LogInformation($"Бот {me.FirstName} запущен");

            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();

            _logger.LogInformation($"Получено обновление типа: {update.Type}");

            try
            {
                switch (update.Type)
                {
                    case UpdateType.Message:
                        {
                            var messageHandler = scope.ServiceProvider.GetRequiredService<MessageHandler>();
                            await messageHandler.HandleAsync(botClient, update.Message!, cancellationToken);
                            break;
                        }

                    case UpdateType.CallbackQuery:
                        {
                            var callbackHandler = scope.ServiceProvider.GetRequiredService<CallbackHandler>();
                            await callbackHandler.HandleAsync(botClient, update.CallbackQuery!, cancellationToken);
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обработки обновления");
            }
        }

        private Task HandleErrorAsync(
            ITelegramBotClient botClient,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Ошибка Telegram Bot API");
            return Task.CompletedTask;
        }
    }
}
