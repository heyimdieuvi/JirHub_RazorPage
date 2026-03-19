using JirHub.MVCWebApp.ViNTD.Models;
using JirHub.Services.ViNTD.IServices;
using JirHub.MVCWebApp.ViNTD.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JirHub.MVCWebApp.ViNTD.Controllers
{
    // Restrict access to Teachers (Role 2)
    [Authorize(Roles = "2")]
    public class TeacherController : Controller
    {
        private readonly IProjectConfigService _projectConfigService;
        private readonly ILogger<TeacherController> _logger;

        public TeacherController(
            IProjectConfigService projectConfigService,
            ILogger<TeacherController> logger)
        {
            _projectConfigService = projectConfigService;
            _logger = logger;
        }

        /// <summary>
        /// GET: View all group projects with filtering options
        /// </summary>
        public async Task<IActionResult> Index([FromQuery] string searchQuery, [FromQuery] string semesterCode, int? pageNumber)
        {
            try
            {
                // Load filters
                ViewData["CurrentSearch"] = searchQuery;
                ViewData["Semesters"] = await _projectConfigService.GetAllSemestersAsync();
                ViewData["SelectedSemester"] = semesterCode;

                // Get Groups
                var groups = await _projectConfigService.GetAllGroupsWithConfigsAsync(searchQuery);

                if (!string.IsNullOrEmpty(semesterCode))
                {
                    groups = groups.Where(g => g.SemesterCode == semesterCode).ToList();
                }

                int pageSize = 5;
                int pageIndex = pageNumber.HasValue && pageNumber.Value > 0 ? pageNumber.Value : 1;
                return View(PaginatedList<JirHub.Entities.ViNTD.Models.ClassGroup>.Create(groups, pageIndex, pageSize));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in TeacherController Index method.");
                TempData["ErrorMessage"] = "An error occurred while fetching group projects.";
                return View(new PaginatedList<JirHub.Entities.ViNTD.Models.ClassGroup>(new List<JirHub.Entities.ViNTD.Models.ClassGroup>(), 0, 1, 10));
            }
        }
    }
}
