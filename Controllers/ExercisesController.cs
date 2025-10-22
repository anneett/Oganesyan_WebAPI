using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oganesyan_WebAPI.Data;
using Oganesyan_WebAPI.DTOs;
using Oganesyan_WebAPI.Models;
using Oganesyan_WebAPI.Services;

namespace Oganesyan_WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExercisesController : ControllerBase
    {
        private readonly ExerciseService _exerciseService;

        public ExercisesController(ExerciseService exerciseService)
        {
            _exerciseService = exerciseService;
        }

        [Authorize(Roles = "admin")]
        [HttpPost("add-exercise")]
        public async Task<ActionResult<Exercise>> AddExersice([FromBody] ExerciseCreateDto exerciseCreateDto)
        {
            var exercise = await _exerciseService.AddExercise(exerciseCreateDto);
            return CreatedAtAction(nameof(GetExerciseById), new { id = exercise.Id }, exercise);
        }

        [Authorize]
        [HttpGet("get-exercise/{id}")]
        public async Task<ActionResult<Exercise>> GetExerciseById(int id)
        {
            var exercise = await _exerciseService.GetExerciseById(id);
            if (exercise == null)
            {
                return NotFound();
            }

            return Ok(exercise);
        }

        [Authorize]
        [HttpGet("get-exercises")]
        public async Task<ActionResult<IEnumerable<Exercise>>> GetExercises()
        {
            return await _exerciseService.GetExercises();
        }

        [Authorize]
        [HttpGet("get-percent/{id}")]
        public async Task<ActionResult<double>> PercentCorrect(int id)
        {
            return await _exerciseService.PercentCorrect(id);
        }
    }
}
