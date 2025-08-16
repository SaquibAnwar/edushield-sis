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
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Student entity
        modelBuilder.Entity<Student>(entity =>
        {
            entity.ToTable("Students");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.DateOfBirth).IsRequired();
            entity.Property(e => e.Address).IsRequired().HasMaxLength(500);
            entity.Property(e => e.EnrollmentDate).IsRequired();
            entity.Property(e => e.Gender).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            // Configure indexes
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.PhoneNumber);

            // Configure relationships
            entity.HasMany(e => e.Performances)
                  .WithOne(p => p.Student)
                  .HasForeignKey(p => p.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Fees)
                  .WithOne(f => f.Student)
                  .HasForeignKey(f => f.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Configure Faculty relationship
            entity.HasOne(e => e.Faculty)
                  .WithMany()
                  .HasForeignKey(e => e.FacultyId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.SetNull);
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

            // Configure Students relationship
            entity.HasMany(e => e.Students)
                  .WithOne(s => s.Faculty)
                  .HasForeignKey(s => s.FacultyId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.SetNull);
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
            entity.Property(e => e.FeeType).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(10, 2);
            entity.Property(e => e.PaidAmount).HasPrecision(10, 2);
            entity.Property(e => e.DueDate).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.IsPaid).IsRequired();
            entity.Property(e => e.PaidDate).IsRequired(false);

            // Ignore calculated properties
            entity.Ignore(e => e.OutstandingAmount);
            entity.Ignore(e => e.IsOverdue);
            entity.Ignore(e => e.DaysOverdue);

            // Configure relationships
            entity.HasMany(e => e.Payments)
                  .WithOne(p => p.Fee)
                  .HasForeignKey(p => p.FeeId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Configure indexes
            entity.HasIndex(e => new { e.StudentId, e.IsPaid })
                  .HasDatabaseName("IX_Fee_Student_Paid");
            entity.HasIndex(e => e.DueDate)
                  .HasDatabaseName("IX_Fee_DueDate");
            entity.HasIndex(e => e.FeeType)
                  .HasDatabaseName("IX_Fee_Type");
        });

        // Configure Payment entity
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payments");
            entity.HasKey(e => e.PaymentId);
            entity.Property(e => e.Amount).HasPrecision(10, 2);
            entity.Property(e => e.PaymentDate).IsRequired();
            entity.Property(e => e.PaymentMethod).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TransactionReference).HasMaxLength(100);

            // Configure indexes
            entity.HasIndex(e => e.FeeId)
                  .HasDatabaseName("IX_Payment_Fee");
            entity.HasIndex(e => e.PaymentDate)
                  .HasDatabaseName("IX_Payment_Date");
        });

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ExternalId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Provider).IsRequired();
            entity.Property(e => e.Role).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.ProfilePictureUrl).HasMaxLength(500);

            // Ignore computed properties
            entity.Ignore(e => e.FullName);

            // Configure indexes
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => new { e.ExternalId, e.Provider }).IsUnique();
            entity.HasIndex(e => e.Role);

            // Configure relationships
            entity.HasMany(e => e.Sessions)
                  .WithOne(s => s.User)
                  .HasForeignKey(s => s.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.AuditLogs)
                  .WithOne(a => a.User)
                  .HasForeignKey(a => a.UserId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure UserSession entity
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.ToTable("UserSessions");
            entity.HasKey(e => e.SessionId);
            entity.Property(e => e.SessionToken).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ExpiresAt).IsRequired();
            entity.Property(e => e.IpAddress).IsRequired().HasMaxLength(45);
            entity.Property(e => e.UserAgent).IsRequired().HasMaxLength(500);
            entity.Property(e => e.IsActive).IsRequired();

            // Ignore computed properties
            entity.Ignore(e => e.IsExpired);
            entity.Ignore(e => e.IsValid);

            // Configure indexes
            entity.HasIndex(e => e.SessionToken).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ExpiresAt);
        });

        // Configure AuditLog entity
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(e => e.AuditId);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Resource).IsRequired().HasMaxLength(200);
            entity.Property(e => e.IpAddress).IsRequired().HasMaxLength(45);
            entity.Property(e => e.UserAgent).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Success).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            entity.Property(e => e.AdditionalData).HasColumnType("text");

            // Configure indexes
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.Success, e.CreatedAt });
        });

        // Update Student entity to include User relationship
        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Update Faculty entity to include User relationship
        modelBuilder.Entity<Faculty>(entity =>
        {
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }

    public override int SaveChanges()
    {
        StampAudit();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampAudit();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void StampAudit()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is Entities.AuditableEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var aud = (Entities.AuditableEntity)entry.Entity;
            if (entry.State == EntityState.Added)
            {
                aud.CreatedAt = DateTime.UtcNow;
            }
            aud.UpdatedAt = DateTime.UtcNow;
        }
    }
}
