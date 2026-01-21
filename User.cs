
using System.ComponentModel.DataAnnotations;

public class User
{
    [Range(1, int.MaxValue, ErrorMessage = "UserId must be a positive integer.")]
    public int UserId { get; set; }

    [Required, MinLength(2), MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(100)]
    public string Email { get; set; } = string.Empty;
}
