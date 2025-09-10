using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Infrastructure.CompiledModels;

namespace Infrastructure.Data;

public class DiscordBotContext(DbContextOptions<DiscordBotContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Song> Songs { get; set; }
    public DbSet<PlayHistory> PlayHistory { get; set; }
    public DbSet<RadioSource> RadioSources { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseModel(DiscordBotContextModel.Instance);
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
        });
        
        modelBuilder.Entity<Song>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SourceUrl).IsUnique();
        });
        
        modelBuilder.Entity<PlayHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
        });
        
        modelBuilder.Entity<RadioSource>(entity =>
        {
            entity.HasKey(e => e.Id);
        });
    }
}