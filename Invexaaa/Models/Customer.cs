using System.ComponentModel.DataAnnotations;

namespace SnomiAssignmentReal.Models;

public class Customer
{
    [Key]
    [MaxLength(10)]
    public string CustomerId { get; set; }

    // Optional: Useful even for guests if they choose to provide it
    [MaxLength(50)]
    public string? CustomerFullName { get; set; }

    // Only used for registered users
    [MaxLength(50)]
    public string? CustomerUserName { get; set; }

    // HashedPassword hash, null for guests
    [MaxLength(255)]
    public string? CustomerPasswordHash { get; set; }

    [MaxLength(255)]
    public string? CustomerProfileImageUrl { get; set; } // Optional

    // Flag to check if user is logged in
    public bool IsCustomerLoggedIn { get; set; } = false;

    // Optional: guests may or may not provide email
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [MaxLength(100)]
    public string? CustomerEmailAddress { get; set; }

    // Optional phone number
    [Phone(ErrorMessage = "Invalid phone number.")]
    [MaxLength(15)]
    public string? CustomerPhoneNumber { get; set; }

    // Points only for registered users (optional)
    public int CustomerRewardPoints { get; set; } = 0;

    // Navigation property: one customer can have many orders
    public ICollection<CustomerOrder>? CustomerOrders { get; set; }

    //Next: Generate CustomerId in controller (e.g., for guest or registration)

    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }

}
