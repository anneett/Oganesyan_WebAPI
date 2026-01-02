using Telegram.Bot;
using Telegram.Bot.Types;

namespace Oganesyan_WebAPI.TgBot.Handlers
{
    public class CallbackHandler
    {
        private readonly ILogger<CallbackHandler> _logger;

        public CallbackHandler(ILogger<CallbackHandler> logger)
        {
            _logger = logger;
        }

        public async Task HandleAsync(ITelegramBotClient telegramBotClient, CallbackQuery callback, CancellationToken cancellationToken)
        {
            var chatId = callback.Message?.Chat.Id;
            var userName = callback.Message?.Chat.Username;
            var data = callback.Data ?? string.Empty;

            _logger.LogInformation("Callback от {ChatId}: {Data}", chatId, data);

            await telegramBotClient.AnswerCallbackQuery(callback.Id, cancellationToken: cancellationToken);

            if (chatId.HasValue)
            {
                await telegramBotClient.SendMessage(
                    chatId: chatId.Value,
                    text: $"🔘 Нажата кнопка: {data}",
                    cancellationToken: cancellationToken
                );
            }
        }
    }
}
