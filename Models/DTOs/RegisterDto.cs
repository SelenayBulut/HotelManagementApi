using HotelManagementApi.Models;
using System.ComponentModel.DataAnnotations;

namespace HotelManagementApi.DTOs 
{

    // Kullanıcının sisteme kayıt olurken API'ye göndereceği bilgileri taşır.
    public class RegisterDto
    {
        public string Username { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Şifre alanı zorunludur.")]
        [MinLength(8, ErrorMessage = "Şifre en az 8 karakter olmalıdır.")]
        public string Password { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Customer;
    }
}