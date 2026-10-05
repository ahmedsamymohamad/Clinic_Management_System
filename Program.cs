using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration.Json;
using Clinic_Management_System.Data;
using Clinic_Management_System.Models;

var builder = WebApplication.CreateBuilder(args);

var jsonSettings = builder.Configuration.Sources
    .OfType<JsonConfigurationSource>()
    .Where(source =>
        source.Path?.Equals("appsettings.json", StringComparison.OrdinalIgnoreCase) == true ||
        source.Path?.Equals("appsettings.Development.json", StringComparison.OrdinalIgnoreCase) == true)
    .ToList();
foreach (var settings in jsonSettings)
    builder.Configuration.Sources.Remove(settings);
builder.Configuration.AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true);

builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter()));
var clinicConnectionString = builder.Configuration.GetConnectionString("ClinicDatabase")
    ?? throw new InvalidOperationException("ConnectionStrings:ClinicDatabase is required.");
builder.Services.AddDbContext<ClinicDbContext>(options => options.UseSqlServer(clinicConnectionString));
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<Clinic_Management_System.Services.IAppointmentService,
    Clinic_Management_System.Services.AppointmentService>();
builder.Services.AddProblemDetails();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
    var adminEmail = builder.Configuration["AdminAccount:Email"] ?? "admin@clinic.local";
    if (!db.Users.Any(x => x.Email == adminEmail))
    {
        var admin = new User
        {
            FullName = builder.Configuration["AdminAccount:FullName"] ?? "Clinic Administrator",
            Email = adminEmail,
            Role = UserRole.Admin
        };
        admin.PasswordHash = new PasswordHasher<User>().HashPassword(
            admin, builder.Configuration["AdminAccount:Password"] ?? "Admin123!");
        db.Users.Add(admin);
        db.SaveChanges();
    }
    var passwordHasher = new PasswordHasher<User>();
    var seededDoctor = db.Users.SingleOrDefault(x => x.Email == "ahmed@clinic.local" && x.PasswordHash == "development-only");
    if (seededDoctor is not null)
    {
        seededDoctor.PasswordHash = passwordHasher.HashPassword(seededDoctor, "Doctor123!");
        db.SaveChanges();
    }
    var seededPatient = db.Users.SingleOrDefault(x => x.Email == "mariam@clinic.local" && x.PasswordHash == "development-only");
    if (seededPatient is not null)
    {
        seededPatient.PasswordHash = passwordHasher.HashPassword(seededPatient, "Patient123!");
        db.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
