using Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Interfaces;

namespace Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly SftpDbContext _sftpDbContext;
        private readonly DbSet<T> _dbSet;

        public Repository(SftpDbContext sftpDbContext)
        {
            _sftpDbContext = sftpDbContext;
            _dbSet = sftpDbContext.Set<T>();
        }

        /// <summary>
        /// Saves all changes in this context to the database
        /// </summary>
        /// <returns></returns>
        private async Task SaveChangesAsync()
        {
            await _sftpDbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Saes a list of records
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public async Task SaveList(IEnumerable<T> list)
        {
            if(list != null)
            {
                await _dbSet.AddRangeAsync(list);

                //commit to database
                await SaveChangesAsync();
            }
        }
    }
}
