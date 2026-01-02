using Oganesyan_WebAPI.Services;
using Oganesyan_WebAPI.TgBot.Keyboards;
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
            var command = text.StartsWith("/") ? parts[0].ToLower() : text.ToLower(); ;

            _logger.LogInformation("Обработка: {Command} от {UserName}", command, userName);

            switch (command)
            {
                case "/start":
                    {
                        await HandleStart(telegramBotClient, chatId, parts, cancellationToken);
                        break;
                    }
                case "/exercises":
                case "📝 задания":
                    {
                        await SendExercisesList(telegramBotClient, chatId, cancellationToken);
                        break;
                    }
                case "/status":
                case "📊 статус":
                    {
                        await HandleStatus(telegramBotClient, chatId, cancellationToken);
                        break;
                    }
                case "/help":
                case "❓ помощь":
                    {
                        await HandleHelp(telegramBotClient, chatId, cancellationToken);
                        break;
                    }
                case "🔗 как привязать?":
                    {
                        await HandleHowToLinkAsync(telegramBotClient, chatId, cancellationToken);
                        break;
                    }
                default:
                    {
                        await telegramBotClient.SendMessage(
                            chatId: chatId,
                            text: "❓ Неизвестная команда.  Используй кнопки меню или напиши /help",
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
                        text: $"✅ Привет, {user?.UserName}!\n\n" +
                              "Аккаунт успешно привязан.\n\n" +
                              "Используй кнопки меню ниже 👇",
                        replyMarkup: ReplyKeyboards.MainMenu,
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
                        replyMarkup: ReplyKeyboards.GuestMenu,
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
                          "Используй кнопки меню ниже 👇",
                    replyMarkup: ReplyKeyboards.MainMenu,
                    cancellationToken: cancellationToken
                );
            }
            else
            {
                await telegramBotClient.SendMessage(
                    chatId: chatId,
                    text: "👋 Привет! Я бот для тренировки SQL.\n\n" +
                          "📝 Можешь посмотреть задания\n" +
                          "🔗 Для отправки решений привяжи аккаунт на сайте\n\n" +
                          "Используй кнопки меню ниже 👇",
                    replyMarkup: ReplyKeyboards.GuestMenu,
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
            var user = await _userService.GetUserByTelegramChatIdAsync(chatId);
            await telegramBotClient.SendMessage(
                chatId: chatId,
                text: "📚 Доступные команды:\n\n" +
                      "📝 Задания — список задач\n" +
                      "📊 Статус — информация об аккаунте\n" +
                      "❓ Помощь — эта справка\n\n" +
                      "Или используй команды:\n" +
                      "/start, /exercises, /status, /help",
                replyMarkup: user != null ? ReplyKeyboards.MainMenu : ReplyKeyboards.GuestMenu,
                cancellationToken: cancellationToken
            );
        }

        private async Task HandleHowToLinkAsync(ITelegramBotClient telegramBotClient, long chatId, CancellationToken cancellationToken)
        {
            await telegramBotClient.SendMessage(
                chatId: chatId,
                text: "🔗 Как привязать аккаунт:\n\n" +
                      "1. Зарегистрируйся на сайте\n" +
                      "2. Зайди в свой профиль\n" +
                      "3. Нажми «Привязать Telegram»\n" +
                      "4. Перейди по ссылке — и готово!\n\n" +
                      "После привязки сможешь отправлять решения задач.",
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
