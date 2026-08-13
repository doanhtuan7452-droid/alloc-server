using AllocServer.DTOs.MLExport;
using AllocServer.Filters;
using AllocServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AllocServer.Controllers
{
    [ApiController]
    [Route("api/v1/ml-export")]
    [Authorize]
    [RequireActiveAccount]
    [RequireSystemAccount] // Secure: only system administrators can export training datasets
    public class MLExportController : ControllerBase
    {
        private readonly MLExportService _exportService;

        public MLExportController(MLExportService exportService)
        {
            _exportService = exportService;
        }

        /// <summary>Xuất dữ liệu huấn luyện nhân sự (Fit & Allocation) dưới dạng JSON phân trang.</summary>
        [HttpGet("personnel-training")]
        [ProducesResponseType(typeof(PagedMLExportResponse<PersonnelTrainingRow>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPersonnelTrainingData(
            [FromQuery] int? workspaceId,
            [FromQuery] int? projectId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 100)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 100;
            if (pageSize > 1000) pageSize = 1000; // Limit page size to prevent memory overload

            var result = await _exportService.GetPersonnelTrainingDataAsync(
                workspaceId,
                projectId,
                startDate,
                endDate,
                page,
                pageSize);

            return Ok(result);
        }

        /// <summary>Xuất dữ liệu huấn luyện rủi ro dự án (Project Risk) dưới dạng JSON phân trang.</summary>
        [HttpGet("project-risk-training")]
        [ProducesResponseType(typeof(PagedMLExportResponse<ProjectRiskTrainingRow>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProjectRiskTrainingData(
            [FromQuery] int? workspaceId,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 100)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 100;
            if (pageSize > 1000) pageSize = 1000;

            var result = await _exportService.GetProjectRiskTrainingDataAsync(
                workspaceId,
                status,
                page,
                pageSize);

            return Ok(result);
        }
    }
}
