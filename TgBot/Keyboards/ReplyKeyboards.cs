using Telegram.Bot.Types.ReplyMarkups;

namespace Oganesyan_WebAPI.TgBot.Keyboards
{
    public class ReplyKeyboards
    {
        public static ReplyKeyboardMarkup MainMenu => new(new[]
        {
            new KeyboardButton[] { "📝 Задания", "📊 Профиль" },
            new KeyboardButton[] { "❓ Помощь" }
        })
        {
            ResizeKeyboard = true,
            InputFieldPlaceholder = "Выбери команду или напиши сообщение"
        };
        public static ReplyKeyboardMarkup ExerciseMenu => new(new[]
{
            new KeyboardButton[] { "❓ Посмотреть ответ" },
            new KeyboardButton[] { "📊 Показать статистику по заданию" },
            new KeyboardButton[] { "⬅️ Вернуться на главную" }
        })
        {
            ResizeKeyboard = true,
            InputFieldPlaceholder = "Введи ответ на задание"
        };

        public static ReplyKeyboardMarkup GuestMenu => new(new[]
        {
            new KeyboardButton[] { "🔗 Как привязать?", "❓ Помощь" }
        })
        {
            ResizeKeyboard = true
        };

        public static ReplyKeyboardRemove Remove => new();
    }
}
