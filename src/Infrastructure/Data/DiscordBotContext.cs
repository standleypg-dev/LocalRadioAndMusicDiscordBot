using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class DiscordBotContext(DbContextOptions<DiscordBotContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Song> Songs { get; set; }
    public DbSet<PlayHistory> PlayHistory { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
        
        modelBuilder.Entity<Song>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SourceUrl).IsUnique();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
        
        modelBuilder.Entity<PlayHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PlayedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}