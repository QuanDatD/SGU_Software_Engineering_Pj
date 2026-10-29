using System.ComponentModel.DataAnnotations;

namespace Zoo.BUS.DTOs;

public sealed class LoginRequest
{
    [Required]
    [StringLength(100)]
    public string Username { get; set; } = "";

    [Required]
    [StringLength(256)]
    public string Password { get; set; } = "";
}

public sealed class AdminResponse
{
    public Guid Id { get; init; }

    public string Username { get; init; } = "";

    public string Role { get; init; } = "";
}