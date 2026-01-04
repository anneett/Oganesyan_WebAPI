using Oganesyan_WebAPI.DTOs;
using Oganesyan_WebAPI.Services;
using Oganesyan_WebAPI.TgBot.Keyboards;
using Oganesyan_WebAPI.TgBot.States;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Oganesyan_WebAPI.TgBot.Handlers
{
    public class MessageHandler
    {
        private readonly CommandHandler _commandHandler;
        private readonly UserService _userService;
        private readonly ExerciseService _exerciseService;
        private readonly SolutionService _solutionService;
        private readonly ILogger<MessageHandler> _logger;

        private static readonly HashSet<string> ButtonTexts = new(StringComparer.OrdinalIgnoreCase)
        {
            "📝 Задания",
            "📊 Профиль",
            "❓ Помощь",
            "🔗 Как привязать?",
            "❓ Посмотреть ответ",
            "📊 Показать статистику по заданию",
            "⬅️ Вернуться на главную"
        };

        public MessageHandler(CommandHandler commandHandler, UserService userService, ExerciseService exerciseService, SolutionService solutionService, ILogger<MessageHandler> logger)
        {
            _commandHandler = commandHandler;
            _userService = userService;
            _exerciseService = exerciseService;
            _solutionService = solutionService;
            _logger = logger;
        }

        public async Task HandleAsync(ITelegramBotClient telegramBotClient, Message message, CancellationToken cancellationToken)
        {
            if (message.Text is null) return;

            var chatId = message.Chat.Id;
            var text = message.Text;

            _logger.LogInformation("Сообщение от {Username}: {Text}", message.Chat.Username, text);

            var session = StateManager.GetOrCreate(chatId);

            if (text == "⬅️ Вернуться на главную")
            {
                StateManager.EndExercise(chatId);
                await telegramBotClient.SendMessage(chatId,
                    "Используй кнопки меню ниже 👇",
                    replyMarkup: ReplyKeyboards.MainMenu,
                    cancellationToken: cancellationToken);

                return;
            }

            if (session.CurrentExerciseId.HasValue)
            {
                await HandleExerciseMode(telegramBotClient, chatId, text, session, cancellationToken);
                return;
            }

            if (text.StartsWith("/") || ButtonTexts.Contains(text))
            {
                await _commandHandler.HandleAsync(telegramBotClient, message, cancellationToken);
            }
            else
            {
                await telegramBotClient.SendMessage(
                    chatId: chatId,
                    text: "❓ Не понимаю. Используй кнопки меню или /help",
                    cancellationToken: cancellationToken
                );
            }
        }

        private async Task HandleExerciseMode(ITelegramBotClient telegramBotClient, long chatId, string text, SessionState session, CancellationToken cancellationToken)
        {
            if (text == "❓ Посмотреть ответ")
            {
                await ShowAnswer(telegramBotClient, chatId, session.CurrentExerciseId!.Value, session, cancellationToken);
                return;
            }

            if (text == "📊 Показать статистику по заданию")
            {
                await ShowExerciseStats(telegramBotClient, chatId, session.CurrentExerciseId!.Value, cancellationToken);
                return;
            }

            await ProcessSqlAnswer(telegramBotClient, chatId, text, session.CurrentExerciseId!.Value, cancellationToken);
        }

        private async Task ProcessSqlAnswer(ITelegramBotClient telegramBotClient, long chatId, string sqlQuery, int exerciseId, CancellationToken cancellationToken)
        {
            var user = await _userService.GetUserByTelegramChatIdAsync(chatId);
            if (user == null)
            {
                await telegramBotClient.SendMessage(chatId, "❌ Ошибка: пользователь не найден", cancellationToken: cancellationToken);
                return;
            }

            await telegramBotClient.SendMessage(chatId, "⏳ Проверяю решение...", cancellationToken: cancellationToken);

            var solutionDto = new SolutionCreateDto
            {
                ExerciseId = exerciseId,
                UserAnswer = sqlQuery
            };

            var solution = await _solutionService.AddSolution(solutionDto, user.Id);

            if (solution == null)
            {
                await telegramBotClient.SendMessage(chatId, "❌ Ошибка при проверке решения", cancellationToken: cancellationToken);
                return;
            }

            if (solution.IsCorrect)
            {
                var text = "✅ <b>Правильно!</b>\n\n" +
                          $"<code>{solution.UserAnswer}</code>\n\n" +
                          "Отличная работа! Можешь переходить к следующему заданию.";

                await telegramBotClient.SendMessage(chatId, text,
                    parseMode: ParseMode.Html,
                    replyMarkup: ReplyKeyboards.ExerciseMenu,
                    cancellationToken: cancellationToken
                    );
            }
            else
            {
                var text = "❌ <b>Неправильно</b>\n\n" +
                          $"<b>Твой ответ:</b>\n<code>{solution.UserAnswer}</code>\n\n" +
                          $"<b>Результат:</b> {solution.Result}\n\n" +
                          "💡 Попробуй ещё раз или посмотри ответ.";

                await telegramBotClient.SendMessage(chatId, text, parseMode: ParseMode.Html, cancellationToken: cancellationToken);

                StateManager.MarkAnswerShown(chatId);
            }
        }

        private async Task ShowAnswer(ITelegramBotClient telegramBotClient, long chatId, int exerciseId, SessionState session, CancellationToken cancellationToken)
        {
            if (!session.ShowedAnswerOnce)
            {
                await telegramBotClient.SendMessage(chatId,
                    "⚠️ Попробуй сначала отправить своё решение!\n" +
                    "Ответ будет доступен после первой попытки.",
                    cancellationToken: cancellationToken);
                return;
            }

            var exercise = await _exerciseService.GetExerciseById(exerciseId);
            if (exercise == null) return;

            var text = $"💡 <b>Правильный ответ:</b>\n\n" +
                      $"<code>{exercise.CorrectAnswer}</code>";

            await telegramBotClient.SendMessage(chatId, text, parseMode: ParseMode.Html, cancellationToken: cancellationToken);
        }

        private async Task ShowExerciseStats(ITelegramBotClient telegramBotClient, long chatId, int exerciseId, CancellationToken cancellationToken)
        {
            var stats = await _solutionService.GetExerciseStatsById(exerciseId);
            if (stats == null || stats.TotalAttempts == 0)
            {
                await telegramBotClient.SendMessage(chatId, "📊 Статистики пока нет", cancellationToken: cancellationToken);
                return;
            }

            var text = $"📊 <b>Статистика задания</b>\n\n" +
                      $"• Всего попыток: <b>{stats.TotalAttempts}</b>\n" +
                      $"• Уникальных пользователей: <b>{stats.UniqueUsers}</b>\n" +
                      $"• Правильных ответов: <b>{stats.CorrectAnswers}</b>\n" +
                      $"• Успешность: <b>{stats.PercentCorrect:F1}%</b>\n\n" +
                      $"📈 <b>В среднем {(double)stats.TotalAttempts / stats.UniqueUsers:F1} попыток на человека</b>";

            await telegramBotClient.SendMessage(chatId, text, parseMode: ParseMode.Html, cancellationToken: cancellationToken);
        }
    }
}
