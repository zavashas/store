using APISportFoodStore.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static System.Net.WebRequestMethods;

namespace WebAppSportFoodStore.Controllers
{
    public class AccountController : Controller
    {
        private readonly HttpClient _httpClient;

        public AccountController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("Api");
        }

        public IActionResult Authorization() => View();

        [HttpPost]
        public async Task<IActionResult> Authorization(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Введите логин и пароль.");
                return View();
            }

            var response = await _httpClient.PostAsJsonAsync("Users/authenticate", new { username, password });

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Неверный логин или пароль.");
                return View();
            }

            var user = await response.Content.ReadFromJsonAsync<User>();
            if (user == null)
            {
                ModelState.AddModelError("", "Ошибка при получении данных.");
                return View();
            }

            string roleName = "Customer";
            var roleResponse = await _httpClient.GetAsync($"Roles/{user.RoleId}");
            if (roleResponse.IsSuccessStatusCode)
            {
                var role = await roleResponse.Content.ReadFromJsonAsync<Role>();
                if (role != null && !string.IsNullOrEmpty(role.Name))
                    roleName = role.Name;
            }

            var claims = new List<Claim>
            {
                new Claim("UserId", user.IdUser?.ToString() ?? "0"),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.IdUser?.ToString() ?? "0"),
                new Claim(ClaimTypes.Role, roleName)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(User user, string ConfirmPassword)
        {
            if (user.PasswordHash != ConfirmPassword)
            {
                ModelState.AddModelError("", "Пароли не совпадают.");
                return View(user);
            }

            user.IdUser = null;
            user.RoleId = 1;
            user.Deleted = false;

            var response = await _httpClient.PostAsJsonAsync("Users/register", user);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Ошибка регистрации: {error}");
                return View(user);
            }

            return RedirectToAction("Authorization");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Authorization");
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string emailOrLogin)
        {
            var payload = new { EmailOrLogin = emailOrLogin ?? "" };
            var resp = await _httpClient.PostAsJsonAsync("Auth/request-password-reset", payload);
            // всегда показываем один и тот же ответ (без утечки существования пользователя)
            TempData["ForgotSent"] = true;
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
                return BadRequest("Некорректная ссылка.");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string token, string email, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
                ModelState.AddModelError("", "Некорректная ссылка.");
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                ModelState.AddModelError("", "Пароль должен быть не короче 6 символов.");
            if (newPassword != confirmPassword)
                ModelState.AddModelError("", "Пароли не совпадают.");

            if (!ModelState.IsValid) return View();

            var payload = new { Email = email, Token = token, NewPassword = newPassword };
            var resp = await _httpClient.PostAsJsonAsync("Auth/reset-password", payload);
            if (!resp.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Ссылка недействительна или истекла.");
                return View();
            }

            TempData["ResetOk"] = true;
            return View();
        }
    }
}
