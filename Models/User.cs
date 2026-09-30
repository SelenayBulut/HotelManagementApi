using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;



namespace HotelManagementApi.Models
{

    // Sistemdeki kullanıcıları temsil eder.
    public class User
    {
        [Key] //primary key
        public int Id { get; set; }

        [Required]  //boş kalmamalı
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        // Kullanıcının gerçek şifresi yerine hashlenmiş hali tutulur.
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        // Admin, HotelOwner veya Customer.
       public UserRole Role { get; set; } = UserRole.Customer;

        // Kullanıcının sistem içerisindeki bakiyesi.
        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; } = 0.00m;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties (Tablolar arası ilişkiler için)
        public ICollection<Hotel> Hotels { get; set; } = new List<Hotel>();
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}