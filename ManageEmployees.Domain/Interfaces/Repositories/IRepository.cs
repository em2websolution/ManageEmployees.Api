namespace ManageEmployees.Domain.Interfaces.Repositories
{
    public interface IRepository<TEntity> where TEntity : class
    {
        void Create(TEntity entity);
        void Delete(TEntity entity);
        void Save();
        Task SaveAsync();
        void Dispose();
    }
}
