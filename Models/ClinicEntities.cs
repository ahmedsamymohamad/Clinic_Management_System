using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.Models;

public enum UserRole { Admin, Doctor, Patient, Receptionist }
public enum AppointmentStatus { Pending, Accepted, Rejected, Completed, Cancelled }

public sealed class User
{
    public int Id { get; set; }
    [Required, MaxLength(120)] public string FullName { get; set; } = string.Empty;
    [Required, MaxLength(180)] public string Email { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public Patient? Patient { get; set; }
    public Doctor? Doctor { get; set; }
}

public sealed class RegisterViewModel
{
    [Required, MaxLength(120)] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress, MaxLength(180)] public string Email { get; set; } = string.Empty;
    [Required, Phone, MaxLength(30)] public string PhoneNumber { get; set; } = string.Empty;
    [Required, DataType(DataType.Date)] public DateOnly DateOfBirth { get; set; }
    [Required, DataType(DataType.Password), MinLength(8)] public string Password { get; set; } = string.Empty;
    [Required, DataType(DataType.Password), Compare(nameof(Password))] public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class LoginViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}

public sealed class DoctorRegisterViewModel
{
    [Required, MaxLength(120)] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress, MaxLength(180)] public string Email { get; set; } = string.Empty;
    [Required, MaxLength(120)] public string LicenseNumber { get; set; } = string.Empty;
    [Required] public int DepartmentId { get; set; }
    [Required, DataType(DataType.Password), MinLength(8)] public string Password { get; set; } = string.Empty;
    [Required, DataType(DataType.Password), Compare(nameof(Password))] public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class Department
{
    public int Id { get; set; }
    [Required, MaxLength(120)] public string Name { get; set; } = string.Empty;
    public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
}

public sealed class Patient
{
    public int Id { get; set; }
    public int UserId { get; set; }
    [Required, MaxLength(30)] public string PhoneNumber { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public User User { get; set; } = null!;
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
}

public sealed class Doctor
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int DepartmentId { get; set; }
    [Required, MaxLength(120)] public string LicenseNumber { get; set; } = string.Empty;
    public User User { get; set; } = null!;
    public Department Department { get; set; } = null!;
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}

public sealed class Appointment
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    [MaxLength(500)] public string? Reason { get; set; }
    public Patient Patient { get; set; } = null!;
    public Doctor Doctor { get; set; } = null!;
    public MedicalRecord? MedicalRecord { get; set; }
}

public sealed class MedicalRecord
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    [Required] public string Diagnosis { get; set; } = string.Empty;
    [Required] public string Notes { get; set; } = string.Empty;
    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;
    public Appointment Appointment { get; set; } = null!;
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
}

public sealed class Prescription
{
    public int Id { get; set; }
    public int MedicalRecordId { get; set; }
    public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;
    public MedicalRecord MedicalRecord { get; set; } = null!;
    public ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
}

public sealed class PrescriptionItem
{
    public int Id { get; set; }
    public int PrescriptionId { get; set; }
    [Required, MaxLength(160)] public string MedicationName { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string Dosage { get; set; } = string.Empty;
    [Required, MaxLength(120)] public string Frequency { get; set; } = string.Empty;
    public int DurationDays { get; set; }
    public Prescription Prescription { get; set; } = null!;
}
