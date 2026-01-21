using Microsoft.AspNetCore.Mvc;

namespace Invexaaa.Controllers;

public class HomeController : Controller
{
    // Startup splash screen
    public IActionResult Welcome() => View();

    // Optional pages (keep if you still use them)
    public IActionResult About() => View();
    public IActionResult Contact() => View();
    public IActionResult ChatPanel() => View();
}
