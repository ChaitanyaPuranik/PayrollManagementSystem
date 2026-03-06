using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using PayrollManagementSystem.Areas.Identity.Data;
using PayrollManagementSystem.Services;
using PayrollManagementSystem.Data;
using PayrollManagementSystem.Services.Interfaces;
using PayrollManagementSystem.Services.Implementations;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("IdentityDbContextConnection")
    ?? throw new InvalidOperationException("Connection string 'IdentityDbContextConnection' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IPayrollService, PayrollService>();
builder.Services.AddScoped<ITimesheetService, TimesheetService>();

builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Configure authorization policies used to restrict access based on user roles.
// These policies define what role a user must have in order to access protected resources.

builder.Services.AddAuthorization(options =>
{
    // Policy: AdminOnly
    // Requires the logged-in user to have the "Admin" role.
    // Any page or folder using this policy will only be accessible to Admin users.
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    // Policy: UserOnly
    // Requires the logged-in user to have the "User" role.
    // Any page or folder using this policy will only be accessible to general users.
    options.AddPolicy("UserOnly", policy =>
        policy.RequireRole("User"));
});

// Configure Razor Pages conventions for folder-level authorization.
// Instead of putting [Authorize] attributes on every page, we secure entire folders here.

builder.Services.AddRazorPages(options =>
{
    // Protect all pages inside /Pages/Admin/*
    // Only users satisfying the "AdminOnly" policy (Admin role) can access these pages.
    options.Conventions.AuthorizeFolder("/Admin", "AdminOnly");

    // Protect all pages inside /Pages/User/*
    // Only users satisfying the "UserOnly" policy (User role) can access these pages.
    options.Conventions.AuthorizeFolder("/User", "UserOnly");
});

builder.Services.AddTransient<IEmailSender, EmailSender>();
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error"); // Razor Pages default is /Error if you scaffold it
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// Seed roles + admin
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

    foreach (var role in new[] { "Admin", "User" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    string email = "admin@payroll.com";
    string pass = "Admin@123";

    var existing = await userManager.FindByEmailAsync(email);
    if (existing == null)
    {
        var user = new AppUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, pass);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(user, "Admin");
    }
}

app.Run();