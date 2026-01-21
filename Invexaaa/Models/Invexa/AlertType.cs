using System.ComponentModel.DataAnnotations;

namespace Invexaaa.Models.Invexa
{
    public class AlertType
    {
        [Key]
        public int AlertTypeID { get; set; }

        [Required(ErrorMessage = "Alert type name is required.")]
        [MaxLength(50, ErrorMessage = "Alert type name must not exceed 50 characters.")]
        public string AlertTypeName { get; set; } = string.Empty;
    }
}
