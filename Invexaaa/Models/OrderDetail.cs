using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnomiAssignmentReal.Models;

public class OrderDetail
{

    [Key]
    public string OrderDetailId { get; set; }

    [Required]
    public string CustomerOrderId { get; set; }
    public CustomerOrder CustomerOrder { get; set; }

    [Required(ErrorMessage = "OrderedQuantity is required.")]
    [Range(1, 100, ErrorMessage = "OrderedQuantity must be between 1 and 100.")]
    public int OrderedQuantity { get; set; }

    [Required]
    public string MenuItemId { get; set; }
    public MenuItem MenuItem { get; set; }

    // ✅ Correct many-to-many or one-to-many relationship (adjust as needed)
    public ICollection<OrderCustomizationSettings> AppliedCustomizations { get; set; }

}
