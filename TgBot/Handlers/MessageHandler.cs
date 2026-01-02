using Telegram.Bot;
using Telegram.Bot.Types;

namespace Oganesyan_WebAPI.TgBot.Handlers
{
    public class MessageHandler
    {
        private readonly CommandHandler _commandHandler;
        private readonly ILogger<MessageHandler> _logger;

        private static readonly HashSet<string> ButtonTexts = new(StringComparer.OrdinalIgnoreCase)
        {
            "📝 Задания",
            "📊 Статус",
            "❓ Помощь",
            "🔗 Как привязать?"
        };

        public MessageHandler(CommandHandler commandHandler, ILogger<MessageHandler> logger)
        {
            _commandHandler = commandHandler;
            _logger = logger;
        }

        public async Task HandleAsync(ITelegramBotClient telegramBotClient, Message message, CancellationToken cancellationToken)
        {
            if (message.Text is null)
            {
                _logger.LogInformation("Получено сообщение без текста, пропускаем");
                return;
            }

            var chatId = message.Chat.Id;
            var userName = message.Chat.Username;
            var text = message.Text;

            _logger.LogInformation("Сообщение от {UserName}: {Text}", userName, text);

            if (text.StartsWith("/") || ButtonTexts.Contains(text))
            {
                await _commandHandler.HandleAsync(telegramBotClient, message, cancellationToken);
            }
            else
            {
                await telegramBotClient.SendMessage(
                    chatId: chatId,
                    text: $"Ты написал: {text}\n\n" +
                          "(Пока не умею обрабатывать SQL, используй команды /start или /help)",
                    cancellationToken: cancellationToken
                );
            }
        }
    }
}
