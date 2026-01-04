using Oganesyan_WebAPI.Models;
using Oganesyan_WebAPI.Services;
using Oganesyan_WebAPI.TgBot.Keyboards;
using Oganesyan_WebAPI.TgBot.States;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Oganesyan_WebAPI.TgBot.Handlers
{
    public class CallbackHandler
    {
        private readonly ExerciseService _exerciseService;
        private readonly UserService _userService;
        private readonly ILogger<CallbackHandler> _logger;

        public CallbackHandler(ExerciseService exerciseService, UserService userService, ILogger<CallbackHandler> logger)
        {
            _exerciseService = exerciseService;
            _userService = userService;
            _logger = logger;
        }

        public async Task HandleAsync(ITelegramBotClient telegramBotClient, CallbackQuery callback, CancellationToken cancellationToken)
        {
            var chatId = callback.Message?.Chat.Id;
            if (!chatId.HasValue) return;

            var data = callback.Data ?? string.Empty;
            _logger.LogInformation("Callback от {ChatId}: {Data}", chatId, data);

            await telegramBotClient.AnswerCallbackQuery(callback.Id, cancellationToken: cancellationToken);

            if (data.StartsWith("ex_"))
            {
                var exerciseId = int.Parse(data.Replace("ex_", ""));
                await ShowExercise(telegramBotClient, chatId.Value, exerciseId, cancellationToken);
            }
        }

        private async Task ShowExercise(ITelegramBotClient telegramBotClient, long chatId, int exerciseId, CancellationToken cancellationToken)
        {
            var exercise = await _exerciseService.GetExerciseById(exerciseId);
            if (exercise == null)
            {
                await telegramBotClient.SendMessage(chatId, "❌ Задание не найдено", cancellationToken: cancellationToken);
                return;
            }

            StateManager.StartExercise(chatId, exerciseId);

            var difficulty = GetDifficultyEmoji(exercise.Difficulty);
            var difficultyText = GetDifficultyText(exercise.Difficulty);

            await telegramBotClient.SendMessage(chatId,
                text: $"📝 <b>Задание #{exerciseId}</b>\n\n" +
                      $"{difficulty} <i>{difficultyText}</i>\n\n" +
                      $"<b>{exercise.Title}</b>\n\n" +
                      $"━━━━━━━━━━━━━━━━━━\n\n" +
                      $"💡 Напиши SQL-запрос для решения задания.\n" +
                      $"Используй кнопки ниже для помощи 👇",
                parseMode: ParseMode.Html,
                replyMarkup: ReplyKeyboards.ExerciseMenu,
                cancellationToken: cancellationToken
             );
        }
        public static string GetDifficultyEmoji(ExerciseDifficulty difficulty)
        {
            return difficulty switch
            {
                ExerciseDifficulty.Easy => "🟢",
                ExerciseDifficulty.Medium => "🟡",
                ExerciseDifficulty.Hard => "🔴",
                _ => "⚪"
            };
        }
        private static string GetDifficultyText(ExerciseDifficulty difficulty)
        {
            return difficulty switch
            {
                ExerciseDifficulty.Easy => "Легкая",
                ExerciseDifficulty.Medium => "Средняя",
                ExerciseDifficulty.Hard => "Сложная",
                _ => "Неизвестно"
            };
        }
    }
}
