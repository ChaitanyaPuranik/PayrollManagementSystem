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
        public DbSet<TimesheetEntry> TimesheetEntries => Set<TimesheetEntry>();
        public DbSet<PayRun> PayRuns => Set<PayRun>();
        public DbSet<PayDetail> PayDetails => Set<PayDetail>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Employee
            builder.Entity<Employee>()
                .HasOne(e => e.AppUser)
                .WithOne()
                .HasForeignKey<Employee>(e => e.AppUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Optional: one employee <-> one app user
            builder.Entity<Employee>()
                .HasIndex(e => e.AppUserId)
                .IsUnique()
                .HasFilter("[AppUserId] IS NOT NULL");

            // Timesheet -> Employee
            builder.Entity<Timesheet>()
                .HasOne(t => t.Employee)
                .WithMany(e => e.Timesheets)
                .HasForeignKey(t => t.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // One weekly timesheet per employee
            builder.Entity<Timesheet>()
                .HasIndex(t => new { t.EmployeeId, t.WeekStart })
                .IsUnique();

            // TimesheetEntry -> Timesheet
            builder.Entity<TimesheetEntry>()
                .HasOne(te => te.Timesheet)
                .WithMany(t => t.Entries)
                .HasForeignKey(te => te.TimesheetId)
                .OnDelete(DeleteBehavior.Cascade);

            // One entry per work date within a timesheet
            builder.Entity<TimesheetEntry>()
                .HasIndex(te => new { te.TimesheetId, te.WorkDate })
                .IsUnique();

            // PayDetail -> Employee
            builder.Entity<PayDetail>()
                .HasOne(pd => pd.Employee)
                .WithMany(e => e.PayDetails)
                .HasForeignKey(pd => pd.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // PayDetail -> PayRun
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