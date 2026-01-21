using System.ComponentModel.DataAnnotations;
using SnomiAssignmentReal.Validation;

namespace SnomiAssignmentReal.Models.ViewModels;

public class UpdatePasswordVM
{
    [StringLength(100, MinimumLength = 5)]
    [DataType(DataType.Password)]
    [Display(Name = "Current HashedPassword")]
    public string Current { get; set; }

    [StringLength(100, MinimumLength = 5)]
    [DataType(DataType.Password)]
    [PasswordComplexity]
    [Display(Name = "New HashedPassword")]
    public string New { get; set; }

    [StringLength(100, MinimumLength = 5)]
    [Compare("New")]
    [DataType(DataType.Password)]
    [PasswordComplexity]
    [Display(Name = "Confirm HashedPassword")]
    public string Confirm { get; set; }
}