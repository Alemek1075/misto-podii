using Microsoft.EntityFrameworkCore;
using MistoPodii.Models;

namespace MistoPodii.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Organizer> Organizers => Set<Organizer>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Event> Events => Set<Event>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Event>()
            .HasOne(e => e.Organizer)
            .WithMany(o => o.Events)
            .HasForeignKey(e => e.OrganizerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Event>()
            .HasOne(e => e.Venue)
            .WithMany(v => v.Events)
            .HasForeignKey(e => e.VenueId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Event>().HasIndex(e => e.StartsAtUtc);
        modelBuilder.Entity<Event>().Property(e => e.Status).HasConversion<string>();
        modelBuilder.Entity<Event>().HasIndex(e => e.Category);
        modelBuilder.Entity<Venue>().HasIndex(v => v.Name);
    }
}
