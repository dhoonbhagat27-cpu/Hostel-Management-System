using HostelEase.Models;

namespace HostelEase.ViewModels
{
    public class AdminDashboardViewModel
    {
        // Statistics Cards
        public int TotalStudents { get; set; }
        public int ActiveStudents { get; set; }
        public int TotalHostels { get; set; }
        public int TotalRooms { get; set; }
        public int TotalBeds { get; set; }
        public int OccupiedBeds { get; set; }
        public int AvailableBeds { get; set; }
        public double OccupancyPercentage { get; set; }

        public decimal TotalFeeCollection { get; set; }
        public decimal OutstandingFees { get; set; }

        public int PendingComplaints { get; set; }
        public int PendingLeaveRequests { get; set; }
        public int VisitorsInside { get; set; }

        // Recent Activity Lists
        public List<Student> RecentRegistrations { get; set; } = new();
        public List<FeePayment> RecentPayments { get; set; } = new();
        public List<Complaint> RecentComplaints { get; set; } = new();
        public List<LeaveRequest> PendingApprovals { get; set; } = new();
        public List<Visitor> RecentVisitors { get; set; } = new();

        // Chart Data (JSON-serializable)
        public List<string> MonthlyFeeLabels { get; set; } = new();
        public List<decimal> MonthlyFeeValues { get; set; } = new();

        public List<string> HostelOccupancyLabels { get; set; } = new();
        public List<int> HostelOccupiedValues { get; set; } = new();
        public List<int> HostelCapacityValues { get; set; } = new();

        public int ComplaintSubmittedCount { get; set; }
        public int ComplaintInProgressCount { get; set; }
        public int ComplaintResolvedCount { get; set; }

        public int LeavePendingCount { get; set; }
        public int LeaveApprovedCount { get; set; }
        public int LeaveRejectedCount { get; set; }
    }

    public class WardenDashboardViewModel
    {
        public Hostel? AssignedHostel { get; set; }
        public int HostelStudentsCount { get; set; }
        public int TotalRooms { get; set; }
        public int TotalBeds { get; set; }
        public int OccupiedBeds { get; set; }
        public int AvailableBeds { get; set; }
        public double OccupancyPercentage { get; set; }

        public int PendingComplaints { get; set; }
        public int InProgressComplaints { get; set; }
        public int PendingLeaves { get; set; }
        public int VisitorsInside { get; set; }

        public List<LeaveRequest> PendingLeaveRequests { get; set; } = new();
        public List<Complaint> RecentComplaints { get; set; } = new();
        public List<Visitor> CurrentVisitors { get; set; } = new();
        public List<Room> RoomsList { get; set; } = new();
    }

    public class StudentDashboardViewModel
    {
        public Student Student { get; set; } = null!;
        public RoomAllocation? CurrentAllocation { get; set; }
        public decimal TotalFee { get; set; }
        public decimal PaidFee { get; set; }
        public decimal BalanceFee { get; set; }
        public int PendingComplaintsCount { get; set; }
        public int ActiveLeavesCount { get; set; }
        public int UnreadNotificationsCount { get; set; }

        public List<StudentFee> PendingFees { get; set; } = new();
        public List<Complaint> RecentComplaints { get; set; } = new();
        public List<LeaveRequest> RecentLeaves { get; set; } = new();
        public List<Announcement> Announcements { get; set; } = new();
    }
}
