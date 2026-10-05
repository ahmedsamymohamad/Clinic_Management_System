using Clinic_Management_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Clinic_Management_System.Data;

public sealed class ClinicDbContext(DbContextOptions<ClinicDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Doctor>().HasIndex(x => x.LicenseNumber).IsUnique();
        modelBuilder.Entity<Patient>().HasOne(x => x.User).WithOne(x => x.Patient)
            .HasForeignKey<Patient>(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Doctor>().HasOne(x => x.User).WithOne(x => x.Doctor)
            .HasForeignKey<Doctor>(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Appointment>().HasOne(x => x.Patient).WithMany(x => x.Appointments)
            .HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Appointment>().HasOne(x => x.Doctor).WithMany(x => x.Appointments)
            .HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Appointment>().HasOne(x => x.MedicalRecord).WithOne(x => x.Appointment)
            .HasForeignKey<MedicalRecord>(x => x.AppointmentId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<MedicalRecord>().HasMany(x => x.Prescriptions).WithOne(x => x.MedicalRecord)
            .HasForeignKey(x => x.MedicalRecordId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Prescription>().HasMany(x => x.Items).WithOne(x => x.Prescription)
            .HasForeignKey(x => x.PrescriptionId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Appointment>().HasIndex(x => new { x.DoctorId, x.StartsAt });
        modelBuilder.Entity<MedicalRecord>().HasIndex(x => x.AppointmentId).IsUnique();
        modelBuilder.Entity<Department>().HasData(
            new Department { Id = 1, Name = "General Medicine" },
            new Department { Id = 2, Name = "Cardiology" });
        modelBuilder.Entity<User>().HasData(
            new User { Id = 1, FullName = "Dr. Ahmed Hassan", Email = "ahmed@clinic.local", PasswordHash = "development-only", Role = UserRole.Doctor },
            new User { Id = 2, FullName = "Mariam Patient", Email = "mariam@clinic.local", PasswordHash = "development-only", Role = UserRole.Patient });
        modelBuilder.Entity<Doctor>().HasData(new Doctor { Id = 1, UserId = 1, DepartmentId = 1, LicenseNumber = "LIC-001" });
        modelBuilder.Entity<Patient>().HasData(new Patient { Id = 1, UserId = 2, PhoneNumber = "01000000000", DateOfBirth = new DateOnly(1995, 5, 20) });
    }
}
