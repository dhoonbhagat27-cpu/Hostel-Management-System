using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HostelEase.Data;
using HostelEase.ViewModels;

namespace HostelEase.Controllers
{
    [Authorize(Roles = "Admin,Warden")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Reports
        public async Task<IActionResult> Index(string type = "Occupancy", int? hostelId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            ViewBag.ReportType = type;
            ViewBag.HostelId = hostelId;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.Hostels = new SelectList(await _context.Hostels.ToListAsync(), "Id", "Name", hostelId);

            // Compute summary cards
            ViewBag.TotalStudents = await _context.Students.CountAsync();
            ViewBag.TotalBeds = await _context.Beds.CountAsync();
            ViewBag.OccupiedBeds = await _context.Beds.CountAsync(b => b.Status == "Occupied");
            ViewBag.TotalFeesCollected = await _context.FeePayments.SumAsync(p => (decimal?)p.Amount) ?? 0m;
            ViewBag.TotalFeesOutstanding = await _context.StudentFees.SumAsync(f => (decimal?)f.BalanceAmount) ?? 0m;
            ViewBag.TotalComplaints = await _context.Complaints.CountAsync();
            ViewBag.ResolvedComplaints = await _context.Complaints.CountAsync(c => c.Status == "Resolved" || c.Status == "Closed");

            if (type == "Fees")
            {
                var query = _context.FeePayments
                    .Include(p => p.StudentFee)
                        .ThenInclude(f => f!.Student)
                    .AsQueryable();

                if (startDate.HasValue) query = query.Where(p => p.PaymentDate >= startDate.Value);
                if (endDate.HasValue) query = query.Where(p => p.PaymentDate <= endDate.Value);

                var payments = await query.OrderByDescending(p => p.PaymentDate).ToListAsync();
                return View("FeesReport", payments);
            }
            else if (type == "Complaints")
            {
                var query = _context.Complaints
                    .Include(c => c.Student)
                    .Include(c => c.AssignedStaff)
                    .AsQueryable();

                if (startDate.HasValue) query = query.Where(c => c.SubmissionDate >= startDate.Value);
                if (endDate.HasValue) query = query.Where(c => c.SubmissionDate <= endDate.Value);

                var complaints = await query.OrderByDescending(c => c.SubmissionDate).ToListAsync();
                return View("ComplaintsReport", complaints);
            }
            else if (type == "Leaves")
            {
                var query = _context.LeaveRequests
                    .Include(l => l.Student)
                    .AsQueryable();

                if (startDate.HasValue) query = query.Where(l => l.StartDate >= startDate.Value);
                if (endDate.HasValue) query = query.Where(l => l.StartDate <= endDate.Value);

                var leaves = await query.OrderByDescending(l => l.SubmissionDate).ToListAsync();
                return View("LeavesReport", leaves);
            }
            else if (type == "Visitors")
            {
                var query = _context.Visitors
                    .Include(v => v.Student)
                    .AsQueryable();

                if (startDate.HasValue) query = query.Where(v => v.EntryTime >= startDate.Value);
                if (endDate.HasValue) query = query.Where(v => v.EntryTime <= endDate.Value);

                var visitors = await query.OrderByDescending(v => v.EntryTime).ToListAsync();
                return View("VisitorsReport", visitors);
            }
            else
            {
                // Default: Occupancy Report
                var query = _context.Rooms
                    .Include(r => r.Hostel)
                    .Include(r => r.Beds)
                        .ThenInclude(b => b.Allocations.Where(a => a.Status == "Active"))
                            .ThenInclude(a => a.Student)
                    .AsQueryable();

                if (hostelId.HasValue && hostelId.Value > 0)
                {
                    query = query.Where(r => r.HostelId == hostelId.Value);
                }

                var rooms = await query.OrderBy(r => r.Hostel!.Name).ThenBy(r => r.RoomNumber).ToListAsync();
                return View("OccupancyReport", rooms);
            }
        }

        // GET: /Reports/ExportCsv
        public async Task<IActionResult> ExportCsv(string type)
        {
            var sb = new StringBuilder();

            switch (type?.ToLower())
            {
                case "students":
                    sb.AppendLine("StudentID,FullName,Email,PhoneNumber,Department,AcademicYear,Gender,Status,AdmissionDate");
                    var students = await _context.Students.OrderBy(s => s.StudentId).ToListAsync();
                    foreach (var s in students)
                    {
                        sb.AppendLine($"\"{EscapeCsv(s.StudentId)}\",\"{EscapeCsv(s.FullName)}\",\"{EscapeCsv(s.Email)}\",\"{EscapeCsv(s.PhoneNumber)}\",\"{EscapeCsv(s.Department)}\",\"{EscapeCsv(s.AcademicYear)}\",\"{EscapeCsv(s.Gender)}\",\"{(s.IsActive ? "Active" : "Inactive")}\",\"{s.AdmissionDate:yyyy-MM-dd}\"");
                    }
                    return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"HostelEase_Students_{DateTime.UtcNow:yyyyMMdd}.csv");

                case "occupancy":
                    sb.AppendLine("Hostel,RoomNumber,Floor,RoomType,Capacity,OccupiedBeds,AvailableBeds,Status,OccupancyRate");
                    var rooms = await _context.Rooms.Include(r => r.Hostel).Include(r => r.Beds).ToListAsync();
                    foreach (var r in rooms)
                    {
                        var occ = r.Beds.Count(b => b.Status == "Occupied");
                        var avail = r.Beds.Count(b => b.Status == "Available");
                        var rate = r.Capacity > 0 ? Math.Round(((double)occ / r.Capacity) * 100, 1) : 0;
                        sb.AppendLine($"\"{EscapeCsv(r.Hostel?.Name)}\",\"{EscapeCsv(r.RoomNumber)}\",{r.Floor},\"{EscapeCsv(r.RoomType)}\",{r.Capacity},{occ},{avail},\"{r.Status}\",{rate}%");
                    }
                    return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"HostelEase_Occupancy_{DateTime.UtcNow:yyyyMMdd}.csv");

                case "fees":
                    sb.AppendLine("ReceiptNumber,StudentID,StudentName,FeeTitle,Amount,PaymentMethod,TransactionReference,PaymentDate");
                    var payments = await _context.FeePayments
                        .Include(p => p.StudentFee)
                            .ThenInclude(f => f!.Student)
                        .OrderByDescending(p => p.PaymentDate)
                        .ToListAsync();

                    foreach (var p in payments)
                    {
                        sb.AppendLine($"\"{EscapeCsv(p.ReceiptNumber)}\",\"{EscapeCsv(p.StudentFee?.Student?.StudentId)}\",\"{EscapeCsv(p.StudentFee?.Student?.FullName)}\",\"{EscapeCsv(p.StudentFee?.FeeTitle)}\",{p.Amount},\"{EscapeCsv(p.PaymentMethod)}\",\"{EscapeCsv(p.TransactionReference)}\",\"{p.PaymentDate:yyyy-MM-dd}\"");
                    }
                    return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"HostelEase_FeePayments_{DateTime.UtcNow:yyyyMMdd}.csv");

