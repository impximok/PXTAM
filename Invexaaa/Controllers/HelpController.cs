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
        public IActionResult HelpIndex()
        {
            return View();
        }

        // =====================================================
        // PUBLIC USER GUIDE DOWNLOAD (PDF)
        // Accessible via QR code without login
        // =====================================================
        [AllowAnonymous]
        public IActionResult DownloadUserGuide()
        {
            var document = new InvexaUserGuidePdf();
            var pdfBytes = document.GeneratePdf();

            return File(
                pdfBytes,
                "application/pdf",
                "Invexa_User_Guide.pdf"
            );
        }

        // =====================================================
        // QR CODE FOR USER GUIDE DOWNLOAD
        // =====================================================
        [AllowAnonymous]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
        public IActionResult UserGuideQr()
        {
            var writer = new ZXing.ImageSharp.BarcodeWriter<Rgba32>
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new EncodingOptions
                {
                    Height = 200,
                    Width = 200,
                    Margin = 1
                }
            };

            // NOTE:
            // Local IP is used so mobile devices on the same Wi-Fi network
            // can access the system during development.
            // In production, this will be replaced with the deployed domain.
            using var image = writer.Write(
                "http://192.168.88.90:5000/Help/DownloadUserGuide"
            );

            using var ms = new MemoryStream();
            image.Save(ms, new PngEncoder());

            return File(ms.ToArray(), "image/png");
        }
    }
}
