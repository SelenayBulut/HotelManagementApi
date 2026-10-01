
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelManagementApi.Data;
using HotelManagementApi.Models;
using HotelManagementApi.DTOs;

namespace HotelManagementApi.Controllers
{
    // Otellerin görüntülenmesi, oluşturulması, güncellenmesi ve silinmesini yönetir.
    [Route("api/[controller]")]
    [ApiController]
    public class HotelsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<HotelsController> _logger;

        // Gerekli servisler Dependency Injection ile alınır.
        public HotelsController(
            AppDbContext context,
            ILogger<HotelsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/hotels
        // Silinmemiş tüm otelleri ve odalarını getirir.
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Hotel>>> GetHotels()
        {
            _logger.LogInformation(
                "Tüm aktif otelleri listeleme işlemi başlatıldı.");

            // Sadece silinmemiş oteller ve silinmemiş odalar getirilir.
            var hotels = await _context.Hotels
                .Where(h => !h.IsDeleted)
                .Include(h => h.Rooms.Where(r => !r.IsDeleted))
                .ToListAsync();

            _logger.LogInformation(
                "Aktif oteller başarıyla listelendi. Otel sayısı: {HotelCount}",
                hotels.Count);

            return hotels;
        }

        // GET: api/hotels/{id}
        // ID'si verilen ve silinmemiş oteli ve odalarını getirir.
        [HttpGet("{id}")]
        public async Task<ActionResult<Hotel>> GetHotel(Guid id)
        {
            _logger.LogInformation(
                "Otel bilgisi getirme işlemi başlatıldı. HotelId: {HotelId}",
                id);

            var hotel = await _context.Hotels
                .Where(h => !h.IsDeleted)
                .Include(h => h.Rooms.Where(r => !r.IsDeleted))
                .FirstOrDefaultAsync(h => h.Id == id);

            // Otel bulunamazsa 404 döndürülür.
            // Silinmiş oteller de burada bulunamaz.
            if (hotel == null)
            {
                _logger.LogWarning(
                    "Otel bulunamadı veya silinmiş durumda. HotelId: {HotelId}",
                    id);

                return NotFound(new
                {
                    message = "Otel bulunamadı."
                });
            }

            _logger.LogInformation(
                "Otel başarıyla getirildi. HotelId: {HotelId}, HotelName: {HotelName}",
                hotel.Id,
                hotel.Name);

            return hotel;
        }

        // POST: api/hotels
        // Yeni otel oluşturur.
        [HttpPost]
        [Authorize(Roles = "Admin,HotelOwner")]
        public async Task<ActionResult<Hotel>> CreateHotel(
            [FromBody] CreateHotelDto hotelDto)
        {
            _logger.LogInformation(
                "Yeni otel oluşturma işlemi başlatıldı. HotelName: {HotelName}",
                hotelDto.Name);

            // Token içerisinden giriş yapan kullanıcının ID'si alınır.
            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Token geçersizse işlem reddedilir.
            if (userIdClaim == null ||
                !Guid.TryParse(userIdClaim, out Guid userId))
            {
                _logger.LogWarning(
                    "Otel oluşturma başarısız. Geçersiz token.");

                return Unauthorized(new
                {
                    message = "Geçersiz token."
                });
            }

            // DTO'daki bilgilerle yeni otel oluşturulur.
            // OwnerId token'daki kullanıcıdan alınır.
            var hotel = new Hotel
            {
                Name = hotelDto.Name,
                Address = hotelDto.Address,
                City = hotelDto.City,
                Rating = hotelDto.Rating,
                Description = hotelDto.Description,
                OwnerId = userId,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _context.Hotels.Add(hotel);

            try
            {
                // Yeni otel veritabanına kaydedilir.
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Otel başarıyla oluşturuldu. HotelId: {HotelId}, HotelName: {HotelName}, OwnerId: {OwnerId}",
                    hotel.Id,
                    hotel.Name,
                    userId);
            }
            catch (DbUpdateException ex)
            {
                // Veritabanı kaynaklı kayıt hatalarını yakalar.
                _logger.LogError(
                    ex,
                    "Otel oluşturulurken veritabanı hatası oluştu. OwnerId: {OwnerId}, HotelName: {HotelName}",
                    userId,
                    hotelDto.Name);

                return BadRequest(new
                {
                    message = "Otel oluşturulurken veritabanı hatası oluştu."
                });
            }

            return CreatedAtAction(
                nameof(GetHotel),
                new { id = hotel.Id },
                hotel);
        }

        // PUT: api/hotels/{id}
        // Mevcut ve silinmemiş otelin bilgilerini günceller.
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,HotelOwner")]
        public async Task<IActionResult> UpdateHotel(
            Guid id,
            [FromBody] UpdateHotelDto updateDto)
        {
            _logger.LogInformation(
                "Otel güncelleme işlemi başlatıldı. HotelId: {HotelId}",
                id);

            // Güncellenecek aktif otel bulunur.
            var hotel = await _context.Hotels
                .FirstOrDefaultAsync(h =>
                    h.Id == id &&
                    !h.IsDeleted);

            if (hotel == null)
            {
                _logger.LogWarning(
                    "Güncellenecek otel bulunamadı veya silinmiş durumda. HotelId: {HotelId}",
                    id);

                return NotFound(new
                {
                    message =
                        "Güncellenecek otel bulunamadı."
                });
            }

            // Token içerisinden giriş yapan kullanıcının ID'si alınır.
            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdClaim == null ||
                !Guid.TryParse(userIdClaim, out Guid userId))
            {
                _logger.LogWarning(
                    "Otel güncelleme başarısız. Geçersiz token. HotelId: {HotelId}",
                    id);

                return Unauthorized(new
                {
                    message = "Geçersiz token."
                });
            }

            // HotelOwner yalnızca kendi otelini güncelleyebilir.
            // Admin için bu sahiplik kontrolü uygulanmaz.
            if (!User.IsInRole("Admin") &&
                hotel.OwnerId != userId)
            {
                _logger.LogWarning(
                    "Yetkisiz otel güncelleme denemesi. HotelId: {HotelId}, UserId: {UserId}, OwnerId: {OwnerId}",
                    id,
                    userId,
                    hotel.OwnerId);

                return Forbid();
            }

            // Sadece gönderilen alanlar güncellenir.
            if (!string.IsNullOrEmpty(updateDto.Name))
                hotel.Name = updateDto.Name;

            if (!string.IsNullOrEmpty(updateDto.Address))
                hotel.Address = updateDto.Address;

            if (!string.IsNullOrEmpty(updateDto.City))
                hotel.City = updateDto.City;

            if (updateDto.Rating.HasValue)
                hotel.Rating = updateDto.Rating.Value;

            if (!string.IsNullOrEmpty(updateDto.Description))
                hotel.Description = updateDto.Description;

            try
            {
                // Güncellenen bilgiler veritabanına kaydedilir.
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Otel başarıyla güncellendi. HotelId: {HotelId}, UserId: {UserId}",
                    id,
                    userId);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Otel güncellenirken veritabanı hatası oluştu. HotelId: {HotelId}, UserId: {UserId}",
                    id,
                    userId);

