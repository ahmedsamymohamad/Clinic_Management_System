using System.Security.Claims;
using Clinic_Management_System.Data;
using Clinic_Management_System.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clinic_Management_System.Controllers;

public sealed class AccountController(ClinicDbContext db, IPasswordHasher<User> hasher) : Controller
{
    [HttpGet] public IActionResult Register() => View(new RegisterViewModel { DateOfBirth = new DateOnly(1990, 1, 1) });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        var email = model.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Email == email, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
            return View(model);
        }
        var user = new User { FullName = model.FullName.Trim(), Email = email, Role = UserRole.Patient };
        user.PasswordHash = hasher.HashPassword(user, model.Password);
        user.Patient = new Patient { PhoneNumber = model.PhoneNumber.Trim(), DateOfBirth = model.DateOfBirth };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        await SignInAsync(user, false);
        return RedirectToAction("Index", "Clinic");
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpGet]
    public async Task<IActionResult> DoctorRegister(CancellationToken cancellationToken)
    {
        ViewBag.Departments = await db.Departments.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
        return View(new DoctorRegisterViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DoctorRegister(DoctorRegisterViewModel model, CancellationToken cancellationToken)
    {
        ViewBag.Departments = await db.Departments.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
        if (!ModelState.IsValid) return View(model);
        var email = model.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Email == email, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
            return View(model);
        }
        if (!await db.Departments.AnyAsync(x => x.Id == model.DepartmentId, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.DepartmentId), "Select a valid department.");
            return View(model);
        }
        var user = new User { FullName = model.FullName.Trim(), Email = email, Role = UserRole.Doctor };
        user.PasswordHash = hasher.HashPassword(user, model.Password);
        user.Doctor = new Doctor { DepartmentId = model.DepartmentId, LicenseNumber = model.LicenseNumber.Trim() };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        await SignInAsync(user, false);
        return RedirectToAction("Index", "Doctor");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }
        var email = model.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user is null || hasher.VerifyHashedPassword(user, user.PasswordHash, model.Password) == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }
        await SignInAsync(user, model.RememberMe);
        var destination = user.Role switch
        {
            UserRole.Admin => RedirectToAction("Index", "Admin"),
            UserRole.Doctor => RedirectToAction("Index", "Doctor"),
            _ => RedirectToAction("Index", "Clinic")
        };
        return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : destination;
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    public IActionResult AccessDenied() => View();

    private async Task SignInAsync(User user, bool persistent)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties { IsPersistent = persistent });
    }
}
