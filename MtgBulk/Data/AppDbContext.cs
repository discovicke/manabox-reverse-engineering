using Microsoft.EntityFrameworkCore;
using MtgBulk.Models;

namespace MtgBulk.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<MtgCard> Cards => Set<MtgCard>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MtgCard>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ScryfallId).IsUnique();
            entity.Property(e => e.Name).IsRequired();
        });
    }
}
