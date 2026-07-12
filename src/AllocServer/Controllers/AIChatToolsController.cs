using System.Collections.Generic;
using System.Threading.Tasks;
using AllocServer.Filters;
using AllocServer.Interfaces.AI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AllocServer.Controllers
{
    [ApiController]
    [Route("api/v1/internal-tools")]
    [RequireInternalToken]
    public class AIChatToolsController : ControllerBase
    {
        private readonly IAIToolDispatcher _dispatcher;

        public AIChatToolsController(IAIToolDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        [HttpPost("create-project")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateProject([FromBody] Dictionary<string, object> arguments)
        {
            Request.Headers.TryGetValue("X-Idempotency-Key", out var idempotencyKey);
            
            var result = await _dispatcher.DispatchAsync("create_project", arguments, idempotencyKey.ToString());
            
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("create-task")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateTask([FromBody] Dictionary<string, object> arguments)
        {
            Request.Headers.TryGetValue("X-Idempotency-Key", out var idempotencyKey);
            
            var result = await _dispatcher.DispatchAsync("create_task", arguments, idempotencyKey.ToString());
            
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("get-project-info")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProjectInfo([FromBody] Dictionary<string, object> arguments)
        {
            var result = await _dispatcher.DispatchAsync("get_project_info", arguments, null);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("get-employee-list")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEmployeeList([FromBody] Dictionary<string, object> arguments)
        {
            var result = await _dispatcher.DispatchAsync("get_employee_list", arguments, null);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("get-employee-detail")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEmployeeDetail([FromBody] Dictionary<string, object> arguments)
        {
            var result = await _dispatcher.DispatchAsync("get_employee_detail", arguments, null);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("get-workspace-projects")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetWorkspaceProjects([FromBody] Dictionary<string, object> arguments)
        {
            var result = await _dispatcher.DispatchAsync("get_workspace_projects", arguments, null);
            return StatusCode(result.StatusCode, result);
        }
    }
}
