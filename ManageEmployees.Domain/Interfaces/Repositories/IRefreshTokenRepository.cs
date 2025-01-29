using ManageEmployees.Domain.Entities;

namespace ManageEmployees.Domain.Interfaces.Repositories
{
    public interface IRefreshTokenRepository : IRepository<RefreshToken>
    {
        Task<RefreshToken?> GetRefreshTokenByUserId(string id);
    }
}
