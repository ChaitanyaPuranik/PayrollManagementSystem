using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PayrollManagementSystem.Areas.Identity.Data;
using PayrollManagementSystem.Models;

namespace PayrollManagementSystem.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<Timesheet> Timesheets => Set<Timesheet>();
        public DbSet<PayRun> PayRuns => Set<PayRun>();
        public DbSet<PayDetail> PayDetails => Set<PayDetail>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Employee
            builder.Entity<Employee>()
                .HasIndex(e => e.Email)
                .IsUnique();

            // Timesheet relationships
            builder.Entity<Timesheet>()
                .HasOne(t => t.Employee)
                .WithMany(e => e.Timesheets)
                .HasForeignKey(t => t.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // One employee should ideally have only one timesheet per week
            builder.Entity<Timesheet>()
                .HasIndex(t => new { t.EmployeeId, t.WeekStart })
                .IsUnique();

            // PayDetail relationships
            builder.Entity<PayDetail>()
                .HasOne(pd => pd.Employee)
                .WithMany(e => e.PayDetails)
                .HasForeignKey(pd => pd.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PayDetail>()
                .HasOne(pd => pd.PayRun)
                .WithMany(pr => pr.PayDetails)
                .HasForeignKey(pd => pd.PayRunId)
                .OnDelete(DeleteBehavior.Cascade);

            // One pay detail per employee per pay run
            builder.Entity<PayDetail>()
                .HasIndex(pd => new { pd.PayRunId, pd.EmployeeId })
                .IsUnique();
        }
    }
}