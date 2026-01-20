
using Microsoft.AspNetCore.Mvc;

namespace SnomiAssignmentReal.Controllers;

public class HomeController : Controller
{
    // Landing Page
    public IActionResult Welcome() => View();

    // About Us page
    public IActionResult About() => View();

    // Contact Us page
    public IActionResult Contact() => View();

    // Optional: Display static Chat page
    public IActionResult ChatPanel() => View();

    // 🔑 Staff or Admin go to general login
    public IActionResult StaffEntry()
    {
        return RedirectToAction("Login", "Account"); // Staff/Admin login
    }

    // 👥 Customers go to separate login
    public IActionResult CustomerEntry()
    {
        return RedirectToAction("Login", "Customer"); // Customer login
    }
}
