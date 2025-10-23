using Microsoft.CodeAnalysis.Host;
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
        private readonly SolutionService _solutionService;
        public ExerciseService(AppDbContext context, SolutionService solutionService)
        {
            _context = context;
            _solutionService = solutionService;
        }

        public async Task<Exercise> AddExercise(ExerciseCreateDto exerciseCreateDto)
        {
            if (await _context.Exercises.AnyAsync(e => e.Title == exerciseCreateDto.Title))
                throw new InvalidOperationException("An exercise with this name already exists.");

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
        public async Task<Exercise?> GetExerciseById(int id)
        {
            return await _context.Exercises.FindAsync(id);
        }
        public async Task<List<Exercise>> GetExercises()
        {
            return await _context.Exercises.ToListAsync();
        }
        public async Task<ExerciseStatsDto?> GetExerciseStatsById(int exerciseId)
        {
            return await _solutionService.GetExerciseStatsById(exerciseId);
        }
    }
}
