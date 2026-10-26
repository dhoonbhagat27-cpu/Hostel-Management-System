using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HostelEase.Models;

namespace HostelEase.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // If old mixed database exists, clean up and rebuild for Girls' Hostel
            var hasBoysHostel = await context.Hostels.AnyAsync(h => h.HostelType == "Boys");
            if (hasBoysHostel)
            {
                await context.Database.EnsureDeletedAsync();
            }

            await context.Database.MigrateAsync();

            // 1. Seed Roles
            string[] roles = { "Admin", "Warden", "Student" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Seed Admin
            var adminEmail = "admin@hostelease.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "Chief Administrator",
                    UserType = "Admin",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // 3. Seed Hostels (All Girls' Halls of Residence)
            if (!await context.Hostels.AnyAsync())
            {
                var blockA = new Hostel
                {
                    Name = "Kasturba Hall of Residence",
                    Code = "BLK-A",
                    HostelType = "Girls",
                    Address = "East Campus, Women's Residential Enclave, Gate 2",
                    ContactNumber = "+91 98765 43210",
                    TotalFloors = 4,
                    Status = "Active",
                    Description = "Flagship residential complex for female undergraduate engineering students featuring 24/7 security, biometric entry, reading lounge, and dining mess."
                };

                var blockB = new Hostel
                {
                    Name = "Sarojini Naidu Hall of Residence",
                    Code = "BLK-B",
                    HostelType = "Girls",
                    Address = "South Campus, Scholar Avenue, Sector 4",
                    ContactNumber = "+91 98765 43211",
                    TotalFloors = 3,
                    Status = "Active",
                    Description = "Quiet and comfortable residential hall for female postgraduate scholars and researchers with dedicated study desks and high-speed fiber."
                };

                var blockC = new Hostel
                {
                    Name = "Kalpana Chawla Hall of Residence",
                    Code = "BLK-C",
                    HostelType = "Girls",
                    Address = "North Campus, Innovation Avenue",
                    ContactNumber = "+91 98765 43212",
                    TotalFloors = 3,
                    Status = "Active",
                    Description = "Air-conditioned deluxe residential wing equipped with modern recreation hall, gymnasium, and 24/7 resident medical infirmary."
                };

                context.Hostels.AddRange(blockA, blockB, blockC);
                await context.SaveChangesAsync();

                // 4. Seed Female Wardens
                var warden1Email = "warden.sarojini@hostelease.com";
                var warden1User = await userManager.FindByEmailAsync(warden1Email);
                if (warden1User == null)
                {
                    warden1User = new ApplicationUser
                    {
                        UserName = warden1Email,
                        Email = warden1Email,
                        EmailConfirmed = true,
                        FullName = "Prof. Sunita Mehra",
                        UserType = "Warden",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    var wRes = await userManager.CreateAsync(warden1User, "Warden@123");
                    if (wRes.Succeeded)
                    {
                        await userManager.AddToRoleAsync(warden1User, "Warden");

                        var staff1 = new Staff
                        {
                            UserId = warden1User.Id,
                            StaffId = "WRD-GH-01",
                            FullName = warden1User.FullName,
                            Email = warden1Email,
                            PhoneNumber = "+91 98111 22233",
                            Designation = "Chief Warden (Girls' Hostels)",
                            AssignedHostelId = blockA.Id,
                            JoiningDate = DateTime.UtcNow.AddYears(-3),
                            IsActive = true
                        };
                        context.StaffMembers.Add(staff1);
                        await context.SaveChangesAsync();

                        blockA.AssignedWardenId = staff1.Id;
                        await context.SaveChangesAsync();
                    }
                }

                var warden2Email = "warden.kalpana@hostelease.com";
                var warden2User = await userManager.FindByEmailAsync(warden2Email);
                if (warden2User == null)
                {
                    warden2User = new ApplicationUser
                    {
                        UserName = warden2Email,
                        Email = warden2Email,
                        EmailConfirmed = true,
                        FullName = "Dr. Ananya Mukherjee",
                        UserType = "Warden",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    var wRes2 = await userManager.CreateAsync(warden2User, "Warden@123");
                    if (wRes2.Succeeded)
                    {
                        await userManager.AddToRoleAsync(warden2User, "Warden");

                        var staff2 = new Staff
                        {
                            UserId = warden2User.Id,
                            StaffId = "WRD-GH-02",
                            FullName = warden2User.FullName,
                            Email = warden2Email,
                            PhoneNumber = "+91 98222 33344",
                            Designation = "Resident Warden (Block B & C)",
                            AssignedHostelId = blockB.Id,
                            JoiningDate = DateTime.UtcNow.AddYears(-2),
                            IsActive = true
                        };
                        context.StaffMembers.Add(staff2);
                        await context.SaveChangesAsync();

                        blockB.AssignedWardenId = staff2.Id;
                        blockC.AssignedWardenId = staff2.Id;
                        await context.SaveChangesAsync();
                    }
                }

                // 5. Seed Rooms & Beds for Girls' Hostels
                // Block A Rooms
                var room101 = new Room
                {
                    HostelId = blockA.Id,
                    RoomNumber = "101",
                    Floor = 1,
                    RoomType = "Double",
                    Capacity = 2,
                    MonthlyFee = 6500.00m,
                    Status = "Available",
                    Description = "Spacious double-occupancy room with garden view, study carrels, and wardrobe storage."
                };
                var room102 = new Room
                {
                    HostelId = blockA.Id,
                    RoomNumber = "102",
                    Floor = 1,
                    RoomType = "Double",
                    Capacity = 2,
                    MonthlyFee = 6500.00m,
                    Status = "Available",
                    Description = "Standard 2-bed room adjacent to the 1st floor reading hall."
                };
                var room201 = new Room
                {
                    HostelId = blockA.Id,
                    RoomNumber = "201",
                    Floor = 2,
                    RoomType = "Triple",
                    Capacity = 3,
                    MonthlyFee = 5500.00m,
                    Status = "Available",
                    Description = "Triple-sharing room on second floor with attached study balcony."
                };

                // Block B Rooms
                var roomB101 = new Room
                {
                    HostelId = blockB.Id,
                    RoomNumber = "B-101",
                    Floor = 1,
                    RoomType = "Double",
                    Capacity = 2,
                    MonthlyFee = 7000.00m,
                    Status = "Available",
                    Description = "Quiet double room tailored for postgraduate researchers."
                };
                var roomB102 = new Room
                {
                    HostelId = blockB.Id,
                    RoomNumber = "B-102",
                    Floor = 1,
                    RoomType = "Single",
                    Capacity = 1,
                    MonthlyFee = 9500.00m,
                    Status = "Available",
                    Description = "Premium single-occupancy room for PhD and final-year thesis scholars."
                };

                // Block C Rooms
                var roomC101 = new Room
                {
                    HostelId = blockC.Id,
                    RoomNumber = "C-101",
                    Floor = 1,
                    RoomType = "Double AC",
                    Capacity = 2,
                    MonthlyFee = 9000.00m,
                    Status = "Available",
                    Description = "Climate-controlled double room with attached modern bathroom."
                };

                context.Rooms.AddRange(room101, room102, room201, roomB101, roomB102, roomC101);
                await context.SaveChangesAsync();

                // Beds
                var bed101A = new Bed { RoomId = room101.Id, BedNumber = "101-A", Status = "Available" };
                var bed101B = new Bed { RoomId = room101.Id, BedNumber = "101-B", Status = "Available" };
                var bed102A = new Bed { RoomId = room102.Id, BedNumber = "102-A", Status = "Available" };
                var bed102B = new Bed { RoomId = room102.Id, BedNumber = "102-B", Status = "Available" };
                var bed201A = new Bed { RoomId = room201.Id, BedNumber = "201-A", Status = "Available" };
                var bed201B = new Bed { RoomId = room201.Id, BedNumber = "201-B", Status = "Available" };
                var bed201C = new Bed { RoomId = room201.Id, BedNumber = "201-C", Status = "Available" };

                var bedB101A = new Bed { RoomId = roomB101.Id, BedNumber = "B101-A", Status = "Available" };
                var bedB101B = new Bed { RoomId = roomB101.Id, BedNumber = "B101-B", Status = "Available" };
                var bedB102A = new Bed { RoomId = roomB102.Id, BedNumber = "B102-A", Status = "Available" };
                var bedC101A = new Bed { RoomId = roomC101.Id, BedNumber = "C101-A", Status = "Available" };
                var bedC101B = new Bed { RoomId = roomC101.Id, BedNumber = "C101-B", Status = "Available" };

                context.Beds.AddRange(bed101A, bed101B, bed102A, bed102B, bed201A, bed201B, bed201C, bedB101A, bedB101B, bedB102A, bedC101A, bedC101B);
                await context.SaveChangesAsync();

                // 6. Fee Structures
                var fsSemesterDouble = new FeeStructure
                {
                    Title = "Semester 1 (Double Sharing)",
                    HostelType = "Girls",
                    RoomType = "Double",
                    AcademicYear = "2026-2027",
                    Term = "Semester",
                    HostelRent = 24000.00m,
                    MessFee = 18000.00m,
                    MaintenanceFee = 4000.00m,
                    SecurityDeposit = 5000.00m,
                    TotalAmount = 51000.00m,
                    IsActive = true
                };

                var fsSemesterTriple = new FeeStructure
                {
                    Title = "Semester 1 (Triple Sharing)",
                    HostelType = "Girls",
                    RoomType = "Triple",
                    AcademicYear = "2026-2027",
                    Term = "Semester",
                    HostelRent = 18000.00m,
                    MessFee = 18000.00m,
                    MaintenanceFee = 3500.00m,
                    SecurityDeposit = 5000.00m,
                    TotalAmount = 44500.00m,
                    IsActive = true
                };

                var fsACDouble = new FeeStructure
                {
                    Title = "Semester 1 (AC Double Deluxe)",
                    HostelType = "Girls",
                    RoomType = "Double AC",
                    AcademicYear = "2026-2027",
                    Term = "Semester",
                    HostelRent = 32000.00m,
                    MessFee = 18000.00m,
                    MaintenanceFee = 5000.00m,
                    SecurityDeposit = 5000.00m,
                    TotalAmount = 60000.00m,
                    IsActive = true
                };

                context.FeeStructures.AddRange(fsSemesterDouble, fsSemesterTriple, fsACDouble);
                await context.SaveChangesAsync();

                // 7. Seed Female Residents (Students)
                // Student 1: Priya Patel (Kasturba Hall, Bed 101-A)
                var s1Email = "priya.patel@hostelease.com";
                var s1User = new ApplicationUser
                {
                    UserName = s1Email,
                    Email = s1Email,
                    EmailConfirmed = true,
                    FullName = "Priya Patel",
                    UserType = "Student",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddMonths(-6)
                };
                if ((await userManager.CreateAsync(s1User, "Student@123")).Succeeded)
                {
                    await userManager.AddToRoleAsync(s1User, "Student");

                    var student1 = new Student
                    {
                        UserId = s1User.Id,
                        StudentId = "STU2026-GH101",
                        FullName = "Priya Patel",
                        Email = s1Email,
                        PhoneNumber = "+91 97123 45678",
                        Department = "Computer Engineering",
                        AcademicYear = "4th Year",
                        DateOfBirth = new DateTime(2004, 5, 14),
                        Gender = "Female",
                        GuardianName = "Suresh Patel",
                        GuardianContact = "+91 97123 99999",
                        LocalGuardianName = "Mrs. Rita Patel (Maternal Aunt)",
                        LocalGuardianContact = "+91 98333 44455",
                        LocalGuardianAddress = "B-204, Vasant Kunj, South Campus",
                        BloodGroup = "B+",
                        EmergencyContact = "+91 97123 99999",
                        Address = "Flat 402, Shivalik Heights, Ahmedabad, Gujarat",
                        AdmissionDate = DateTime.UtcNow.AddMonths(-6),
                        IsActive = true
                    };
                    context.Students.Add(student1);
                    await context.SaveChangesAsync();

                    // Allocate Bed 101-A
                    bed101A.Status = "Occupied";
                    var alloc1 = new RoomAllocation
                    {
                        StudentId = student1.Id,
                        BedId = bed101A.Id,
                        AllocationDate = DateTime.UtcNow.AddMonths(-5),
                        ExpectedVacatingDate = DateTime.UtcNow.AddMonths(7),
                        Status = "Active",
                        Remarks = "Allotted room 101 upon admission.",
                        AllocatedByUserId = adminUser.Id
                    };
                    context.RoomAllocations.Add(alloc1);

                    // Add Fee & Partial Payment
                    var s1Fee = new StudentFee
                    {
                        StudentId = student1.Id,
                        FeeStructureId = fsSemesterDouble.Id,
                        FeeTitle = $"{fsSemesterDouble.Title} (2026-2027 - Semester)",
                        DueDate = DateTime.UtcNow.AddDays(15),
                        TotalAmount = 51000.00m,
                        PaidAmount = 30000.00m,
                        BalanceAmount = 21000.00m,
                        Status = "Partially Paid",
                        CreatedAt = DateTime.UtcNow.AddMonths(-5)
                    };
                    context.StudentFees.Add(s1Fee);
                    await context.SaveChangesAsync();

                    var pay1 = new FeePayment
                    {
                        StudentFeeId = s1Fee.Id,
                        ReceiptNumber = "REC-2026-00101",
                        Amount = 30000.00m,
                        PaymentDate = DateTime.UtcNow.AddMonths(-4),
                        PaymentMethod = "UPI",
                        TransactionReference = "UPI/20260601/788192847",
                        RecordedByUserId = adminUser.Id,
                        Remarks = "Advance semester installment paid."
                    };
                    context.FeePayments.Add(pay1);

                    // Complaint for Student 1
                    var comp1 = new Complaint
                    {
                        StudentId = student1.Id,
                        Category = "Electrical",
                        Priority = "Medium",
                        Subject = "Study table lamp socket flickering",
                        Description = "The electrical wall outlet above study desk 101-A loses power periodically.",
                        Status = "In Progress",
                        SubmissionDate = DateTime.UtcNow.AddDays(-2),
                        AssignedStaffId = blockA.AssignedWardenId,
                        ResolutionRemarks = "Electrician dispatched to replace loose connector."
                    };
                    context.Complaints.Add(comp1);

                    // Leave Request for Student 1
                    var leave1 = new LeaveRequest
                    {
                        StudentId = student1.Id,
                        StartDate = DateTime.UtcNow.AddDays(5),
                        EndDate = DateTime.UtcNow.AddDays(8),
                        Reason = "Attending elder brother's wedding ceremony",
                        Destination = "Ahmedabad, Gujarat",
                        EmergencyContact = "+91 97123 99999",
                        ParentInformed = true,
                        Status = "Approved",
                        SubmissionDate = DateTime.UtcNow.AddDays(-1),
                        ReviewedByUserId = warden1User.Id,
                        ReviewDate = DateTime.UtcNow,
                        ReviewRemarks = "Approved after phone confirmation with parent."
                    };
                    context.LeaveRequests.Add(leave1);

                    // Visitor for Student 1
                    var vis1 = new Visitor
                    {
                        StudentId = student1.Id,
                        VisitorName = "Mrs. Rita Patel",
                        ContactNumber = "+91 98333 44455",
                        RelationToStudent = "Local Guardian / Aunt",
                        PurposeOfVisit = "Dropping off winter clothes and academic books",
                        EntryTime = DateTime.UtcNow.AddHours(-3),
                        ExitTime = DateTime.UtcNow.AddHours(-1),
                        RecordedByUserId = warden1User.Id,
                        Remarks = "Verified with local guardian register & Aadhaar card."
                    };
                    context.Visitors.Add(vis1);
                }

                // Student 2: Ananya Sharma (Kasturba Hall, Bed 101-B - Roommate to Priya!)
                var s2Email = "ananya.sharma@hostelease.com";
                var s2User = new ApplicationUser
                {
                    UserName = s2Email,
                    Email = s2Email,
                    EmailConfirmed = true,
                    FullName = "Ananya Sharma",
                    UserType = "Student",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddMonths(-4)
                };
                if ((await userManager.CreateAsync(s2User, "Student@123")).Succeeded)
                {
                    await userManager.AddToRoleAsync(s2User, "Student");

                    var student2 = new Student
                    {
                        UserId = s2User.Id,
                        StudentId = "STU2026-GH102",
                        FullName = "Ananya Sharma",
                        Email = s2Email,
                        PhoneNumber = "+91 98333 44556",
                        Department = "Information Technology",
                        AcademicYear = "3rd Year",
                        DateOfBirth = new DateTime(2005, 8, 22),
                        Gender = "Female",
                        GuardianName = "Dr. Vikram Sharma",
                        GuardianContact = "+91 98333 99999",
                        LocalGuardianName = "Mr. Vinod Sharma (Uncle)",
                        LocalGuardianContact = "+91 98444 55566",
                        LocalGuardianAddress = "C-14, Mayur Vihar, Phase 1",
                        BloodGroup = "O+",
                        EmergencyContact = "+91 98333 99999",
                        Address = "B-12, Green Park Extension, New Delhi",
                        AdmissionDate = DateTime.UtcNow.AddMonths(-4),
                        IsActive = true
                    };
                    context.Students.Add(student2);
                    await context.SaveChangesAsync();

                    // Allocate Bed 101-B (Room 101 is now full!)
                    bed101B.Status = "Occupied";
                    room101.Status = "Occupied";
                    var alloc2 = new RoomAllocation
                    {
                        StudentId = student2.Id,
                        BedId = bed101B.Id,
                        AllocationDate = DateTime.UtcNow.AddMonths(-4),
                        ExpectedVacatingDate = DateTime.UtcNow.AddMonths(8),
                        Status = "Active",
                        Remarks = "Allotted Room 101 Bed B.",
                        AllocatedByUserId = adminUser.Id
                    };
                    context.RoomAllocations.Add(alloc2);

                    // Add Fee - Fully Paid
                    var s2Fee = new StudentFee
                    {
                        StudentId = student2.Id,
                        FeeStructureId = fsSemesterDouble.Id,
                        FeeTitle = $"{fsSemesterDouble.Title} (2026-2027 - Semester)",
                        DueDate = DateTime.UtcNow.AddDays(30),
                        TotalAmount = 51000.00m,
                        PaidAmount = 51000.00m,
                        BalanceAmount = 0.00m,
                        Status = "Paid",
                        CreatedAt = DateTime.UtcNow.AddMonths(-4)
                    };
                    context.StudentFees.Add(s2Fee);
                    await context.SaveChangesAsync();

                    var pay2 = new FeePayment
                    {
                        StudentFeeId = s2Fee.Id,
                        ReceiptNumber = "REC-2026-00102",
                        Amount = 51000.00m,
                        PaymentDate = DateTime.UtcNow.AddMonths(-3),
                        PaymentMethod = "Bank Transfer",
                        TransactionReference = "NEFT-HDFC-99182371",
                        RecordedByUserId = adminUser.Id,
                        Remarks = "Full semester fee cleared."
                    };
                    context.FeePayments.Add(pay2);

                    // Complaint for Student 2
                    var comp2 = new Complaint
                    {
                        StudentId = student2.Id,
                        Category = "Internet / Wi-Fi",
                        Priority = "High",
                        Subject = "Wi-Fi access point weak signal in Block A corridor",
                        Description = "The signal strength drops frequently during evening study hours in Block A.",
                        Status = "Resolved",
                        SubmissionDate = DateTime.UtcNow.AddDays(-6),
                        ResolvedDate = DateTime.UtcNow.AddDays(-4),
                        AssignedStaffId = blockA.AssignedWardenId,
                        ResolutionRemarks = "Repositioned corridor wireless repeater; speed verified at 95 Mbps."
                    };
                    context.Complaints.Add(comp2);

                    // Leave Request for Student 2 (Pending)
                    var leave2 = new LeaveRequest
                    {
                        StudentId = student2.Id,
                        StartDate = DateTime.UtcNow.AddDays(3),
                        EndDate = DateTime.UtcNow.AddDays(6),
                        Reason = "Attending Grace Hopper Women in Tech Conference",
                        Destination = "Bangalore Campus Center",
                        EmergencyContact = "+91 98333 99999",
                        ParentInformed = true,
                        Status = "Pending",
                        SubmissionDate = DateTime.UtcNow.AddHours(-12)
                    };
                    context.LeaveRequests.Add(leave2);

                    // Active Visitor currently inside for Student 2
                    var vis2 = new Visitor
                    {
                        StudentId = student2.Id,
                        VisitorName = "Mrs. Sunita Sharma",
                        ContactNumber = "+91 98333 88888",
                        RelationToStudent = "Mother",
                        PurposeOfVisit = "Parent-warden check-in and delivery of medical supplies",
                        EntryTime = DateTime.UtcNow.AddMinutes(-45),
                        ExitTime = null, // currently inside visitors' lounge!
                        RecordedByUserId = warden1User.Id,
                        Remarks = "Visitor lounge badge #07 issued."
                    };
                    context.Visitors.Add(vis2);
                }

                // Student 3: Riya Verma (Kasturba Hall, Bed 102-A)
                var s3Email = "riya.verma@hostelease.com";
                var s3User = new ApplicationUser
                {
                    UserName = s3Email,
                    Email = s3Email,
                    EmailConfirmed = true,
                    FullName = "Riya Verma",
                    UserType = "Student",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddMonths(-2)
                };
                if ((await userManager.CreateAsync(s3User, "Student@123")).Succeeded)
                {
                    await userManager.AddToRoleAsync(s3User, "Student");

                    var student3 = new Student
                    {
                        UserId = s3User.Id,
                        StudentId = "STU2026-GH103",
                        FullName = "Riya Verma",
                        Email = s3Email,
                        PhoneNumber = "+91 98455 12345",
                        Department = "Electronics & Communication",
                        AcademicYear = "2nd Year",
                        DateOfBirth = new DateTime(2006, 2, 10),
                        Gender = "Female",
                        GuardianName = "Mr. Ramesh Verma",
                        GuardianContact = "+91 98455 99999",
                        LocalGuardianName = "Mrs. Geeta Verma (Aunt)",
                        LocalGuardianContact = "+91 98555 66677",
                        LocalGuardianAddress = "Sector 21, Gandhinagar",
                        BloodGroup = "A+",
                        EmergencyContact = "+91 98455 99999",
                        Address = "Plot 12, Civil Lines, Jaipur, Rajasthan",
                        AdmissionDate = DateTime.UtcNow.AddMonths(-2),
                        IsActive = true
                    };
                    context.Students.Add(student3);
                    await context.SaveChangesAsync();

                    bed102A.Status = "Occupied";
                    var alloc3 = new RoomAllocation
                    {
                        StudentId = student3.Id,
                        BedId = bed102A.Id,
                        AllocationDate = DateTime.UtcNow.AddMonths(-2),
                        ExpectedVacatingDate = DateTime.UtcNow.AddMonths(10),
                        Status = "Active",
                        Remarks = "Room 102 Bed A allocation.",
                        AllocatedByUserId = adminUser.Id
                    };
                    context.RoomAllocations.Add(alloc3);

                    var s3Fee = new StudentFee
                    {
                        StudentId = student3.Id,
                        FeeStructureId = fsSemesterDouble.Id,
                        FeeTitle = $"{fsSemesterDouble.Title} (2026-2027 - Semester)",
                        DueDate = DateTime.UtcNow.AddDays(20),
                        TotalAmount = 51000.00m,
                        PaidAmount = 51000.00m,
                        BalanceAmount = 0.00m,
                        Status = "Paid",
                        CreatedAt = DateTime.UtcNow.AddMonths(-2)
                    };
                    context.StudentFees.Add(s3Fee);
                }

                // Student 4: Sneha Kulkarni (Unallocated female resident, ready for allocation demo)
                var s4Email = "sneha.kulkarni@hostelease.com";
                var s4User = new ApplicationUser
                {
                    UserName = s4Email,
                    Email = s4Email,
                    EmailConfirmed = true,
                    FullName = "Sneha Kulkarni",
                    UserType = "Student",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                };
                if ((await userManager.CreateAsync(s4User, "Student@123")).Succeeded)
                {
                    await userManager.AddToRoleAsync(s4User, "Student");

                    var student4 = new Student
                    {
                        UserId = s4User.Id,
                        StudentId = "STU2026-GH104",
                        FullName = "Sneha Kulkarni",
                        Email = s4Email,
                        PhoneNumber = "+91 99666 77889",
                        Department = "Computer Engineering",
                        AcademicYear = "1st Year",
                        DateOfBirth = new DateTime(2006, 9, 18),
                        Gender = "Female",
                        GuardianName = "Mr. Anand Kulkarni",
                        GuardianContact = "+91 99666 99999",
                        LocalGuardianName = "Mrs. Neha Kulkarni",
                        LocalGuardianContact = "+91 98666 77788",
                        LocalGuardianAddress = "A-102, Shanti Park, Pune",
                        BloodGroup = "AB+",
                        EmergencyContact = "+91 99666 99999",
                        Address = "Shivaji Nagar, Pune, Maharashtra",
                        AdmissionDate = DateTime.UtcNow.AddDays(-5),
                        IsActive = true
                    };
                    context.Students.Add(student4);
                    await context.SaveChangesAsync();

                    var s4Fee = new StudentFee
                    {
                        StudentId = student4.Id,
                        FeeStructureId = fsSemesterTriple.Id,
                        FeeTitle = $"{fsSemesterTriple.Title} (2026-2027 - Semester)",
                        DueDate = DateTime.UtcNow.AddDays(10),
                        TotalAmount = 44500.00m,
                        PaidAmount = 0.00m,
                        BalanceAmount = 44500.00m,
                        Status = "Unpaid",
                        CreatedAt = DateTime.UtcNow.AddDays(-5)
                    };
                    context.StudentFees.Add(s4Fee);
                }

                // 8. Seed Announcements (Tailored for Girls' Hostel)
                var ann1 = new Announcement
                {
                    Title = "Evening Curfew & Biometric Roll-Call Notice (8:30 PM)",
                    Content = "All female residents are reminded that evening biometric roll-call is conducted strictly between 8:00 PM and 8:30 PM at the main security desk. Late entries require prior permission from the Chief Warden.",
                    TargetAudience = "All",
                    Priority = "Urgent",
                    PublishDate = DateTime.UtcNow.AddDays(-2),
                    PublishedByUserId = adminUser.Id,
                    IsActive = true
                };

                var ann2 = new Announcement
                {
                    Title = "Weekend Home Leave & Gate Pass Guidelines",
                    Content = "Applications for weekend outstation leaves must be submitted via the portal 24 hours prior. Parental verbal or SMS authorization will be verified by the supervisory warden before physical gate passes are activated.",
                    TargetAudience = "All",
                    Priority = "Important",
                    PublishDate = DateTime.UtcNow.AddDays(-1),
                    PublishedByUserId = adminUser.Id,
                    IsActive = true
                };

                var ann3 = new Announcement
                {
                    Title = "Women's Wellness & Self-Defense Workshop",
                    Content = "The campus Women's Development Cell (WDC) is organizing an interactive wellness and self-defense workshop this Saturday at 4:30 PM in the Kasturba Hall common recreation center.",
                    TargetAudience = "All",
                    Priority = "Normal",
                    PublishDate = DateTime.UtcNow,
                    PublishedByUserId = adminUser.Id,
                    IsActive = true
                };

                context.Announcements.AddRange(ann1, ann2, ann3);

                // 9. Seed Audit Logs
                context.AuditLogs.Add(new AuditLog
                {
                    UserId = adminUser.Id,
                    UserEmail = adminEmail,
                    Action = "SystemSetup",
                    EntityName = "Database",
                    EntityId = "Init",
                    Details = "HostelEase Girls' Hostel Management System database initialized with role security, female halls of residence, and resident profiles.",
                    Timestamp = DateTime.UtcNow
                });

                await context.SaveChangesAsync();
            }
        }
    }
}
