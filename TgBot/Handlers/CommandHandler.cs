using Oganesyan_WebAPI.Services;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Types;
using static System.Net.Mime.MediaTypeNames;

namespace Oganesyan_WebAPI.TgBot.Handlers
{
    public class CommandHandler
    {
        private readonly UserService _userService;
        private readonly ExerciseService _exerciseService;
        private readonly ILogger<CommandHandler> _logger;

        public CommandHandler(UserService userService, ExerciseService exerciseService, ILogger<CommandHandler> logger)
        {
            _userService = userService;
            _exerciseService = exerciseService;
            _logger = logger;
        }

        public async Task HandleAsync(ITelegramBotClient telegramBotClient, Message message, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            var userName = message.Chat.Username;
            var text = message.Text ?? string.Empty;
            var parts = text.Split(' ');
            var command = parts[0].ToLower();

            _logger.LogInformation("Команда: {Command} от {UserName}", command, userName);

            switch (command)
            {
                case "/start":
                    {
                        await HandleStart(telegramBotClient, chatId, parts, cancellationToken);
                        break;
                    }
                case "/help":
                    {
                        await HandleHelp(telegramBotClient, chatId, cancellationToken);
                        break;
                    }
                case "/status":
                    {
                        await HandleStatus(telegramBotClient, chatId, cancellationToken);
                        break;
                    }
                case "/exercises":
                    {
                        await SendExercisesList(telegramBotClient, chatId, cancellationToken);
                        break;
                    }
                default:
                    {
                        await telegramBotClient.SendMessage(
                            chatId: chatId,
                            text: "❓ Неизвестная команда. Напиши /help",
                            cancellationToken: cancellationToken
                        );
                        break;
                    }
            }
        }

        private async Task HandleStart(ITelegramBotClient telegramBotClient, long chatId, string[] parts, CancellationToken cancellationToken)
        {
            if (parts.Length > 1)
            {
                var code = parts[1];
                _logger.LogInformation("Попытка привязки с кодом: {Code}, ChatId: {ChatId}", code, chatId);

                var success = await _userService.LinkTelegramAsync(code, chatId);

                if (success)
                {
                    var user = await _userService.GetUserByTelegramChatIdAsync(chatId);

                    await telegramBotClient.SendMessage(
                        chatId: chatId,
                        text: "👋 Привет! Я бот для тренировки SQL.\n\n" +
                                "📚 Доступные команды:\n" +
                                "/exercises — список заданий\n" +
                                "/help — помощь",
                        cancellationToken: cancellationToken

                    );

                    _logger.LogInformation("Успешная привязка! User: {UserName}, ChatId: {ChatId}", user?.UserName, chatId);
                }
                else
                {
                    await telegramBotClient.SendMessage(
                        chatId: chatId,
                        text: "❌ Код недействителен или просрочен.\n\n" +
                              "Получи новый код в профиле на сайте.",
                        cancellationToken: cancellationToken
                    );

                    _logger.LogWarning("Неудачная привязка. Код: {Code}, ChatId: {ChatId}", code, chatId);
                }
                return;
            }

            var existingUser = await _userService.GetUserByTelegramChatIdAsync(chatId);

            if (existingUser != null)
            {
                await telegramBotClient.SendMessage(
                    chatId: chatId,
                    text: $"👋 С возвращением, {existingUser.UserName}!\n\n" +
                          "📚 Команды:\n" +
                          "/exercises — список заданий\n" +
                          "/status — статус аккаунта\n" +
                          "/help — помощь",
                    cancellationToken: cancellationToken
                );
            }
            else
            {
                await telegramBotClient.SendMessage(
                    chatId: chatId,
                    text: "👋 Привет! Я бот для тренировки SQL.\n\n" +
                          "🔗 Для отправки решений привяжи аккаунт:\n" +
                          "1. Зарегистрируйся на сайте\n" +
                          "2. В профиле нажми «Привязать Telegram»",
                    cancellationToken: cancellationToken
                );
            }
        }

        private async Task HandleStatus(ITelegramBotClient bot, long chatId, CancellationToken ct)
        {
            var user = await _userService.GetUserByTelegramChatIdAsync(chatId);

            if (user != null)
            {
                await bot.SendMessage(
                    chatId: chatId,
                    text: $"👤 Аккаунт: {user.UserName}\n" +
                          $"📧 Логин: {user.Login}\n" +
                          $"🔗 Telegram: привязан ✅",
                    cancellationToken: ct
                );
            }
            else
            {
                await bot.SendMessage(
                    chatId: chatId,
                    text: "🔗 Telegram не привязан.\n\n" +
                          "Привяжи аккаунт в профиле на сайте.",
                    cancellationToken: ct
                );
            }
        }

        private async Task HandleHelp(ITelegramBotClient telegramBotClient, long chatId, CancellationToken cancellationToken)
        {
            await telegramBotClient.SendMessage(
                chatId: chatId,
                text: "📚 Команды:\n\n" +
                      "/start — начать\n" +
                      "/exercises — список заданий\n" +
                      "/status — статус аккаунта\n" +
                      "/help — помощь",
                cancellationToken: cancellationToken
            );
        }

        private async Task SendExercisesList(ITelegramBotClient telegramBotClient, long chatId, CancellationToken cancellationToken)
        {
            var exercises = await _exerciseService.GetExercises();

            if (!exercises.Any())
            {
                await telegramBotClient.SendMessage(
                    chatId: chatId,
                    text: "📭 Сейчас нет доступных для решения заданий",
                    cancellationToken: cancellationToken
                );
            }

            var text = "📝 Список заданий:\n" + string.Join("\n", exercises.Select(e => $"• {e.Title}"));
            await telegramBotClient.SendMessage(chatId, text, cancellationToken: cancellationToken);
        }
    }
}
