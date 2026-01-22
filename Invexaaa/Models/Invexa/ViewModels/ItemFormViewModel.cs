using System.Collections.Generic;
using Invexaaa.Models.Invexa;

namespace Invexaaa.Models.ViewModels
{
    public class ItemFormViewModel
    {
        public Item Item { get; set; } = new();

        public List<Category> Categories { get; set; } = new();
        public List<Supplier> Suppliers { get; set; } = new();
    }
}
