namespace Oganesyan_WebAPI.TgBot.States
{
    public class SessionState
    {
        public long ChatId { get; set; }
        public int? CurrentExerciseId { get; set; }
        public DateTime LastActivity { get; set; }
        public bool ShowedAnswerOnce { get; set; } = false;
    }
    public static class StateManager
    {
        private static readonly Dictionary<long, SessionState> _sessions = new();
        private static readonly object _lock = new();

        public static SessionState GetOrCreate(long chatId)
        {
            lock (_lock)
            {
                if (!_sessions.ContainsKey(chatId))
                {
                    _sessions[chatId] = new SessionState { ChatId = chatId };
                }
                _sessions[chatId].LastActivity = DateTime.UtcNow;
                return _sessions[chatId];
            }
        }

        public static void StartExercise(long chatId, int exerciseId)
        {
            var session = GetOrCreate(chatId);
            session.CurrentExerciseId = exerciseId;
            session.ShowedAnswerOnce = false;
        }

        public static void EndExercise(long chatId)
        {
            var session = GetOrCreate(chatId);
            session.CurrentExerciseId = null;
            session.ShowedAnswerOnce = false;
        }

        public static void MarkAnswerShown(long chatId)
        {
            var session = GetOrCreate(chatId);
            session.ShowedAnswerOnce = true;
        }

        public static void ClearOldSessions()
        {
            lock (_lock)
            {
                var threshold = DateTime.UtcNow.AddHours(-2);
                var oldSessions = _sessions.Where(s => s.Value.LastActivity < threshold)
                    .Select(s => s.Key).ToList();
                foreach (var key in oldSessions)
                {
                    _sessions.Remove(key);
                }
            }
        }
    }
}
