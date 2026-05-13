using Microsoft.EntityFrameworkCore;
using FingerprintService.Models;

namespace FingerprintService.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<FingerprintTemplate> FingerprintTemplates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FingerprintTemplate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Template).HasColumnType("varbinary(max)");
                entity.HasIndex(e => e.UserId);
                entity.ToTable("fingerprint_templates");
            });
        }
    }
}