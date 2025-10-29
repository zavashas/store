using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebAppSportFoodStore.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly IConfiguration _cfg;
        public ChatController(IConfiguration cfg) => _cfg = cfg;

        public IActionResult Index()
        {
            ViewBag.ChatHubUrl = _cfg["ChatHub:Url"] ?? "http://localhost:5198/hubs/chat";
            ViewBag.ApiBase = _cfg["Api:BaseUrl"];
            ViewBag.CurrentUserId =
            User.FindFirst("UserId")?.Value ??
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0";
            return View();
        }
    }
}
