using Microsoft.EntityFrameworkCore;
using Oganesyan_WebAPI.Data;
using Oganesyan_WebAPI.Models;
using System.Collections.Specialized;

namespace Oganesyan_WebAPI.Services
{
    public class ExerciseService
    {
        private readonly AppDbContext _context;
        public ExerciseService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Exercise?> GetExerciseById(int id)
        {
            return await _context.Exercises.FindAsync(id);
        }
        public async Task<Exercise> AddExercise(string title, ExerciseDifficulty difficulty, string correctAnswer)
        {
            var exercise = new Exercise
            {
                Title = title,
                Difficulty = difficulty,
                CorrectAnswer = correctAnswer
            };

            _context.Exercises.Add(exercise);
            await _context.SaveChangesAsync();
            return exercise;
        }
        public async Task<List<Exercise>> GetExercises()
        {
            return await _context.Exercises.ToListAsync();
        }

        //public async Task<Exercise> UpdateExercise(Exercise exercise)
        //{
        //    _context.Exercises.Update(exercise);
        //    await _context.SaveChangesAsync();
        //    return exercise;
        //}
        //public async Task<bool> DeleteExercise(int id)
        //{
        //    var exercise = await _context.Exercises.FindAsync(id);
        //    if (exercise != null)
        //    {
        //        _context.Exercises.Remove(exercise);
        //        await _context.SaveChangesAsync();
        //    }
        //    return true;
        //}
    }
}
