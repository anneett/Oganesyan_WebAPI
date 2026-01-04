using Oganesyan_WebAPI.Models;
using Oganesyan_WebAPI.Services;
using Oganesyan_WebAPI.TgBot.Keyboards;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using static System.Net.Mime.MediaTypeNames;

namespace Oganesyan_WebAPI.TgBot.Handlers
{
    public class CommandHandler
    {
        private readonly UserService _userService;
        private readonly ExerciseService _exerciseService;
        private readonly SolutionService _solutionService;
        private readonly ILogger<CommandHandler> _logger;

        public CommandHandler(UserService userService, ExerciseService exerciseService, SolutionService solutionService, ILogger<CommandHandler> logger)
        {
            _userService = userService;
            _exerciseService = exerciseService;
            _solutionService = solutionService;
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
                        await HandleExercises(telegramBotClient, chatId, cancellationToken);
                        break;
                    }
                case "/profile":
                case "📊 профиль":
                    {
                        await HandleProfile(telegramBotClient, chatId, cancellationToken);
                        break;
                    }
                case "/help":
                case "❓ помощь":
                    {
                        await HandleHelp(telegramBotClient, chatId, cancellationToken);
                        break;
                    }
                case "/binding":
                case "🔗 как привязать?":
                    {
                        await HandleHowToLinkAsync(telegramBotClient, chatId, cancellationToken);
                        break;
                    }
                default:
                    {
                        await telegramBotClient.SendMessage(
                            chatId: chatId,
                            text: "❓ Неизвестная команда. Используй кнопки меню или напиши /help",
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
                              "<b>Аккаунт успешно привязан.</b>\n\n" +
                              "Теперь ты можешь решать задания и смотреть статистику прямо здесь!\n\n" +
                              "Используй кнопки меню ниже 👇",
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
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
                          "Готов снова тренировать SQL?\n" +
                          "Используй кнопки меню ниже 👇",
                    replyMarkup: ReplyKeyboards.MainMenu,
                    cancellationToken: cancellationToken
                );
            }
            else
            {
                await telegramBotClient.SendMessage(
                    chatId: chatId,
                    text: "👋 Привет! Я бот SQL-тренажёра.\n\n" +
                          "Здесь ты можешь решать SQL-задания и отслеживать свой прогресс.\n\n" +
                          "🔗 Для начала работы привяжи аккаунт — нажми кнопку 👇",
                    replyMarkup: ReplyKeyboards.GuestMenu,
                    cancellationToken: cancellationToken
                );
            }
        }

