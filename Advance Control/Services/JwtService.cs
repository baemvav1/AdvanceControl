using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace AdvanceControl.Services
{
    // Implementación cliente de ITokenService usando JwtSecurityTokenHandler.
    // Valida tokens firmados simétricamente (HMAC-SHA256). Ajustar según backend.
    public class JwtService : ITokenService
    {
        private readonly TokenOptions _options;
        private readonly byte[] _keyBytes;
        private readonly SigningCredentials _signingCredentials;
        private readonly TokenValidationParameters _validationParameters;

        public JwtService(TokenOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _keyBytes = Encoding.UTF8.GetBytes(_options.SigningKey);

            var securityKey = new SymmetricSecurityKey(_keyBytes);
            _signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            _validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = !string.IsNullOrWhiteSpace(_options.Issuer),
                ValidIssuer = _options.Issuer,
                ValidateAudience = !string.IsNullOrWhiteSpace(_options.Audience),
                ValidAudience = _options.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = securityKey,
                RequireExpirationTime = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2)
            };
        }

        public string GenerateToken(Guid userId, IEnumerable<string>? roles = null)
        {
            var now = DateTime.UtcNow;

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, ((DateTimeOffset)now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            if (roles != null)
            {
                claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
            }

            var jwt = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                notBefore: now,
                expires: now.AddMinutes(_options.ExpiryMinutes),
                signingCredentials: _signingCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }

        public bool ValidateToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            try
            {
                handler.ValidateToken(token, _validationParameters, out var validatedToken);
                return validatedToken is JwtSecurityToken jwt && jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }

        public ClaimsPrincipal? GetPrincipalFromToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            try
            {
                var principal = handler.ValidateToken(token, _validationParameters, out var validatedToken);
                return principal;
            }
            catch
            {
                return null;
            }
        }

        public Guid? GetUserId(string token)
        {
            var principal = GetPrincipalFromToken(token);
            var sub = principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (Guid.TryParse(sub, out var id)) return id;
            return null;
        }

        public IEnumerable<string> GetRoles(string token)
        {
            var principal = GetPrincipalFromToken(token);
            if (principal == null) return Enumerable.Empty<string>();
            return principal.FindAll(ClaimTypes.Role).Select(c => c.Value);
        }
    }
}