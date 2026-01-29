using Microsoft.AspNetCore.Mvc;
using System.Text;
using ZXing;
using ZXing.Common;
using ZXing.ImageSharp;
using ZXing.QrCode;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;

namespace Invexaaa.Controllers
{
    [Route("Barcode")]
    public class BarcodeController : Controller
    {
        [HttpGet("Generate")]
        public IActionResult Generate(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest();

            var writer = new BarcodeWriterSvg
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Width = 300,
                    Height = 100,
                    Margin = 2
                }
            };

            var svgImage = writer.Write(code);

            return File(
                Encoding.UTF8.GetBytes(svgImage.Content),
                "image/svg+xml"
            );
        }

        // =====================================================
        // QR CODE (UNCHANGED / SAME PATTERN)
        // =====================================================
        [HttpGet("GenerateQr")]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
        public IActionResult GenerateQr(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest();

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

            using var image = writer.Write(code);
            using var ms = new MemoryStream();

            image.Save(ms, new PngEncoder());

            return File(ms.ToArray(), "image/png");
        }
    }
}
