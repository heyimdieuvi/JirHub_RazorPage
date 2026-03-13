using System.Security.Claims;
using JirHub.MVCWebApp.ViNTD.Models;
using JirHub.Services.ViNTD.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using JirHub.Services.ViNTD.IServices;

namespace JirHub.MVCWebApp.ViNTD.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        public AccountController(IUserService userService) => _userService = userService;
          
        public IActionResult Index()
        {
            return RedirectToAction("Login");
        }
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string userName, string password)
        {
            try
            {
                var userAccount = await _userService.GetUserAccountAsync(userName, password);

                if (userAccount != null)
                {
                    bool isLeader = userAccount.GroupMembers?.Any(gm => gm.IsLeader == true) ?? false;
                    var claims = new List<Claim>
                                {
                                    new Claim(ClaimTypes.Name, userAccount.Email),
                                    new Claim(ClaimTypes.Role, userAccount.Role.ToString()),
                                    new Claim("IsLeader", isLeader.ToString().ToLower())
                                };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

                    Response.Cookies.Append("UserName", userAccount.FullName);
                    Response.Cookies.Append("Role", userAccount.Role.ToString());
                    Response.Cookies.Append("IsLeader", isLeader.ToString().ToLower());
                    
                    if (userAccount.Role == 2) RedirectToAction("Index", "Teacher");

                    return RedirectToAction("Index", "Home");
                }
            }
            catch (Exception ex)
            {

            }

            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            ModelState.AddModelError("", "Login failure");
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        public async Task<IActionResult> Forbidden()
        {
            return View();
        }
    }
}
