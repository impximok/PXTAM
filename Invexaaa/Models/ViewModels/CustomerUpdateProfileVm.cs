using System.ComponentModel.DataAnnotations;

namespace SnomiAssignmentReal.Models.ViewModels
{
    public class CustomerUpdateProfileVm
    {

        public string? Email { get; set; }

        [StringLength(100)]
        public string Name { get; set; }

        public string? PhotoURL { get; set; }

        public IFormFile? Photo { get; set; }
    }
}