                return BadRequest(new
                {
                    message =
                        "Otel güncellenirken veritabanı hatası oluştu."
                });
            }

            return Ok(new
            {
                message =
                    "Otel bilgileri başarıyla güncellendi.",
                hotel
            });
        }

        // DELETE: api/hotels/{id}
        // Oteli ve ona bağlı kayıtları fiziksel olarak silmez.
        // IsDeleted = true yaparak soft delete gerçekleştirir.
        //
        // Otelin aktif rezervasyonları varsa,
        // müşterilere tam para iadesi yapılır.
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,HotelOwner")]
        public async Task<IActionResult> DeleteHotel(Guid id)
        {
            _logger.LogInformation(
                "Otel soft delete işlemi başlatıldı. HotelId: {HotelId}",
                id);

            // --------------------------------------------------
            // 1. SİLİNECEK AKTİF OTELİ BUL
            // --------------------------------------------------
            var hotel = await _context.Hotels
                .FirstOrDefaultAsync(h =>
                    h.Id == id &&
                    !h.IsDeleted);

            if (hotel == null)
            {
                _logger.LogWarning(
                    "Silinecek otel bulunamadı veya zaten silinmiş durumda. HotelId: {HotelId}",
                    id);

                return NotFound(new
                {
                    message =
                        "Silinecek otel bulunamadı."
                });
            }

            // --------------------------------------------------
            // 2. TOKEN İÇERİSİNDEN KULLANICI ID'SİNİ AL
            // --------------------------------------------------
            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdClaim == null ||
                !Guid.TryParse(userIdClaim, out Guid userId))
            {
                _logger.LogWarning(
                    "Otel silme başarısız. Geçersiz token. HotelId: {HotelId}",
                    id);

                return Unauthorized(new
                {
                    message = "Geçersiz token."
                });
            }

            // --------------------------------------------------
            // 3. HOTEL OWNER SADECE KENDİ OTELİNİ SİLEBİLİR
            // ADMIN İÇİN SAHİPLİK KONTROLÜ YOK
            // --------------------------------------------------
            if (!User.IsInRole("Admin") &&
                hotel.OwnerId != userId)
            {
                _logger.LogWarning(
                    "Yetkisiz otel silme denemesi. HotelId: {HotelId}, UserId: {UserId}, OwnerId: {OwnerId}",
                    id,
                    userId,
                    hotel.OwnerId);

                return Forbid();
            }

            try
            {
                // --------------------------------------------------
                // 4. OTELİ SOFT DELETE
                // --------------------------------------------------
                hotel.IsDeleted = true;

                // --------------------------------------------------
                // 5. OTELİN AKTİF ODALARINI BUL
                // --------------------------------------------------
                var rooms = await _context.Rooms
                    .Where(r =>
                        r.HotelId == hotel.Id &&
                        !r.IsDeleted)
                    .ToListAsync();

                // --------------------------------------------------
                // 6. ODALARI SOFT DELETE
                // --------------------------------------------------
                foreach (var room in rooms)
                {
                    room.IsDeleted = true;
                }

                // --------------------------------------------------
                // 7. ODALARA AİT AKTİF REZERVASYONLARI BUL
                //
                // User bilgisi de Include edilir çünkü
                // müşterinin bakiyesine iade yapılacaktır.
                // --------------------------------------------------
                var roomIds = rooms
                    .Select(r => r.Id)
                    .ToList();

                var reservations = await _context.Reservations
                    .Include(r => r.User)
                    .Where(r =>
                        roomIds.Contains(r.RoomId) &&
                        !r.IsDeleted)
                    .ToListAsync();

                // Toplam iade tutarını takip eder.
                decimal totalRefundAmount = 0;

                // İade yapılan rezervasyon sayısını takip eder.
                int refundCount = 0;

                // --------------------------------------------------
                // 8. REZERVASYONLARI SOFT DELETE
                // VE MÜŞTERİYE TAM İADE YAP
                // --------------------------------------------------
                foreach (var reservation in reservations)
                {
                    // Otel/Admin tarafından iptal edildiği için
                    // müşteriye rezervasyonun tamamı iade edilir.
                    decimal refundAmount = reservation.TotalPrice;

                    // Otel tarafından iptal edildiği için
                    // herhangi bir ceza uygulanmaz.
                    reservation.PenaltyFee = 0;

                    // Rezervasyonun durumu iptal edildi yapılır.
                    reservation.Status =
                        ReservationStatus.Cancelled;

                    // Rezervasyon soft delete yapılır.
                    reservation.IsDeleted = true;

                    // --------------------------------------------------
                    // 9. MÜŞTERİNİN BAKİYESİNE İADE EKLE
                    // --------------------------------------------------
                    if (reservation.User != null &&
                        !reservation.User.IsDeleted)
                    {
                        reservation.User.Balance += refundAmount;

                        totalRefundAmount += refundAmount;
                        refundCount++;
                    }

                    // --------------------------------------------------
                    // 10. REZERVASYONA AİT ESKİ ÖDEMELERİ BUL
                    // --------------------------------------------------
                    var reservationPayments =
                        await _context.Payments
                            .Where(p =>
                                p.ReservationId == reservation.Id &&
                                !p.IsDeleted)
                            .ToListAsync();

                    // Eski ödeme kayıtlarını fiziksel olarak silme.
                    // Sadece soft delete yap.
                    foreach (var payment in reservationPayments)
                    {
                        payment.IsDeleted = true;
                    }

                    // --------------------------------------------------
                    // 11. YENİ REFUND ÖDEME KAYDI OLUŞTUR
                    // --------------------------------------------------
                    var refundPayment = new Payment
                    {
                        UserId = reservation.UserId,
                        ReservationId = reservation.Id,
                        Amount = refundAmount,
                        TransactionType =
                            TransactionType.Refund,
                        Status =
                            PaymentStatus.Refunded,
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    _context.Payments.Add(refundPayment);
                }

                // --------------------------------------------------
                // 12. TÜM DEĞİŞİKLİKLERİ VERİTABANINA KAYDET
                // --------------------------------------------------
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Otel ve bağlı kayıtlar soft delete edildi ve iadeler yapıldı. " +
                    "HotelId: {HotelId}, UserId: {UserId}, " +
                    "RoomCount: {RoomCount}, ReservationCount: {ReservationCount}, " +
                    "RefundCount: {RefundCount}, TotalRefundAmount: {TotalRefundAmount}",
                    id,
                    userId,
                    rooms.Count,
                    reservations.Count,
                    refundCount,
                    totalRefundAmount);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Otel soft delete ve iade işlemi sırasında veritabanı hatası oluştu. " +
                    "HotelId: {HotelId}, UserId: {UserId}",
                    id,
                    userId);

                return BadRequest(new
                {
                    message =
                        "Otel silinirken ve para iadeleri yapılırken veritabanı hatası oluştu."
                });
            }

            // --------------------------------------------------
            // 13. BAŞARILI SONUÇ
            // --------------------------------------------------
            return Ok(new
            {
                message =
                    "Otel ve ona bağlı oda, rezervasyon ve ödeme kayıtları başarıyla silindi. " +
                    "Aktif rezervasyonların ücretleri müşterilere tam olarak iade edildi."
            });
        }
    }
}
