using Microsoft.AspNetCore.Mvc;
using ZXing;
using ZXing.Common;
using ZXing.Rendering;
using System.Text;

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
    }
}
