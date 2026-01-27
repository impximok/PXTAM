using Invexaaa.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using ZXing;
using ZXing.Common;
using ZXing.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;
using System.IO;

namespace Invexaaa.Controllers
{
    public class HelpController : Controller
    {
        private readonly IConfiguration _configuration;

        // ===============================
        // Constructor
        // ===============================
        public HelpController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ===============================
        // HELP PAGE
        // ===============================
        public IActionResult HelpIndex()
        {
            return View();
        }

        // =====================================================
        // SAFARI-SAFE PDF VIEW (INLINE)
        // =====================================================
        [AllowAnonymous]
        [HttpGet]
        public IActionResult DownloadUserGuide()
        {
            var document = new InvexaUserGuidePdf();
            var pdfBytes = document.GeneratePdf();

            // REQUIRED for iOS Safari
            Response.Headers["Content-Disposition"] =
                "inline; filename=Invexa_User_Guide.pdf";
            Response.Headers["Content-Length"] =
                pdfBytes.Length.ToString();

            return File(pdfBytes, "application/pdf");
        }

        // =====================================================
        // QR CODE FOR USER GUIDE DOWNLOAD
        // (Uses ngrok / public URL from config)
        // =====================================================
        [AllowAnonymous]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
        public IActionResult UserGuideQr()
        {
            // 🔑 Get public base URL (ngrok / production)
            var publicBaseUrl = _configuration["PublicBaseUrl"];

            // Fallback (just in case)
            if (string.IsNullOrWhiteSpace(publicBaseUrl))
            {
                publicBaseUrl = $"{Request.Scheme}://{Request.Host}";
            }

            var downloadUrl =
                $"{publicBaseUrl}/Help/DownloadUserGuide";

            var writer = new ZXing.ImageSharp.BarcodeWriter<Rgba32>
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new EncodingOptions
                {
                    Width = 240,
                    Height = 240,
                    Margin = 2
                }
            };

            using var image = writer.Write(downloadUrl);
            using var ms = new MemoryStream();

            image.Save(ms, new PngEncoder());

            return File(ms.ToArray(), "image/png");
        }
    }
}
