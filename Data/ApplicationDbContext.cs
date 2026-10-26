using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HostelEase.Models;

namespace HostelEase.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Student> Students => Set<Student>();
        public DbSet<Staff> StaffMembers => Set<Staff>();
        public DbSet<Hostel> Hostels => Set<Hostel>();
        public DbSet<Room> Rooms => Set<Room>();
        public DbSet<Bed> Beds => Set<Bed>();
        public DbSet<RoomAllocation> RoomAllocations => Set<RoomAllocation>();
        public DbSet<FeeStructure> FeeStructures => Set<FeeStructure>();
        public DbSet<StudentFee> StudentFees => Set<StudentFee>();
        public DbSet<FeePayment> FeePayments => Set<FeePayment>();
        public DbSet<Complaint> Complaints => Set<Complaint>();
        public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
        public DbSet<Visitor> Visitors => Set<Visitor>();
        public DbSet<Announcement> Announcements => Set<Announcement>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Indexes and Unique Constraints
            builder.Entity<Student>()
                .HasIndex(s => s.StudentId)
                .IsUnique();

            builder.Entity<Student>()
                .HasIndex(s => s.Email)
                .IsUnique();

            builder.Entity<Staff>()
                .HasIndex(s => s.StaffId)
                .IsUnique();

            builder.Entity<Hostel>()
                .HasIndex(h => h.Code)
                .IsUnique();

            builder.Entity<Room>()
                .HasIndex(r => new { r.HostelId, r.RoomNumber })
                .IsUnique();

            builder.Entity<Bed>()
                .HasIndex(b => new { b.RoomId, b.BedNumber })
                .IsUnique();

            builder.Entity<FeePayment>()
                .HasIndex(p => p.ReceiptNumber)
                .IsUnique();

            // Decimal Precision
            builder.Entity<Room>()
                .Property(r => r.MonthlyFee)
                .HasPrecision(18, 2);

            builder.Entity<FeeStructure>()
                .Property(f => f.HostelRent)
                .HasPrecision(18, 2);
            builder.Entity<FeeStructure>()
                .Property(f => f.MessFee)
                .HasPrecision(18, 2);
            builder.Entity<FeeStructure>()
                .Property(f => f.MaintenanceFee)
                .HasPrecision(18, 2);
            builder.Entity<FeeStructure>()
                .Property(f => f.SecurityDeposit)
                .HasPrecision(18, 2);
            builder.Entity<FeeStructure>()
                .Property(f => f.TotalAmount)
                .HasPrecision(18, 2);

            builder.Entity<StudentFee>()
                .Property(sf => sf.TotalAmount)
                .HasPrecision(18, 2);
            builder.Entity<StudentFee>()
                .Property(sf => sf.PaidAmount)
                .HasPrecision(18, 2);
            builder.Entity<StudentFee>()
                .Property(sf => sf.BalanceAmount)
                .HasPrecision(18, 2);

            builder.Entity<FeePayment>()
                .Property(fp => fp.Amount)
                .HasPrecision(18, 2);

            // Relationships and Delete Behaviors
            builder.Entity<Student>()
                .HasOne(s => s.User)
                .WithOne(u => u.Student)
                .HasForeignKey<Student>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Staff>()
                .HasOne(st => st.User)
                .WithOne(u => u.Staff)
                .HasForeignKey<Staff>(st => st.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Staff>()
                .HasOne(st => st.AssignedHostel)
                .WithMany()
                .HasForeignKey(st => st.AssignedHostelId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Hostel>()
                .HasOne(h => h.AssignedWarden)
                .WithMany()
                .HasForeignKey(h => h.AssignedWardenId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Room>()
                .HasOne(r => r.Hostel)
                .WithMany(h => h.Rooms)
                .HasForeignKey(r => r.HostelId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Bed>()
                .HasOne(b => b.Room)
                .WithMany(r => r.Beds)
                .HasForeignKey(b => b.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<RoomAllocation>()
                .HasOne(ra => ra.Bed)
                .WithMany(b => b.Allocations)
                .HasForeignKey(ra => ra.BedId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RoomAllocation>()
                .HasOne(ra => ra.Student)
                .WithMany(s => s.Allocations)
                .HasForeignKey(ra => ra.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<StudentFee>()
                .HasOne(sf => sf.Student)
                .WithMany(s => s.Fees)
                .HasForeignKey(sf => sf.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FeePayment>()
                .HasOne(fp => fp.StudentFee)
                .WithMany(sf => sf.Payments)
                .HasForeignKey(fp => fp.StudentFeeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Complaint>()
                .HasOne(c => c.Student)
                .WithMany(s => s.Complaints)
                .HasForeignKey(c => c.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Complaint>()
                .HasOne(c => c.AssignedStaff)
                .WithMany(s => s.AssignedComplaints)
                .HasForeignKey(c => c.AssignedStaffId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<LeaveRequest>()
                .HasOne(lr => lr.Student)
                .WithMany(s => s.LeaveRequests)
                .HasForeignKey(lr => lr.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Visitor>()
                .HasOne(v => v.Student)
                .WithMany(s => s.Visitors)
                .HasForeignKey(v => v.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
