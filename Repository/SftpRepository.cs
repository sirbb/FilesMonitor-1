using Entities;
using Repository.Interfaces;

namespace Repository
{
    public class SftpRepository : Repository<FileMonitor>, ISftpRepository
    {
      
        public SftpRepository(SftpDbContext sftpDbContext) : base(sftpDbContext)
        {
        
        }
    }
}