                case "complaints":
                    sb.AppendLine("ID,StudentID,StudentName,Category,Priority,Subject,Status,SubmissionDate,ResolvedDate");
                    var complaints = await _context.Complaints.Include(c => c.Student).OrderByDescending(c => c.SubmissionDate).ToListAsync();
                    foreach (var c in complaints)
                    {
                        sb.AppendLine($"{c.Id},\"{EscapeCsv(c.Student?.StudentId)}\",\"{EscapeCsv(c.Student?.FullName)}\",\"{EscapeCsv(c.Category)}\",\"{EscapeCsv(c.Priority)}\",\"{EscapeCsv(c.Subject)}\",\"{c.Status}\",\"{c.SubmissionDate:yyyy-MM-dd}\",\"{(c.ResolvedDate.HasValue ? c.ResolvedDate.Value.ToString("yyyy-MM-dd") : "N/A")}\"");
                    }
                    return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"HostelEase_Complaints_{DateTime.UtcNow:yyyyMMdd}.csv");

                case "visitors":
                    sb.AppendLine("ID,VisitorName,ContactNumber,Relation,StudentVisited,StudentID,EntryTime,ExitTime,Purpose");
                    var visitors = await _context.Visitors.Include(v => v.Student).OrderByDescending(v => v.EntryTime).ToListAsync();
                    foreach (var v in visitors)
                    {
                        sb.AppendLine($"{v.Id},\"{EscapeCsv(v.VisitorName)}\",\"{EscapeCsv(v.ContactNumber)}\",\"{EscapeCsv(v.RelationToStudent)}\",\"{EscapeCsv(v.Student?.FullName)}\",\"{EscapeCsv(v.Student?.StudentId)}\",\"{v.EntryTime:yyyy-MM-dd HH:mm}\",\"{(v.ExitTime.HasValue ? v.ExitTime.Value.ToString("yyyy-MM-dd HH:mm") : "Currently Inside")}\",\"{EscapeCsv(v.PurposeOfVisit)}\"");
                    }
                    return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"HostelEase_Visitors_{DateTime.UtcNow:yyyyMMdd}.csv");

                case "leaves":
                    sb.AppendLine("ID,StudentID,StudentName,StartDate,EndDate,Destination,Reason,EmergencyContact,ParentInformed,Status");
                    var leaves = await _context.LeaveRequests.Include(l => l.Student).OrderByDescending(l => l.SubmissionDate).ToListAsync();
                    foreach (var l in leaves)
                    {
                        sb.AppendLine($"{l.Id},\"{EscapeCsv(l.Student?.StudentId)}\",\"{EscapeCsv(l.Student?.FullName)}\",\"{l.StartDate:yyyy-MM-dd}\",\"{l.EndDate:yyyy-MM-dd}\",\"{EscapeCsv(l.Destination)}\",\"{EscapeCsv(l.Reason)}\",\"{EscapeCsv(l.EmergencyContact)}\",\"{(l.ParentInformed ? "Yes" : "No")}\",\"{l.Status}\"");
                    }
                    return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"HostelEase_Leaves_{DateTime.UtcNow:yyyyMMdd}.csv");

                default:
                    return RedirectToAction("Index");
            }
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace("\"", "\"\"");
        }
    }
}
