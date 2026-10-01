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
        public string Password { get; set; } = null!;
        public UserRole Role { get; set; } = UserRole.Customer;
    }
}