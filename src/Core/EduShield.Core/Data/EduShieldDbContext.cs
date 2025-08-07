using EduShield.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduShield.Core.Data;

public class EduShieldDbContext : DbContext
{
    public EduShieldDbContext(DbContextOptions<EduShieldDbContext> options) : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<Faculty> Faculty => Set<Faculty>();
    public DbSet<Performance> Performances => Set<Performance>();
    public DbSet<Fee> Fees => Set<Fee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Student entity
        modelBuilder.Entity<Student>(entity =>
        {
            entity.ToTable("Students");
            entity.HasKey(e => e.StudentId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Class).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Section).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Gender).IsRequired();

            // Configure relationships
            entity.HasMany(e => e.Performances)
                  .WithOne(p => p.Student)
                  .HasForeignKey(p => p.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Fees)
                  .WithOne(f => f.Student)
                  .HasForeignKey(f => f.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Faculty entity
        modelBuilder.Entity<Faculty>(entity =>
        {
            entity.ToTable("Faculty");
            entity.HasKey(e => e.FacultyId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Department).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Gender).IsRequired();

            // Configure relationships
            entity.HasMany(e => e.Performances)
                  .WithOne(p => p.Faculty)
                  .HasForeignKey(p => p.FacultyId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Performance entity
        modelBuilder.Entity<Performance>(entity =>
        {
            entity.ToTable("Performances");
            entity.HasKey(e => e.PerformanceId);
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Marks).HasPrecision(5, 2);
            entity.Property(e => e.MaxMarks).HasPrecision(5, 2);
            entity.Property(e => e.ExamDate).IsRequired();

            // Ignore calculated property
            entity.Ignore(e => e.Percentage);

            // Configure indexes
            entity.HasIndex(e => new { e.StudentId, e.Subject, e.ExamDate })
                  .HasDatabaseName("IX_Performance_Student_Subject_Date");
        });

        // Configure Fee entity
        modelBuilder.Entity<Fee>(entity =>
        {
            entity.ToTable("Fees");
            entity.HasKey(e => e.FeeId);
            entity.Property(e => e.FeeType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Amount).HasPrecision(10, 2);
            entity.Property(e => e.DueDate).IsRequired();
            entity.Property(e => e.IsPaid).IsRequired();
            entity.Property(e => e.PaidDate).IsRequired(false);

            // Ignore calculated properties
            entity.Ignore(e => e.IsOverdue);
            entity.Ignore(e => e.DaysOverdue);

            // Configure indexes
            entity.HasIndex(e => new { e.StudentId, e.IsPaid })
                  .HasDatabaseName("IX_Fee_Student_Paid");
            entity.HasIndex(e => e.DueDate)
                  .HasDatabaseName("IX_Fee_DueDate");
        });
    }
}
