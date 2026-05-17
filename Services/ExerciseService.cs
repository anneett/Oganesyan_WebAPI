using Microsoft.EntityFrameworkCore;
using Oganesyan_WebAPI.Data;
using Oganesyan_WebAPI.DTOs;
using Oganesyan_WebAPI.Models;

namespace Oganesyan_WebAPI.Services
{
    public class ExerciseService
    {
        private readonly AppDbContext _context;
        private readonly SolutionService _solutionService;
        private readonly QueryExecutionService _queryExecutionService;

        public ExerciseService(AppDbContext context, SolutionService solutionService, QueryExecutionService queryExecutionService)
        {
            _context = context;
            _solutionService = solutionService;
            _queryExecutionService = queryExecutionService;
        }

        public async Task<Exercise> AddExercise(ExerciseCreateDto exerciseCreateDto)
        {
            if (await _context.Exercises.AnyAsync(e => e.Title == exerciseCreateDto.Title))
                throw new InvalidOperationException("An exercise with this name already exists.");

            if (!Enum.IsDefined(typeof(ExerciseDifficulty), exerciseCreateDto.Difficulty))
                throw new InvalidOperationException("Недопустимая сложность.");

            var exercise = new Exercise
            {
                Title = exerciseCreateDto.Title,
                Difficulty = exerciseCreateDto.Difficulty,
                DatabaseMetaId = exerciseCreateDto.DatabaseMetaId,
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

        public async Task<List<Exercise>> GetExercises(int? databaseMetaId = null)
        {
            var query = _context.Exercises.AsQueryable();
            if (databaseMetaId.HasValue)
            {
                query = query.Where(exercise => exercise.DatabaseMetaId == databaseMetaId.Value);
            }

            return await query
                .OrderBy(exercise => exercise.Title)
                .ToListAsync();
        }

        public async Task<ExerciseStatsDto?> GetExerciseStatsById(int exerciseId)
        {
            return await _solutionService.GetExerciseStatsById(exerciseId);
        }

        public async Task<QueryResultDto> TestQueryAsync(TestQueryDto dto)
        {
            return await _queryExecutionService.ExecutePreviewAsync(dto.DeploymentId, dto.Query);
        }

        public async Task<BatchUploadResultDto> BatchUploadExercises(BatchExerciseUploadDto dto)
        {
            var result = new BatchUploadResultDto { TotalProcessed = dto.Exercises.Count };

            var dbMetaExists = await _context.DatabaseMetas.AnyAsync(dm => dm.Id == dto.DatabaseMetaId);
            if (!dbMetaExists)
                throw new InvalidOperationException($"DatabaseMeta с ID {dto.DatabaseMetaId} не найдена.");

            for (var i = 0; i < dto.Exercises.Count; i++)
            {
                var exercise = dto.Exercises[i];

                try
                {
                    if (await _context.Exercises.AnyAsync(e => e.Title == exercise.Title))
                    {
                        result.SkippedCount++;
                        result.Errors.Add(new BatchUploadErrorDto
                        {
                            LineNumber = i + 1,
                            Title = exercise.Title,
                            ErrorMessage = "Задание с таким названием уже существует (пропущено)"
                        });
                        continue;
                    }

                    var difficulty = exercise.Difficulty ?? dto.DefaultDifficulty ?? ExerciseDifficulty.Medium;

                    if (!Enum.IsDefined(typeof(ExerciseDifficulty), difficulty))
                    {
                        result.FailedCount++;

                        result.Errors.Add(new BatchUploadErrorDto
                        {
                            LineNumber = i + 1,
                            Title = exercise.Title,
                            ErrorMessage = $"Недопустимая сложность: {(int)difficulty}. Разрешены только 1, 2, 3."
                        });

                        continue;
                    }

                    var newExercise = new Exercise
                    {
                        Title = exercise.Title,
                        Difficulty = difficulty,
                        DatabaseMetaId = dto.DatabaseMetaId,
                        CorrectAnswer = exercise.CorrectAnswer
                    };

                    _context.Exercises.Add(newExercise);
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.Errors.Add(new BatchUploadErrorDto
                    {
                        LineNumber = i + 1,
                        Title = exercise.Title,
                        ErrorMessage = ex.Message
                    });
                }
            }

            await _context.SaveChangesAsync();
            return result;
        }
    }
}
