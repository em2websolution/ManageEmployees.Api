using ManageEmployees.Domain.Entities;

namespace ManageEmployees.Domain.Interfaces.Repositories
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByUserIdAsync(string userId);
        Task CreateAsync(RefreshToken token);
        Task DeleteAsync(Guid id);
    }
}
