
using Microsoft.EntityFrameworkCore;
using HotelManagementApi.Models;

// Uygulamadaki modeller ile SQL Server veritabanı arasındaki bağlantıyı yönetir.

// Soft delete için IsDeleted alanları kullanılır.
// Fiziksel silme yapılmaz, kayıtlar IsDeleted = true olarak işaretlenir.

namespace HotelManagementApi.Data
{
    // Program.cs tarafından verilen veritabanı bağlantı ayarlarını DbContext'e aktarır.
    public class AppDbContext : DbContext
    {

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Veritabanındaki Tablolarımız (DbSet'ler)
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Hotel> Hotels { get; set; } = null!;
        public DbSet<Room> Rooms { get; set; } = null!;
        public DbSet<Reservation> Reservations { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;

        // Modellerin ve tabloların veritabanında nasıl yapılandırılacağını belirler.
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User tablosu ve Role enum dönüşümü
            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.Role)
                      .HasConversion<string>(); // Enum'ı string olarak kaydeder

                // Soft Delete
                // SQL Server'da false değeri 0 olarak tutulur.
                entity.Property(e => e.IsDeleted)
                      .HasDefaultValue(false);
            });

            // Room tablosu ve RoomType enum dönüşümü
            modelBuilder.Entity<Room>(entity =>
            {
                entity.Property(e => e.RoomType)
                      .HasConversion<string>();

                // Soft Delete
                entity.Property(e => e.IsDeleted)
                      .HasDefaultValue(false);
            });

            // Reservation tablosu ve Status enum dönüşümü
            modelBuilder.Entity<Reservation>(entity =>
            {
                entity.Property(e => e.Status)
                      .HasConversion<string>();

                // Soft Delete
                entity.Property(e => e.IsDeleted)
                      .HasDefaultValue(false);
            });

            // Payment tablosu ve Enum dönüşümleri
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.Property(e => e.TransactionType)
                      .HasConversion<string>();

                entity.Property(e => e.Status)
                      .HasConversion<string>();

                // Soft Delete
                entity.Property(e => e.IsDeleted)
                      .HasDefaultValue(false);
            });

            // Hotel tablosu
            modelBuilder.Entity<Hotel>(entity =>
            {
                entity.Property(h => h.Rating)
                      .HasPrecision(3, 2);

                // Soft Delete
                entity.Property(e => e.IsDeleted)
                      .HasDefaultValue(false);
            });

            modelBuilder.Entity<Room>()
                .Property(r => r.PricePerNight)
                .HasPrecision(18, 2);

            // Çoklu Cascade Path hatasını önlemek için tüm ilişkilerde
            // silme davranışını Restrict (kısıtlı) olarak ayarlıyoruz.
            // Böylece ilişkili kayıtlar varken ana kaydın yanlışlıkla silinmesi engellenir.
            //
            // Soft delete kullandığımız için fiziksel silme işlemi
            // Controller'larda IsDeleted alanı üzerinden yapılmaktadır.
            foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }
}
