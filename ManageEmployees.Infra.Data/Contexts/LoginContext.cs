using ManageEmployees.Domain.Entities;
using ManageEmployees.Infra.Data.EntityConfig;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ManageEmployees.Infra.Data.Contexts
{
    public class LoginContext : IdentityDbContext<User>
    {
        public LoginContext(DbContextOptions<LoginContext> options) : base(options)
        {
            this.Database.SetCommandTimeout(600);
        }

        #region Properties
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

        #endregion

        #region Methods
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new RefreshTokenConfig());

            base.OnModelCreating(modelBuilder);

            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                var tableName = entity.GetTableName();
                if (tableName != null)
                {
                    entity.SetTableName(tableName.Replace("AspNet", ""));
                }
            }
        }
        #endregion

    }
}
