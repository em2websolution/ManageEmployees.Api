using Microsoft.EntityFrameworkCore;
using ManageEmployees.Domain.Entities;
using ManageEmployees.Domain.Interfaces.Repositories;
using ManageEmployees.Infra.Data.Contexts;

namespace ManageEmployees.Infra.Data.Repositories
{
    public class RefreshTokenRepository : Repository<RefreshToken>, IRefreshTokenRepository
    {
        public RefreshTokenRepository(LoginContext context) : base(context) { }

        public async Task<RefreshToken?> GetRefreshTokenByUserId(string userId) =>
            await DbContext.RefreshTokens.FirstOrDefaultAsync(x => x.UserId.Equals(userId));
    }
}
