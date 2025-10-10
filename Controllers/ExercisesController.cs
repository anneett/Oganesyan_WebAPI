using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oganesyan_WebAPI.Data;
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

        // GET: api/Exercises/get-exercise/{id}
        [HttpGet("get-exercise/{id}")]
        public async Task<ActionResult<Exercise>> GetExercise(int id)
        {
            var exercise = await _exerciseService.GetExerciseById(id);
            if (exercise == null)
            {
                return NotFound();
            }

            return Ok(exercise);
        }

        // POST: api/Exercises/add-exercise
        [HttpPost("add-exercise")]
        public async Task<ActionResult<Exercise>> AddExersice(string title, ExerciseDifficulty difficulty, string correctAnswer)
        {
            var exercise = await _exerciseService.AddExercise(title, difficulty, correctAnswer);
            return Ok(exercise);
        }

        // GET: api/Exercises/get-exercise
        [HttpGet("get-exercise")]
        public async Task<ActionResult<IEnumerable<Exercise>>> GetExercises()
        {
            return await _exerciseService.GetExercises();
        }

        //// PUT: api/Exercises/{id}
        //[HttpPut("{id}")]
        //public async Task<IActionResult> UpdateExercise(int id, Exercise exercise)
        //{
        //    if (id != exercise.Id)
        //    {
        //        return BadRequest();
        //    }
        //    try
        //    {
        //        //exercise.Id = id;
        //        await _exerciseService.UpdateExercise(exercise);
        //    }
        //    catch (Exception)
        //    {
        //        return NotFound();
        //    }
        //
        //    return NoContent();
        //}

        //// DELETE: api/Exercises/{id}
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> DeleteExercise(int id)
        //{
        //    var exercise = await _exerciseService.GetExerciseById(id);
        //    if (exercise == null)
        //    {
        //        return NotFound();
        //    }
        //    await _exerciseService.DeleteExercise(id);
        //    return NoContent();
        //}
    }
}
