using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using APISportFoodStore.Models;
using APISportFoodStore.Helpers;

namespace APISportFoodStore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly SportFoodStoreDbContext _context;

        public UsersController(SportFoodStoreDbContext context)
        {
            _context = context;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users
                                 .Where(u => !u.Deleted)
                                 .ToListAsync();
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int? id)
        {
            var user = await _context.Users
                                     .Where(u => !u.Deleted && u.IdUser == id)
                                     .FirstOrDefaultAsync();

            if (user == null)
                return NotFound();

            return user;
        }

        // PUT: api/Users/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int? id, User dto)
        {
            if (id != dto.IdUser)
                return BadRequest();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.IdUser == id);
            if (user == null)
                return NotFound();

            // нормализация email
            var normalizedEmail = dto.Email?.Trim().ToLowerInvariant();

            // если email меняется — проверяем уникальность среди активных
            if (!string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
            {
                var emailTaken = await _context.Users
                    .AnyAsync(u => u.IdUser != id && u.Email == normalizedEmail);
                if (emailTaken)
                    return Conflict("Пользователь с таким email уже существует");
            }

            // применяем изменения (пример — подставь свои поля)
            user.Email = normalizedEmail;
            user.Surname = dto.Surname;
            user.Name = dto.Name;
            user.MiddleName = dto.MiddleName ?? null;
            user.Phone = dto.Phone;
            user.RoleId = dto.RoleId;
            user.Deleted = dto.Deleted; // если нужно запретить реанимацию при конфликте — см. проверку выше

            // пароль хэшируем, только если пришёл и отличается
            if (!string.IsNullOrWhiteSpace(dto.PasswordHash) && dto.PasswordHash != user.PasswordHash)
            {
                user.PasswordHash = PasswordHelper.Hash(dto.PasswordHash);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                    return NotFound();
                throw;
            }

            return NoContent();
        }


        // POST: api/Users
        [HttpPost]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            user.Deleted = false;

            // Нормализация email
            user.Email = user.Email?.Trim().ToLowerInvariant();

            var emailTaken = await _context.Users.AnyAsync(u => u.Email == user.Email && !u.Deleted);
            if (emailTaken)
                return Conflict("Пользователь с таким email уже существует");

            if (!string.IsNullOrWhiteSpace(user.PasswordHash))
                user.PasswordHash = PasswordHelper.Hash(user.PasswordHash);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUser), new { id = user.IdUser }, user);
        }



        // DELETE: api/Users/5 — логическое удаление
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int? id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || user.Deleted)
                return NotFound();

            user.Deleted = true;
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserExists(int? id)
        {
            return _context.Users.Any(e => e.IdUser == id && !e.Deleted);
        }

        [HttpGet("by-login/{login}")]
        public async Task<ActionResult<User>> GetUserByLogin(string login)
        {
            var user = await _context.Users
                                     .FirstOrDefaultAsync(u => u.Email == login && !u.Deleted);
            if (user == null) return NotFound();
            return user;
        }

        [HttpPost("authenticate")]
        public async Task<ActionResult<User>> Authenticate([FromBody] LoginModel model)
        {
            Console.WriteLine("== POST /Users/authenticate ==");
            Console.WriteLine($"Username: {model.Username}");

            if (string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.Password))
                return BadRequest("Логин и пароль обязательны");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Username && !u.Deleted);
            if (user == null) return Unauthorized();

            var hashed = PasswordHelper.Hash(model.Password);
            if (user.PasswordHash != hashed) return Unauthorized();

            Console.WriteLine("Аутентификация успешна.");
            return Ok(user);
        }

        [HttpPost("register")]
        public async Task<ActionResult<User>> Register([FromBody] User user)
        {
            Console.WriteLine("== POST /Users/register ==");

            user.Email = user.Email?.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(user.Email) || string.IsNullOrWhiteSpace(user.PasswordHash))
                return BadRequest("Email и пароль обязательны");

            var emailTaken = await _context.Users.AnyAsync(u => u.Email == user.Email && !u.Deleted);
            if (emailTaken)
                return Conflict("Пользователь с таким email уже существует");

            if (!IsStrongPassword(user.PasswordHash))
                return BadRequest("Пароль слишком простой. Он должен содержать не менее 6 символов, включая заглавную букву, цифру и специальный символ.");

            if (user.RoleId == 0)
                user.RoleId = 1;

            user.Deleted = false;
            user.PasswordHash = PasswordHelper.Hash(user.PasswordHash);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            Console.WriteLine("Пользователь успешно зарегистрирован.");
            return Ok(user);
        }

        private bool IsStrongPassword(string password)
        {
            if (password.Length < 6) return false;
            if (!password.Any(char.IsUpper)) return false;
            if (!password.Any(char.IsDigit)) return false;
            if (!password.Any(ch => !char.IsLetterOrDigit(ch))) return false;
            return true;
        }

        [HttpPut("admin-edit/{id}")]
        public async Task<IActionResult> AdminEditUser(int id, User dto, [FromQuery] int requesterId)
        {
            if (id != dto.IdUser)
                return BadRequest("Несовпадение id и dto.IdUser.");

            // кто вызывает
            var requester = await _context.Users.FirstOrDefaultAsync(u => u.IdUser == requesterId && !u.Deleted);
            if (requester == null)
                return Unauthorized("Запросивший пользователь не найден или удалён.");

            // запрет редактировать самого себя
            if (requesterId == id)
                return Forbid("Нельзя редактировать самого себя через этот метод.");

            // цель редактирования
            var target = await _context.Users.FirstOrDefaultAsync(u => u.IdUser == id);
            if (target == null)
                return NotFound("Редактируемый пользователь не найден.");

            if (target.RoleId == 1)
                return Forbid("Нельзя редактировать/удалять пользователя с ролью IdRole = 1.");

            // Нормализуем email
            var normalizedEmail = dto.Email?.Trim().ToLowerInvariant();

            // Если меняем email — проверим уникальность среди активных
            if (!string.Equals(target.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
            {
                var emailTaken = await _context.Users
                    .AnyAsync(u => u.IdUser != id && u.Email == normalizedEmail && !u.Deleted);
                if (emailTaken)
                    return Conflict("Пользователь с таким email уже существует.");
            }

            target.Email = normalizedEmail;
            target.Surname = dto.Surname;
            target.Name = dto.Name;
            target.MiddleName = dto.MiddleName;
            target.Phone = dto.Phone;
            target.RoleId = dto.RoleId;

            target.Deleted = dto.Deleted;

            // Пароль: если пришёл новый (и он не совпадает с хэшем в БД) — хешируем
            if (!string.IsNullOrWhiteSpace(dto.PasswordHash) && dto.PasswordHash != target.PasswordHash)
            {
                target.PasswordHash = PasswordHelper.Hash(dto.PasswordHash);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

    }
}
