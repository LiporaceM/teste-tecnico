using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Models;

namespace TaskManager.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>().Property(u => u.FullName).HasMaxLength(120);

        builder.Entity<TaskItem>(e =>
        {
            e.ToTable("Tasks");
            e.HasKey(t => t.Id);
            e.Property(t => t.Title).IsRequired().HasMaxLength(150);
            e.Property(t => t.Description).HasMaxLength(2000);
            e.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(t => t.Priority).HasConversion<string>().HasMaxLength(20);
            e.HasOne(t => t.Owner).WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(t => new { t.UserId, t.Status });
        });
    }
}
