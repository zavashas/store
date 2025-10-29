using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebAppSportFoodStore.Controllers
{
    public class SupportController : Controller
    {
        private readonly IConfiguration _cfg;
        public SupportController(IConfiguration cfg) => _cfg = cfg;

        public IActionResult Manager()
        {
            ViewBag.ChatHubUrl = _cfg["ChatHub:Url"] ?? "http://localhost:5198/hubs/chat";
            ViewBag.ApiBase = _cfg["Api:BaseUrl"] ?? "http://localhost:5191";

            ViewBag.AgentId = User.FindFirst("UserId")?.Value
                              ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0";
            ViewBag.IsAdmin = User.IsInRole("Admin");
            return View();
        }
    }
}
