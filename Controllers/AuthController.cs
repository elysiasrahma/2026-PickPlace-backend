using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PickPlace.Api.Data;
using PickPlace.Api.Helpers;
using PickPlace.Api.Models;
using System.Security.Cryptography;
using System.Text;

namespace PickPlace.Api.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtHelper _jwt;

        public AuthController(ApplicationDbContext context, JwtHelper jwt)
        {
            _context = context;
            _jwt     = jwt;
        }

        // POST api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == req.Username && u.IsActive);

            if (user == null || !VerifyPassword(req.Password, user.PasswordHash))
                return Unauthorized(new { error = "Username atau password salah." });

            var token = _jwt.GenerateToken(user);

            return Ok(new
            {
                token,
                role      = user.Role,
                userId    = user.Id,
                username  = user.Username,
                expiresAt = DateTime.UtcNow.AddHours(8)
            });
        }

        // GET api/auth/me
        [HttpGet("me")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> Me()
        {
            var userId = JwtHelper.GetUserId(User);
            var user   = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            return Ok(new { user.Id, user.Username, user.Role, user.IsActive });
        }

        // ─── Helpers ────────────────────────────────────────────────────────────

        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private static bool VerifyPassword(string input, string storedHash)
            => HashPassword(input) == storedHash;

        /// <summary>
        /// Helper internal: buat hash password untuk seed / admin create user.
        /// Panggil via: AuthController.CreateHash("password")
        /// </summary>
        public static string CreateHash(string password) => HashPassword(password);
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
