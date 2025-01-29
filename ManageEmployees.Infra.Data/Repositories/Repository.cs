using Microsoft.EntityFrameworkCore;
using ManageEmployees.Domain.Interfaces.Repositories;
using ManageEmployees.Infra.Data.Contexts;

namespace ManageEmployees.Infra.Data.Repositories
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        protected LoginContext DbContext;
        protected DbSet<TEntity> DbEntity;
        protected Repository(LoginContext context)
        {
            DbContext = context;
            DbEntity = DbContext.Set<TEntity>();
        }

        public void Create(TEntity entity)
        {
            DbEntity.Add(entity);
        }

        public void Delete(TEntity entity)
        {
            DbEntity.Remove(entity);
        }

        public void Dispose()
        {
            DbContext.Dispose();
        }

        public void Save()
        {
            DbContext.SaveChanges();
        }

        public async Task SaveAsync()
        {
            await DbContext.SaveChangesAsync();
        }
    }
}
