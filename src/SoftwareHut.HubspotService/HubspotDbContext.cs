using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SoftwareHut.HubspotService.DbModels;

namespace SoftwareHut.HubspotService
{
    public interface IHubspotDbContext
    {
        DbSet<HubspotDbContact> Users { get; set; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken());
    }

    public class HubspotDbContext : DbContext, IHubspotDbContext
    {
        public HubspotDbContext(DbContextOptions<HubspotDbContext> options) : base(options) { }

        public DbSet<HubspotDbContact> Users { get; set; }
    }
}