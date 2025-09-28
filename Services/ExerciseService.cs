using Oganesyan_WebAPI.Data;
using Oganesyan_WebAPI.Models;

namespace Oganesyan_WebAPI.Services
{
    public class ExerciseService
    {
        private readonly AppDbContext _context;
        public ExerciseService(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddExercise(Exercise exercise)
        {
            _context.Exercises.Add(exercise);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateExercise(Exercise exercise)
        {
            _context.Exercises.Update(exercise);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteExercise(int id)
        {
            var exercise = await _context.Exercises.FindAsync(id);
            if (exercise != null)
            {
                _context.Exercises.Remove(exercise);
                await _context.SaveChangesAsync();
            }
        }
        // добавить задание в базу данных, удалить задание из базы данных, поменять что-либо
    }
}
