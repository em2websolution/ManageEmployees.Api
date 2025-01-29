using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using ManageEmployees.Domain.Entities;
using ManageEmployees.Domain.Exceptions;
using ManageEmployees.Domain.Interfaces.Repositories;
using ManageEmployees.Domain.Interfaces.Services;
using ManageEmployees.Domain.Models;
using ManageEmployees.Services.Settings;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ManageEmployees.Services.Services
{
    public class AuthService : IAuthService
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ILogger<AuthService> _logger;
        private readonly UserManager<User> _userManager;
        private readonly JwtSettings _jwtSettings;

        private const string ACCESS_TOKEN_TYPE = "at+jwt";
        private const string TRACE_ID = "TraceId";

        public AuthService(JwtSettings jwtSettings, IRefreshTokenRepository refreshTokenRepository,
            UserManager<User> userManager, ILogger<AuthService> logger)
        {
            _jwtSettings = jwtSettings;
            _refreshTokenRepository = refreshTokenRepository;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<Token> GenerateTokenAsync(string username)
        {
            _logger.LogInformation($"Generating token...");

            var user = await GetUser(username);
            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);
            var claims = await GetClaims(user);
            var roles = await _userManager.GetRolesAsync(user);

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identityClaims = new ClaimsIdentity();

            identityClaims.AddClaims(claims);

            var securityTokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials =
                    new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                NotBefore = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddDays(double.Parse(_jwtSettings.ExpiresAt)),
                IssuedAt = DateTime.UtcNow,
                TokenType = ACCESS_TOKEN_TYPE,
                Subject = identityClaims
            };

            var securityToken = handler.CreateToken(securityTokenDescriptor);
            var encodedJwt = handler.WriteToken(securityToken);
            var refreshToken = await GenerateRefreshToken(user);

            return new Token
            {
                AccessToken = encodedJwt,
                RefreshToken = refreshToken
            };
        }

        public async Task<Token> RefreshTokenSwapAsync(string username, string refreshToken)
        {
            _logger.LogInformation($"Refreshing token...");

            var user = await GetUser(username);

            if (user is null)
                throw new BusinessException(
                    $"{nameof(User)} not found || Method: {nameof(RefreshTokenSwapAsync)} || {TRACE_ID}: {Activity.Current?.Id}");

            var isValid = await ValidateRefreshToken(user.Id, refreshToken);

            if (isValid) return await GenerateTokenAsync(username);

            throw new BusinessException(
                $"Invalid {nameof(RefreshToken)} || Method: {nameof(RefreshTokenSwapAsync)} || {TRACE_ID}: {Activity.Current?.Id}");
        }

        public async Task<bool> RemoveRefreshTokenAsync(string userId)
        {
            var tokenToRemove = await _refreshTokenRepository.GetRefreshTokenByUserId(userId);

            if (tokenToRemove is null) return false;

            _refreshTokenRepository.Delete(tokenToRemove);
            return true;
        }

        private async Task<string> GenerateRefreshToken(User user)
        {
            var randomNumber = new byte[32];
            using var randomNumberGenerator = RandomNumberGenerator.Create();
            randomNumberGenerator.GetBytes(randomNumber);
            var refreshToken = Convert.ToBase64String(randomNumber);
            _refreshTokenRepository.Create(new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = refreshToken,
                UserId = user.Id
            });

            await _refreshTokenRepository.SaveAsync();

            return refreshToken;
        }

        private async Task<User> GetUser(string username)
        {
            var user = await _userManager.FindByNameAsync(username);
            return user ?? throw new Exception("Usuário não encontrado.");
        }

        private async Task<bool> ValidateRefreshToken(string userId, string refreshToken)
        {
            _logger.LogInformation($"Validating existing refresh token...");

            var dbRefresh = await _refreshTokenRepository.GetRefreshTokenByUserId(userId);

            if (dbRefresh is null)
                throw new BusinessException("Refresh token not found");

            if (dbRefresh.Token != refreshToken) return false;

            _refreshTokenRepository.Delete(dbRefresh);
            await _refreshTokenRepository.SaveAsync();
            return true;
        }

        private async Task<IList<Claim>> GetClaims(User user)
        {
            var claims = await _userManager.GetClaimsAsync(user) ?? new List<Claim>();

            claims.Add(new Claim(ClaimTypes.UserData, user.Id));

            return claims;
        }
    }
}
