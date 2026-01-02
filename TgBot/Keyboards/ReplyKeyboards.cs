using Telegram.Bot.Types.ReplyMarkups;

namespace Oganesyan_WebAPI.TgBot.Keyboards
{
    public class ReplyKeyboards
    {
        public static ReplyKeyboardMarkup MainMenu => new(new[]
        {
            new KeyboardButton[] { "📝 Задания", "📊 Статус" },
            new KeyboardButton[] { "❓ Помощь" }
        })
        {
            ResizeKeyboard = true,
            InputFieldPlaceholder = "Выбери команду или напиши сообщение"
        };

        public static ReplyKeyboardMarkup GuestMenu => new(new[]
        {
            new KeyboardButton[] { "📝 Задания" },
            new KeyboardButton[] { "🔗 Как привязать?", "❓ Помощь" }
        })
        {
            ResizeKeyboard = true
        };

        public static ReplyKeyboardRemove Remove => new();
    }
}
