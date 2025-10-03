using Microsoft.EntityFrameworkCore;
using MedicalHistoryService.API.Models;

namespace MedicalHistoryService.API.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<MedicalHistory> MedicalHistories => Set<MedicalHistory>();
    public DbSet<Diagnosis> Diagnoses => Set<Diagnosis>();
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // MedicalHistory configuration
        modelBuilder.Entity<MedicalHistory>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Document)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.Notes)
                .HasMaxLength(1000);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("NOW()");

            // Indexes
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.Document);
            entity.HasIndex(e => e.IsDeleted);

            entity.ToTable("MedicalHistories");
        });

        // Diagnosis configuration
        modelBuilder.Entity<Diagnosis>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.Date)
              .HasColumnName("Date") 
              .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("NOW()");

            // Relationships
            entity.HasOne(e => e.MedicalHistory)
                .WithMany(e => e.Diagnoses)
                .HasForeignKey(e => e.MedicalHistoryId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            entity.HasIndex(e => e.MedicalHistoryId);
            entity.HasIndex(e => e.Date);

            entity.ToTable("Diagnoses");
        });

        // Exam configuration
        modelBuilder.Entity<Exam>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Type)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Result)
                .HasMaxLength(2000);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("NOW()");

            // Relationships
            entity.HasOne(e => e.MedicalHistory)
                .WithMany(e => e.Exams)
                .HasForeignKey(e => e.MedicalHistoryId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            entity.HasIndex(e => e.MedicalHistoryId);
            entity.HasIndex(e => e.Date);
            entity.HasIndex(e => e.Type);

            entity.ToTable("Exams");
        });

        // Prescription configuration
        modelBuilder.Entity<Prescription>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Medication)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.Dosage)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("NOW()");

            // Relationships
            entity.HasOne(e => e.MedicalHistory)
                .WithMany(e => e.Prescriptions)
                .HasForeignKey(e => e.MedicalHistoryId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            entity.HasIndex(e => e.MedicalHistoryId);
            entity.HasIndex(e => e.Date);
            entity.HasIndex(e => e.Medication);

            entity.ToTable("Prescriptions");
        });
    }
}