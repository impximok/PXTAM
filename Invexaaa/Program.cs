global using Invexaaa.Models;
global using Invexaaa.Models.Invexa;
using Invexaaa.Helpers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Rotativa.AspNetCore;
using Invexaaa.Data;


var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// DbContext
builder.Services.AddDbContext<InvexaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Auth

builder.Services.AddAuthentication("MyCookieAuth")
    .AddCookie("MyCookieAuth", options =>
    {
        options.Cookie.Name = "InvexaAuthCookie";
        options.LoginPath = "/User/Login";
        options.LogoutPath = "/User/Logout";          //  add (used by SignOut)
      //  options.AccessDeniedPath = "/User/AccessDenied"; //  add (clean 403 UX)
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

RotativaConfiguration.Setup(
    builder.Environment.WebRootPath,
    "Rotativa"
);

var app = builder.Build();



// Pipeline
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRotativa();
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
    var context = scope.ServiceProvider.GetRequiredService<InvexaDbContext>();
    context.Database.Migrate();
    InvexaDbSeeder.Seed(context);
}

app.Run();
