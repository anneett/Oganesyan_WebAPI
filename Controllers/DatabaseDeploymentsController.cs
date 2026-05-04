using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oganesyan_WebAPI.DTOs;
using Oganesyan_WebAPI.Models;
using Oganesyan_WebAPI.Services;

namespace Oganesyan_WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DatabaseDeploymentsController : ControllerBase
    {
        private readonly DatabaseDeploymentService _databaseDeploymentService;

        public DatabaseDeploymentsController(DatabaseDeploymentService databaseDeploymentService)
        {
            _databaseDeploymentService = databaseDeploymentService;
        }

        [Authorize(Roles = "admin")]
        [HttpGet("{metaId}")]
        public async Task<ActionResult<IEnumerable<DatabaseDeployment>>> GetDatabaseDeploymentsByMetaId(int metaId)
        {
            return await _databaseDeploymentService.GetDeploymentsByMetaIdAsync(metaId);
        }

        [Authorize(Roles = "admin")]
        [HttpPost("deploy/{databaseMetaId}")]
        public async Task<IActionResult> DeployDatabase(int databaseMetaId, [FromBody] DatabaseDeployDto dto)
        {
            try
            {
                var deployment = await _databaseDeploymentService.DeployDatabaseAsync(databaseMetaId, dto);

                return Ok(new
                {
                    deployment.Id,
                    deployment.PhysicaDatabaseName,
                    deployment.IsDeployed
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
