using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagementApi.Models
{

    // Sistemdeki rezervasyonlara ait ödeme, iade ve ceza işlemlerini temsil eder.
    public class Payment
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ReservationId { get; set; } // Hangi rezervasyona ait olduğu (Foreign Key)

        [Required]
        public Guid UserId { get; set; } // İşlemi yapan kullanıcı (Foreign Key)

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; } // İşlem tutarı

        public TransactionType TransactionType { get; set; } //işlem türü 

        public PaymentStatus Status { get; set; } = PaymentStatus.Success;  //ödeme durumu
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false;
        
       // Navigation Properties
        [ForeignKey("ReservationId")]
        public Reservation Reservation { get; set; } = null!; // İlgili rezervasyon

        [ForeignKey("UserId")]
        public User User { get; set; } = null!; // İşlemi yapan kullanıcı
    }
}