        private async Task HandleProfile(ITelegramBotClient bot, long chatId, CancellationToken cancellationToken)
        {
            var user = await _userService.GetUserByTelegramChatIdAsync(chatId);

            if (user == null)
            {
                await bot.SendMessage(
                    chatId: chatId,
                    text: "🔗 Telegram не привязан к аккаунту.\n\n" +
                          "Привяжи аккаунт, чтобы видеть свой профиль и статистику.",
                    replyMarkup: ReplyKeyboards.GuestMenu,
                    cancellationToken: cancellationToken
                );
                return;
            }

            var solutions = await _solutionService.GetUserSolutionsDetailed(user.Id);
            var solutionsList = solutions.ToList();

            var totalAttempts = solutionsList.Count;
            var correctAnswers = solutionsList.Count(s => s.IsCorrect);
            var uniqueExercises = solutionsList.Select(s => s.ExerciseId).Distinct().Count();
            var successRate = totalAttempts > 0 ? Math.Round((double)correctAnswers / totalAttempts * 100, 1) : 0;

            var allExercises = await _exerciseService.GetExercises();
            var totalExercises = allExercises.Count();

            var solvedExercises = solutionsList
                .Where(s => s.IsCorrect)
                .Select(s => s.ExerciseId)
                .Distinct()
                .Count();

            var progressPercent = totalExercises > 0 ? Math.Round((double)solvedExercises / totalExercises * 100, 1) : 0;

            var role = user.IsAdmin ? "Администратор" : "Пользователь";

            var text = $"👤 <b>Твой профиль</b>\n\n" +
              $"<b>Имя:</b> {user.UserName}\n" +
              $"<b>Логин:</b> {user.Login}\n" +
              $"<b>Роль:</b> {role}\n\n" +
              $"━━━━━━━━━━━━━━━━━━\n\n" +
              $"📈 <b>Статистика</b>\n\n" +
              $"Всего попыток: <b>{totalAttempts}</b>\n" +
              $"Правильных: <b>{correctAnswers}</b>\n" +
              $"Уникальных заданий: <b>{uniqueExercises}</b>\n\n" +
              $"<b>Точность ответов:</b> {successRate}%\n" +
              $"<b>Прогресс:</b> {solvedExercises} из {totalExercises} заданий ({progressPercent}%)\n";

            await bot.SendMessage(chatId, text, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, cancellationToken: cancellationToken);
        }
        private async Task HandleExercises(ITelegramBotClient telegramBotClient, long chatId, CancellationToken cancellationToken)
        {
            var user = await _userService.GetUserByTelegramChatIdAsync(chatId);

            if (user == null)
            {
                await telegramBotClient.SendMessage(
                    chatId: chatId,
                    text: "🔗 Для просмотра заданий привяжи аккаунт.\n\n" +
                          "Нажми «Как привязать?» чтобы узнать подробнее.",
                    replyMarkup: ReplyKeyboards.GuestMenu,
                    cancellationToken: cancellationToken
                );
                return;
            }

            var exercises = await _exerciseService.GetExercises();
            var exercisesList = exercises.ToList();

            if (!exercisesList.Any())
            {
                await telegramBotClient.SendMessage(
                    chatId: chatId,
                    text: "📭 Заданий пока нет.\n\nЗагляни позже!",
                    cancellationToken: cancellationToken
                );
                return;
            }

            var solutions = await _solutionService.GetUserSolutionsDetailed(user.Id);
            var solvedIds = solutions
                .Where(s => s.IsCorrect)
                .Select(s => s.ExerciseId)
                .Distinct()
                .ToHashSet();

            var solvedCount = exercisesList.Count(e => solvedIds.Contains(e.Id));

            var text = "📝 <b>Список заданий</b>\n";
            text += $"☑️ Решено: <b>{solvedCount}</b> из <b>{exercisesList.Count}</b>\n\n";

            text += $"Выбери задание, чтобы начать:\n";
            
            var buttons = exercisesList.Select(e =>
            {
                var isSolved = solvedIds.Contains(e.Id);
                var status = isSolved ? "☑️" : "⬜";
                var difficulty = CallbackHandler.GetDifficultyEmoji(e.Difficulty);

                return new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        $"{status} {difficulty} {e.Title}",
                        $"ex_{e.Id}"
                    )
                };
            });

            var keyboard = new InlineKeyboardMarkup(buttons);

            await telegramBotClient.SendMessage(chatId, text, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, replyMarkup: keyboard, cancellationToken: cancellationToken);
        }

        private async Task HandleHelp(ITelegramBotClient telegramBotClient, long chatId, CancellationToken cancellationToken)
        {
            var user = await _userService.GetUserByTelegramChatIdAsync(chatId);

            await telegramBotClient.SendMessage(
                chatId: chatId,
                text: "ℹ️ <b>SQL-тренажёр — Помощь</b>\n\n" +
                          "<b>Этот бот позволяет:</b>\n\n" +
                          "• Просматривать свой профиль\n" +
                          "• Отслеживать свой прогресс\n" +
                          "• Видеть список заданий и решать их\n\n" +
                          "━━━━━━━━━━━━━━━━━━\n\n" +
                          "<b>Доступные команды:</b>\n\n" +
                          "📝 <b>Задания</b> — список всех задач\n" +
                          "👤 <b>Профиль</b> — твоя статистика\n" +
                          "❓ <b>Помощь</b> — эта справка\n\n" +
                          "━━━━━━━━━━━━━━━━━━\n\n" +
                          "💡 <i>Для расширенных возможностей и администрирования используй сайт</i>",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                replyMarkup: user != null ? ReplyKeyboards.MainMenu : ReplyKeyboards.GuestMenu,
                cancellationToken: cancellationToken
            );
        }

        private async Task HandleHowToLinkAsync(ITelegramBotClient telegramBotClient, long chatId, CancellationToken cancellationToken)
        {
            await telegramBotClient.SendMessage(
                chatId: chatId,
                text: "🔗 <b>Как привязать аккаунт</b>\n\n" +
                      "Чтобы использовать бота, нужно связать его с твоим аккаунтом на сайте.\n\n" +
                      "<b>Инструкция:</b>\n\n" +
                      "1️⃣ Зарегистрируйся на сайте\n" +
                      "2️⃣ Зайди в свой профиль\n" +
                      "3️⃣ Найди раздел «Telegram»\n" +
                      "4️⃣ Нажми «Привязать Telegram»\n" +
                      "5️⃣ Перейди по ссылке — готово! ✅\n\n" +
                      "━━━━━━━━━━━━━━━━━━\n\n" +
                      "После привязки ты сможешь:\n" +
                      "• Решать задания\n" +
                      "• Видеть прогресс по заданиям\n" +
                      "• Смотреть свою статистику",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                replyMarkup: ReplyKeyboards.GuestMenu,
                cancellationToken: cancellationToken
            );
        }
    }
}
