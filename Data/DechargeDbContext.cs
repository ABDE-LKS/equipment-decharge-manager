using EquipmentDechargeManager.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EquipmentDechargeManager.Data;

public class DechargeDbContext : DbContext
{
    public DechargeDbContext(DbContextOptions<DechargeDbContext> options)
        : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Equipment> Equipments => Set<Equipment>();
    public DbSet<Decharge> Decharges => Set<Decharge>();
    public DbSet<DechargeItem> DechargeItems => Set<DechargeItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Employee
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Matricule).IsUnique();
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Matricule).IsRequired().HasMaxLength(50);
        });

        // Equipment
        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SerialNumber).IsUnique();
            entity.HasIndex(e => e.InventoryNumber).IsUnique();
            entity.Property(e => e.Type).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Brand).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Model).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SerialNumber).HasMaxLength(100);
            entity.Property(e => e.InventoryNumber).HasMaxLength(100);
            entity.Property(e => e.Status).HasConversion<string>();
        });

        // Decharge
        modelBuilder.Entity<Decharge>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.HasIndex(d => d.DechargeNumber).IsUnique();
            entity.Property(d => d.DechargeNumber).IsRequired().HasMaxLength(50);
            entity.Property(d => d.Status).IsRequired().HasMaxLength(50);

            entity.HasOne(d => d.Employee)
                .WithMany(e => e.Decharges)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // DechargeItem
        modelBuilder.Entity<DechargeItem>(entity =>
        {
            entity.HasKey(di => di.Id);
            entity.Property(di => di.ConditionAtAssignment).IsRequired().HasMaxLength(500);
            entity.Property(di => di.ConditionReturned).HasMaxLength(500);

            entity.HasOne(di => di.Decharge)
                .WithMany(d => d.Items)
                .HasForeignKey(di => di.DechargeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(di => di.Equipment)
                .WithMany(e => e.DechargeItems)
                .HasForeignKey(di => di.EquipmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
