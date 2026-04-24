using Microsoft.CodeAnalysis.Host;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
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
        public async Task<List<Exercise>> GetExercises()
        {
            return await _context.Exercises.ToListAsync();
        }
        public async Task<ExerciseStatsDto?> GetExerciseStatsById(int exerciseId)
        {
            return await _solutionService.GetExerciseStatsById(exerciseId);
        }
        public async Task<QueryResultDto> TestQueryAsync(TestQueryDto dto)
        {
            var deployment = await _context.DatabaseDeployments
                .Include(d => d.DbMeta)
                .FirstOrDefaultAsync(d => d.Id == dto.DeploymentId);

            if (deployment?.DbMeta == null)
                throw new InvalidOperationException("Развертывание не найдено");

            if (!deployment.IsDeployed)
                throw new InvalidOperationException("База данных не развёрнута");

            if (!_queryExecutionService.IsSafeSelectQuery(dto.Query))
            {
                return new QueryResultDto
                {
                    IsCorrect = false,
                    Message = "Разрешены только SELECT-запросы."
                };
            }

            try
            {
                var result = await _queryExecutionService.ExecuteQueryForTestAsync(deployment, dto.Query);
                return result;
            }
            catch (Exception ex)
            {
                return new QueryResultDto
                {
                    IsCorrect = false,
                    Message = "Ошибка выполнения запроса",
                    ErrorDetails = ex.Message
                };
            }
        }

        public async Task<BatchUploadResultDto> BatchUploadExercises(BatchExerciseUploadDto dto)
        {
            var result = new BatchUploadResultDto();
            result.TotalProcessed = dto.Exercises.Count;

            var dbMetaExists = await _context.DatabaseMetas.AnyAsync(dm => dm.Id == dto.DatabaseMetaId);
            if (!dbMetaExists)
            {
                throw new InvalidOperationException($"DatabaseMeta с ID {dto.DatabaseMetaId} не найдена.");
            }

            for (int i = 0; i < dto.Exercises.Count; i++)
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

                    var newExercise = new Exercise
                    {
                        Title = exercise.Title,
                        Difficulty = exercise.Difficulty ?? dto.DefaultDifficulty ?? ExerciseDifficulty.Medium,
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
