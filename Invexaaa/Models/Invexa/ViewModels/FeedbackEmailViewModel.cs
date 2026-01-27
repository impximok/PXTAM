using System.ComponentModel.DataAnnotations;

namespace Invexaaa.Models.ViewModels
{
    public class FeedbackEmailViewModel
    {
        [Required]
        [StringLength(100)]
        public string Subject { get; set; }

        [Required]
        [StringLength(2000)]
        public string Message { get; set; }
    }
}
