using DistributedCachingWithRedisCache;
using Microsoft.EntityFrameworkCore;
namespace DistributedCachingWithRedisCache
{
    public partial class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }
        public virtual DbSet<Subscriber> Subscribers { get; set; }

    }
}

