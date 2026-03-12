using JirHub.Services.ViNTD.IServices;
using JirHub.Services.ViNTD.Services;
using Microsoft.AspNetCore.Mvc;

namespace JirHub.MVCWebApp.ViNTD.Controllers
{
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService)
        {
            _userService = userService;
        }
        
        public async Task<IActionResult> Index()
        {
            var users = await _userService.GetAllUser();
            return View(users);
        }

    }
}
