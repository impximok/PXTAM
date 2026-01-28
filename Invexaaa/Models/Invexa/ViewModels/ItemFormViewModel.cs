using System.Collections.Generic;
using Invexaaa.Models.Invexa;
using Microsoft.AspNetCore.Http;
namespace Invexaaa.Models.ViewModels
{
    public class ItemFormViewModel
    {
        public Item Item { get; set; } = new();

        public IFormFile? ImageFile { get; set; }
        // ✅ edited image from JS (Base64)
        public string? EditedImageData { get; set; }
        public List<Category> Categories { get; set; } = new();
        public List<Supplier> Suppliers { get; set; } = new();
    }
}
