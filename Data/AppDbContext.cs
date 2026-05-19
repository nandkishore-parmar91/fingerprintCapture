using Microsoft.EntityFrameworkCore;

namespace FingerprintService.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

    }
}