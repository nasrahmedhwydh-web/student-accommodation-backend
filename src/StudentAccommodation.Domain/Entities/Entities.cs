using System;

namespace StudentAccommodation.Domain.Entities
{
    public class User
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "Student";
        public string? College { get; set; }
        public string? Department { get; set; }
        public double GPA { get; set; }
    }

    public class AccommodationRequest
    {
        public int Id { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public User? Student { get; set; }
        public string AcademicStatus { get; set; } = "Pending";
        public string? AcademicStatusPath { get; set; }
        public string SecurityStatus { get; set; } = "Pending";
        public string? ClearanceStatusPath { get; set; }
        public string FinanceStatus { get; set; } = "Pending";
        public string? BankReceiptPath { get; set; }
        public string AllocationStatus { get; set; } = "Pending";
        public string Status { get; set; } = "Pending";
        public string? RoomInfo { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Room
    {
        public int Id { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string Building { get; set; } = string.Empty;
        public int Floor { get; set; }
        public int Capacity { get; set; } = 4;
    }

    public class Bed
    {
        public int Id { get; set; }
        public int RoomId { get; set; }
        public Room? Room { get; set; }
        public string BedNumber { get; set; } = string.Empty;
        public bool IsOccupied { get; set; } = false;
        public string? CurrentStudentId { get; set; }
    }

    public class MaintenanceTicket
    {
        public int Id { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public string? StudentName { get; set; }
        public string? RoomInfo { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Open";
        public string Priority { get; set; } = "Normal";
        public string? AdminReply { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
