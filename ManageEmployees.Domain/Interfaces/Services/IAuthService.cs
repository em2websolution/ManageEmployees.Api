using ManageEmployees.Domain.Models;

namespace ManageEmployees.Domain.Interfaces.Services
{
    public interface IAuthService
    {
        Task<Token> GenerateTokenAsync(string username);
        Task<Token> RefreshTokenSwapAsync(string username, string refreshToken);
        Task<bool> RemoveRefreshTokenAsync(string userId);
    }
}
