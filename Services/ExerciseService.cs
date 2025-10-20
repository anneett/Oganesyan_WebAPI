using Microsoft.EntityFrameworkCore;
using Oganesyan_WebAPI.Data;
using Oganesyan_WebAPI.DTOs;
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
        public async Task<Exercise> AddExercise(ExerciseCreateDto exerciseCreateDto)
        {
            var exercise = new Exercise
            {
                Title = exerciseCreateDto.Title,
                Difficulty = exerciseCreateDto.Difficulty,
                CorrectAnswer = exerciseCreateDto.CorrectAnswer
            };

            _context.Exercises.Add(exercise);
            await _context.SaveChangesAsync();
            return exercise;
        }
        public async Task<List<Exercise>> GetExercises()
        {
            return await _context.Exercises.ToListAsync();
        }
    }
}
