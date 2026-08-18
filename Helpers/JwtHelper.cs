using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using PickPlace.Api.Models;

namespace PickPlace.Api.Helpers
{
    public class JwtHelper
    {
        private readonly IConfiguration _config;

        public JwtHelper(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateToken(User user)
        {
            var jwtKey    = _config["Jwt:Key"]    ?? throw new InvalidOperationException("Jwt:Key tidak dikonfigurasi.");
            var jwtIssuer = _config["Jwt:Issuer"] ?? "PickPlace";
            var expiryHours = int.TryParse(_config["Jwt:ExpiryHours"], out var h) ? h : 8;

            var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var token = new JwtSecurityToken(
                issuer:             jwtIssuer,
                audience:           jwtIssuer,
                claims:             claims,
                expires:            DateTime.UtcNow.AddHours(expiryHours),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>Ambil UserId dari ClaimsPrincipal (hasil JWT).</summary>
        public static int GetUserId(ClaimsPrincipal principal)
        {
            var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                   ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(sub, out var id) ? id : 0;
        }
    }
}
