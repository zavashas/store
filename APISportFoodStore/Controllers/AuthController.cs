using APISportFoodStore.Helpers;
using APISportFoodStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace APISportFoodStore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly SportFoodStoreDbContext _db;
        private readonly IEmailSender _email;

        private readonly EmailSettings _settings;

        public AuthController(
             SportFoodStoreDbContext db,
             IEmailSender email,
             IOptions<EmailSettings> settings)
        {
            _db = db;
            _email = email;
            _settings = settings.Value;
        }


        // POST: api/Auth/request-password-reset
        [HttpPost("request-password-reset")]
        public async Task<IActionResult> RequestPasswordReset([FromBody] RequestResetDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.EmailOrLogin))
                return BadRequest("Укажите Email или логин.");

            var login = dto.EmailOrLogin.Trim().ToLowerInvariant();

            var user = await _db.Users
                .FirstOrDefaultAsync(u => (u.Email == login) && !u.Deleted);

            if (user == null)
                return Ok(new { sent = true });

            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');

            user.ResetToken = token;
            user.ResetTokenExpires = DateTime.UtcNow.AddHours(1);
            await _db.SaveChangesAsync();

            var siteUrl = _settings.Branding.SiteUrl;  
            var link = $"{siteUrl.TrimEnd('/')}/Account/ResetPassword?token={user.ResetToken}&email={Uri.EscapeDataString(user.Email ?? "")}";


            await _email.SendPasswordResetAsync(
                toEmail: user.Email!,
                fullName: user.Name ?? "Клиент",
                resetLink: link);

            return Ok(new { sent = true });
        }

        // POST: api/Auth/reset-password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Token) ||
                string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest("Не все поля заполнены.");

            var email = dto.Email.Trim().ToLowerInvariant();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email && !u.Deleted);
            if (user == null)
                return BadRequest("Неверные данные.");

            if (string.IsNullOrEmpty(user.ResetToken) ||
                user.ResetToken != dto.Token ||
                user.ResetTokenExpires == null ||
                user.ResetTokenExpires < DateTime.UtcNow)
                return BadRequest("Ссылка для сброса недействительна или истекла.");

            user.PasswordHash = PasswordHelper.Hash(dto.NewPassword);

            // очистить токен
            user.ResetToken = null;
            user.ResetTokenExpires = null;

            await _db.SaveChangesAsync();
            return Ok(new { ok = true });
        }

    }

    public class RequestResetDto
    {
        public string EmailOrLogin { get; set; } = "";
    }

    public class ResetPasswordDto
    {
        public string Email { get; set; } = "";
        public string Token { get; set; } = "";
        public string NewPassword { get; set; } = "";
    }
}
