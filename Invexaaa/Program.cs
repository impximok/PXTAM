global using SnomiAssignmentReal.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Rotativa.AspNetCore;
using SnomiAssignmentReal.Data;
using SnomiAssignmentReal.Helpers;    // <-- needed to reference the verifiers
using SnomiAssignmentReal.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Auth
builder.Services.AddAuthentication("MyCookieAuth")
    .AddCookie("MyCookieAuth", options =>
    {
        options.Cookie.Name = "SnomiAuthCookie";
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";          //  add (used by SignOut)
        options.AccessDeniedPath = "/Account/AccessDenied"; //  add (clean 403 UX)
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

builder.Services.AddMemoryCache();

// --- BOT DEFENSE SERVICES ---
builder.Services.AddHttpClient();                 // required for API calls
builder.Services.AddTransient<HCaptchaVerifier>();

builder.Services.AddHttpContextAccessor();

// Session
builder.Services.AddSession();

var app = builder.Build();



// Pipeline
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Welcome}/{id?}");

app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["Permissions-Policy"] = "camera=(self)"; // or "camera=*"
    await next();
});

// Seed
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    DbInitializer.Seed(context);
}

app.Run();
