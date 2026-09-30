using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelManagementApi.Data;
using HotelManagementApi.Models;
using HotelManagementApi.DTOs;

namespace HotelManagementApi.Controllers
{
    // Rezervasyon işlemlerini yöneten controller.
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReservationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ReservationsController> _logger;

        public ReservationsController(
            AppDbContext context,
            ILogger<ReservationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        
        // GET: api/reservations
        // Kullanıcının rolüne göre rezervasyonları listeler.
        [HttpGet]
        public async Task<IActionResult> GetReservations()
        {
            _logger.LogInformation(
                "Rezervasyonları listeleme işlemi başlatıldı.");

            // JWT token içerisinden kullanıcı ID'sini al.
            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdClaim == null ||
                !int.TryParse(userIdClaim, out int userId))
            {
                _logger.LogWarning(
                    "Rezervasyon listeleme başarısız. Geçersiz token.");

                return Unauthorized(new
                {
                    message = "Geçersiz token."
                });
            }

            // Admin tüm rezervasyonları görebilir.

            if (User.IsInRole("Admin"))
            {
                var adminReservations = await _context.Reservations
                    .Include(r => r.Room)
                        .ThenInclude(room => room!.Hotel)
                    .Include(r => r.User)
                    .Select(r => new
                    {
                        r.Id,
                        r.UserId,
                        Username = r.User!.Username,
                        r.RoomId,
                        RoomNumber = r.Room!.RoomNumber,
                        HotelId = r.Room.Hotel!.Id,
                        HotelName = r.Room.Hotel.Name,
                        r.CheckInDate,
                        r.CheckOutDate,
                        r.GuestCount,
                        r.TotalPrice,
                        r.PenaltyFee,
                        r.Status,
                        r.CreatedAt
                    })
                    .ToListAsync();

                _logger.LogInformation(
                    "Admin tüm rezervasyonları listeledi. Rezervasyon sayısı: {ReservationCount}",
                    adminReservations.Count);

                return Ok(adminReservations);
            }

            // HotelOwner sadece kendi otellerindeki rezervasyonları görebilir.
            if (User.IsInRole("HotelOwner"))
            {
                var ownerReservations = await _context.Reservations
                    .Include(r => r.Room)
                        .ThenInclude(room => room!.Hotel)
                    .Include(r => r.User)
                    .Where(r =>
                        r.Room != null &&
                        r.Room.Hotel != null &&
                        r.Room.Hotel.OwnerId == userId)
                    .Select(r => new
                    {
                        r.Id,
                        r.UserId,
                        Username = r.User!.Username,
                        r.RoomId,
                        RoomNumber = r.Room!.RoomNumber,
                        HotelId = r.Room.Hotel!.Id,
                        HotelName = r.Room.Hotel.Name,
                        r.CheckInDate,
                        r.CheckOutDate,
                        r.GuestCount,
                        r.TotalPrice,
                        r.PenaltyFee,
                        r.Status,
                        r.CreatedAt
                    })
                    .ToListAsync();

                _logger.LogInformation(
                    "HotelOwner kendi otellerindeki rezervasyonları listeledi. UserId: {UserId}, Rezervasyon sayısı: {ReservationCount}",
                    userId,
                    ownerReservations.Count);

                return Ok(ownerReservations);
            }

            // Customer sadece kendi rezervasyonlarını görebilir.
            var customerReservations = await _context.Reservations
                .Include(r => r.Room)
                    .ThenInclude(room => room!.Hotel)
                .Include(r => r.User)
                .Where(r => r.UserId == userId)
                .Select(r => new
                {
                    r.Id,
                    r.UserId,
                    Username = r.User!.Username,
                    r.RoomId,
                    RoomNumber = r.Room!.RoomNumber,
                    HotelId = r.Room.Hotel!.Id,
                    HotelName = r.Room.Hotel.Name,
                    r.CheckInDate,
                    r.CheckOutDate,
                    r.GuestCount,
                    r.TotalPrice,
                    r.PenaltyFee,
                    r.Status,
                    r.CreatedAt
                })
                .ToListAsync();

            _logger.LogInformation(
                "Customer kendi rezervasyonlarını listeledi. UserId: {UserId}, Rezervasyon sayısı: {ReservationCount}",
                userId,
                customerReservations.Count);

            return Ok(customerReservations);
        }

        // GET: api/reservations/5
        // Belirli bir rezervasyonun detayını getirir.
        [HttpGet("{id}")]
        public async Task<IActionResult> GetReservation(int id)
        {
            _logger.LogInformation(
                "Rezervasyon bilgisi getirme işlemi başlatıldı. ReservationId: {ReservationId}",
                id);
            
            // Rezervasyonu kullanıcı ve oda bilgileriyle birlikte getir.
            var reservation = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Room)
                    .ThenInclude(room => room!.Hotel)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
            {
                _logger.LogWarning(
                    "Rezervasyon bulunamadı. ReservationId: {ReservationId}",
                    id);

                return NotFound(new
                {
                    message = "Rezervasyon bulunamadı."
                });
            }

            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdClaim == null ||
                !int.TryParse(userIdClaim, out int userId))
            {
                _logger.LogWarning(
                    "Rezervasyon görüntüleme başarısız. Geçersiz token. ReservationId: {ReservationId}",
                    id);

                return Unauthorized(new
                {
                    message = "Geçersiz token."
                });
            }

            
            if (User.IsInRole("Admin"))
            {
                // Admin bütün rezervasyonları görebilir.
            }
            // HotelOwner sadece kendi otellerinin rezervasyonlarını görebilir.
            else if (User.IsInRole("HotelOwner"))
            {
                if (reservation.Room?.Hotel?.OwnerId != userId)
                {
                    _logger.LogWarning(
                        "HotelOwner yetkisiz rezervasyon görüntüleme denemesi. ReservationId: {ReservationId}, UserId: {UserId}",
                        id,
                        userId);

                    return Forbid();
                }
            }
            // Customer sadece kendi rezervasyonunu görebilir.
            else
            {
                if (reservation.UserId != userId)
                {
                    _logger.LogWarning(
                        "Customer başka kullanıcıya ait rezervasyonu görüntülemeye çalıştı. ReservationId: {ReservationId}, UserId: {UserId}",
                        id,
                        userId);

                    return Forbid();
                }
            }

            _logger.LogInformation(
                "Rezervasyon başarıyla getirildi. ReservationId: {ReservationId}, UserId: {UserId}",
                id,
                userId);

            // Gereksiz navigation alanlarını response'a dahil etme.
            return Ok(new
            {
                reservation.Id,
                reservation.UserId,
                Username = reservation.User?.Username,
                reservation.RoomId,
                RoomNumber = reservation.Room?.RoomNumber,
                HotelId = reservation.Room?.Hotel?.Id,
                HotelName = reservation.Room?.Hotel?.Name,
                reservation.CheckInDate,
                reservation.CheckOutDate,
                reservation.GuestCount,
                reservation.TotalPrice,
                reservation.PenaltyFee,
                reservation.Status,
                reservation.CreatedAt
            });
        }

        // POST: api/reservations
        // Customer tarafından yeni rezervasyon oluşturur.
        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> PostReservation(
            [FromBody] CreateReservationDto dto)
        {
            _logger.LogInformation(
                "Yeni rezervasyon oluşturma işlemi başlatıldı. RoomId: {RoomId}",
                dto.RoomId);

             // 1. Kullanıcı ID'si token'dan alınır.
            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdClaim == null ||
                !int.TryParse(userIdClaim, out int userId))
            {
                _logger.LogWarning(
                    "Rezervasyon oluşturma başarısız. Geçersiz token.");

                return Unauthorized(new
                {
                    message = "Geçersiz token."
                });
            }

             // 2. Rezervasyonu oluşturan kullanıcı bulunur.
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                _logger.LogWarning(
                    "Rezervasyon oluşturma başarısız. Kullanıcı bulunamadı. UserId: {UserId}",
                    userId);

                return BadRequest(new
                {
                    message = "Kullanıcı bulunamadı."
                });
            }

            // 3. Rezervasyon yapılacak oda bulunur.
            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.Id == dto.RoomId);

            if (room == null)
            {
                _logger.LogWarning(
                    "Rezervasyon oluşturma başarısız. Oda bulunamadı. RoomId: {RoomId}",
                    dto.RoomId);

                return BadRequest(new
                {
                    message = "Belirtilen oda bulunamadı."
                });
            }

            // 4. Misafir sayısı kontrol edilir.
            if (dto.GuestCount <= 0)
            {
                _logger.LogWarning(
                    "Rezervasyon oluşturma başarısız. Geçersiz misafir sayısı: {GuestCount}",
                    dto.GuestCount);

                return BadRequest(new
                {
                    message = "Misafir sayısı en az 1 olmalıdır."
                });
            }

            // Misafir sayısı oda kapasitesini aşamaz.
            if (dto.GuestCount > room.Capacity)
            {
                _logger.LogWarning(
                    "Rezervasyon oluşturma başarısız. Oda kapasitesi yetersiz. RoomId: {RoomId}, GuestCount: {GuestCount}, Capacity: {Capacity}",
                    dto.RoomId,
                    dto.GuestCount,
                    room.Capacity);

                return BadRequest(new
                {
                    message =
                        $"Seçilen odanın kapasitesi yetersiz! " +
                        $"Bu oda maksimum {room.Capacity} kişiliktir."
                });
            }

             // 5. Tarihlerin geçerli olup olmadığı kontrol edilir..
            if (dto.CheckOutDate <= dto.CheckInDate)
            {
                _logger.LogWarning(
                    "Rezervasyon oluşturma başarısız. Geçersiz tarih aralığı. RoomId: {RoomId}",
                    dto.RoomId);

                return BadRequest(new
                {
                    message =
                        "Çıkış tarihi giriş tarihinden sonra olmalıdır."
                });
            }

             // Geçmiş bir tarihe rezervasyon yapılamaz.
            if (dto.CheckInDate.Date < DateTime.UtcNow.Date)
            {
                _logger.LogWarning(
                    "Rezervasyon oluşturma başarısız. Geçmiş tarih kullanıldı. CheckIn: {CheckIn}",
                    dto.CheckInDate);

                return BadRequest(new
                {
                    message =
                        "Geçmiş bir tarihe rezervasyon yapılamaz."
                });
            }

            // 6. Aynı odada tarih çakışması var mı kontrol et.
            var isConflict = await _context.Reservations.AnyAsync(r =>
                r.RoomId == dto.RoomId &&
                r.Status == ReservationStatus.Confirmed &&
                dto.CheckInDate < r.CheckOutDate &&
                dto.CheckOutDate > r.CheckInDate
            );

            if (isConflict)
            {
                _logger.LogWarning(
                    "Rezervasyon oluşturma başarısız. Tarih çakışması mevcut. RoomId: {RoomId}",
                    dto.RoomId);

                return BadRequest(new
                {
                    message =
                        "Seçilen tarihler arasında bu oda zaten rezerve edilmiş."
                });
            }

            // 7. Konaklama gün sayısını hesapla.
            var totalDays =
                (dto.CheckOutDate - dto.CheckInDate).Days;

            if (totalDays <= 0)
            {
                _logger.LogWarning(
                    "Rezervasyon oluşturma başarısız. Geçersiz konaklama süresi.");

                return BadRequest(new
                {
                    message =
                        "Geçerli bir konaklama süresi belirtilmelidir."
                });
            }

            // 8. Toplam fiyatı hesapla.
            var totalPrice =
                room.PricePerNight *
                dto.GuestCount *
                totalDays;

             // 9. Kullanıcının yeterli bakiyesi var mı kontrol et.
            if (user.Balance < totalPrice)
            {
                _logger.LogWarning(
                    "Rezervasyon oluşturma başarısız. Yetersiz bakiye. UserId: {UserId}, Balance: {Balance}, TotalPrice: {TotalPrice}",
                    userId,
                    user.Balance,
                    totalPrice);

                return BadRequest(new
                {
                    message =
                        $"Yetersiz bakiye! " +
                        $"Mevcut bakiyeniz: {user.Balance} TL, " +
                        $"ödemeniz gereken tutar: {totalPrice} TL."
                });
            }

             // 10. Yeni rezervasyon nesnesini oluştur.
            var reservation = new Reservation
            {
                UserId = userId,
                RoomId = dto.RoomId,
                CheckInDate = dto.CheckInDate,
                CheckOutDate = dto.CheckOutDate,
                GuestCount = dto.GuestCount,
                TotalPrice = totalPrice,
                PenaltyFee = 0,
                Status = ReservationStatus.Confirmed,
                CreatedAt = DateTime.UtcNow
            };

             // 11. Rezervasyon ücretini kullanıcı bakiyesinden düş.
            user.Balance -= totalPrice;

            _context.Reservations.Add(reservation);

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Rezervasyon başarıyla oluşturuldu. ReservationId: {ReservationId}, UserId: {UserId}, RoomId: {RoomId}, TotalPrice: {TotalPrice}",
                    reservation.Id,
                    userId,
                    dto.RoomId,
                    totalPrice);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Rezervasyon oluşturulurken veritabanı hatası oluştu. UserId: {UserId}, RoomId: {RoomId}",
                    userId,
                    dto.RoomId);

                return BadRequest(new
                {
                    message =
                        "Rezervasyon oluşturulurken veritabanı hatası oluştu."
                });
            }

            // 12. Ödeme kaydını oluştur.
            var payment = new Payment
            {
                UserId = userId,
                ReservationId = reservation.Id,
                Amount = totalPrice,
                TransactionType = TransactionType.Payment,
                Status = PaymentStatus.Success,
                CreatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Ödeme kaydı başarıyla oluşturuldu. PaymentId: {PaymentId}, ReservationId: {ReservationId}, Amount: {Amount}",
                    payment.Id,
                    reservation.Id,
                    totalPrice);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Ödeme kaydı oluşturulurken veritabanı hatası oluştu. ReservationId: {ReservationId}",
                    reservation.Id);

                return BadRequest(new
                {
                    message =
                        "Ödeme kaydı oluşturulurken veritabanı hatası oluştu."
                });
            }

            // 13. Oluşturulan rezervasyonu response olarak döndür.
            return CreatedAtAction(
                nameof(GetReservation),
                new { id = reservation.Id },
                new
                {
                    reservation.Id,
                    reservation.UserId,
                    reservation.RoomId,
                    reservation.CheckInDate,
                    reservation.CheckOutDate,
                    reservation.GuestCount,
                    reservation.TotalPrice,
                    reservation.PenaltyFee,
                    reservation.Status,
                    reservation.CreatedAt
                });
        }

         // PUT: api/reservations/5
        // Customer kendi rezervasyonunu güncelleyebilir.
        [HttpPut("{id}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> PutReservation(
            int id,
            [FromBody] UpdateReservationDto reservation)
        {
            _logger.LogInformation(
                "Rezervasyon güncelleme işlemi başlatıldı. ReservationId: {ReservationId}",
                id);

            // 1. URL'deki ID ile body'deki ID aynı olmalı.
            if (id != reservation.Id)
            {
                _logger.LogWarning(
                    "Rezervasyon güncelleme başarısız. ID uyuşmazlığı. URL Id: {UrlId}, Body Id: {BodyId}",
                    id,
                    reservation.Id);

                return BadRequest(new
                {
                    message = "ID uyuşmazlığı."
                });
            }

            
            // 2. Güncellenecek mevcut rezervasyonu bul.
            var existingReservation =
                await _context.Reservations
                    .FirstOrDefaultAsync(r => r.Id == id);

            if (existingReservation == null)
            {
                _logger.LogWarning(
                    "Güncellenecek rezervasyon bulunamadı. ReservationId: {ReservationId}",
                    id);

                return NotFound(new
                {
                    message = "Rezervasyon bulunamadı."
                });
            }

             // 3. Kullanıcı ID'sini token'dan al.

            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdClaim == null ||
                !int.TryParse(userIdClaim, out int userId))
            {
                _logger.LogWarning(
                    "Rezervasyon güncelleme başarısız. Geçersiz token. ReservationId: {ReservationId}",
                    id);

                return Unauthorized(new
                {
                    message = "Geçersiz token."
                });
            }

            // 4. Sadece rezervasyon sahibi kendi rezervasyonunu güncelleyebilir.
            if (existingReservation.UserId != userId)
            {
                _logger.LogWarning(
                    "Yetkisiz rezervasyon güncelleme denemesi. ReservationId: {ReservationId}, UserId: {UserId}, OwnerId: {OwnerId}",
                    id,
                    userId,
                    existingReservation.UserId);

                return Forbid();
            }

             // 5. İptal edilmiş rezervasyon güncellenemez.

            if (existingReservation.Status ==
                ReservationStatus.Cancelled)
            {
                _logger.LogWarning(
                    "İptal edilmiş rezervasyon güncellenmeye çalışıldı. ReservationId: {ReservationId}",
                    id);

                return BadRequest(new
                {
                    message =
                        "İptal edilmiş bir rezervasyon güncellenemez."
                });
            }

            // 6. Tarih kontrolleri.
            if (reservation.CheckOutDate <=
                reservation.CheckInDate)
            {
                _logger.LogWarning(
                    "Rezervasyon güncelleme başarısız. Geçersiz tarih aralığı. ReservationId: {ReservationId}",
                    id);

                return BadRequest(new
                {
                    message =
                        "Çıkış tarihi giriş tarihinden ileri olmalıdır."
                });
            }

            if (reservation.CheckInDate.Date <
                DateTime.UtcNow.Date)
            {
                _logger.LogWarning(
                    "Rezervasyon güncelleme başarısız. Geçmiş tarih kullanıldı. ReservationId: {ReservationId}",
                    id);

                return BadRequest(new
                {
                    message =
                        "Geçmiş bir tarihe rezervasyon yapılamaz."
                });
            }

            // 7. Yeni oda mevcut mu kontrol et.

            var room =
                await _context.Rooms
                    .FirstOrDefaultAsync(r =>
                        r.Id == reservation.RoomId);

            if (room == null)
            {
                _logger.LogWarning(
                    "Rezervasyon güncelleme başarısız. Oda bulunamadı. RoomId: {RoomId}",
                    reservation.RoomId);

                return BadRequest(new
                {
                    message = "Belirtilen oda bulunamadı."
                });
            }

             // 8. Misafir sayısını kontrol et.
            if (reservation.GuestCount <= 0)
            {
                _logger.LogWarning(
                    "Rezervasyon güncelleme başarısız. Geçersiz misafir sayısı: {GuestCount}",
                    reservation.GuestCount);

                return BadRequest(new
                {
                    message =
                        "Misafir sayısı en az 1 olmalıdır."
                });
            }

            if (reservation.GuestCount > room.Capacity)
            {
                _logger.LogWarning(
                    "Rezervasyon güncelleme başarısız. Oda kapasitesi yetersiz. RoomId: {RoomId}, GuestCount: {GuestCount}, Capacity: {Capacity}",
                    reservation.RoomId,
                    reservation.GuestCount,
                    room.Capacity);

                return BadRequest(new
                {
                    message =
                        $"Seçilen odanın kapasitesi yetersiz! " +
                        $"Bu oda maksimum {room.Capacity} kişiliktir."
                });
            }

            // 9. Yeni tarihlerin başka rezervasyonlarla çakışıp çakışmadığını kontrol et.

            var isConflict = await _context.Reservations.AnyAsync(r =>
                r.Id != id &&
                r.RoomId == reservation.RoomId &&
                r.Status == ReservationStatus.Confirmed &&
                reservation.CheckInDate < r.CheckOutDate &&
                reservation.CheckOutDate > r.CheckInDate
            );

            if (isConflict)
            {
                _logger.LogWarning(
                    "Rezervasyon güncelleme başarısız. Tarih çakışması mevcut. ReservationId: {ReservationId}, RoomId: {RoomId}",
                    id,
                    reservation.RoomId);

                return BadRequest(new
                {
                    message =
                        "Seçilen tarihler arasında bu oda zaten rezerve edilmiş."
                });
            }

            // 10. Yeni konaklama süresini hesapla.

            var totalDays =
                (reservation.CheckOutDate -
                 reservation.CheckInDate).Days;

            if (totalDays <= 0)
            {
                _logger.LogWarning(
                    "Rezervasyon güncelleme başarısız. Geçersiz konaklama süresi. ReservationId: {ReservationId}",
                    id);

                return BadRequest(new
                {
                    message =
                        "Geçerli bir konaklama süresi belirtilmelidir."
                });
            }

             // 11. Yeni toplam fiyatı hesapla.
            var newTotalPrice =
                room.PricePerNight *
                reservation.GuestCount *
                totalDays;

            // 12. Eski fiyat ile yeni fiyat arasındaki farkı hesapla.
            var priceDifference =
                newTotalPrice -
                existingReservation.TotalPrice;

             // 13. Rezervasyon sahibini bul.
            var user =
                await _context.Users
                    .FindAsync(existingReservation.UserId);

            if (user == null)
            {
                _logger.LogError(
                    "Rezervasyon sahibine ait kullanıcı bulunamadı. ReservationId: {ReservationId}, UserId: {UserId}",
                    id,
                    existingReservation.UserId);

                return BadRequest(new
                {
                    message =
                        "Rezervasyon sahibine ait kullanıcı bulunamadı."
                });
            }

            // 14. Yeni fiyat arttıysa farkı bakiyeden düş.

            if (priceDifference > 0)
            {
                if (user.Balance < priceDifference)
                {
                    _logger.LogWarning(
                        "Rezervasyon güncelleme başarısız. Ek ödeme için yetersiz bakiye. ReservationId: {ReservationId}, UserId: {UserId}, PriceDifference: {PriceDifference}",
                        id,
                        userId,
                        priceDifference);

                    return BadRequest(new
                    {
                        message =
                            $"Rezervasyon güncellemesi için yeterli bakiye yok. " +
                            $"Ödenmesi gereken ek tutar: {priceDifference} TL."
                    });
                }

                user.Balance -= priceDifference;
            }

             // 15. Yeni fiyat düştüyse farkı kullanıcıya iade et.

            else if (priceDifference < 0)
            {
                var refundAmount =
                    Math.Abs(priceDifference);

                user.Balance += refundAmount;

                // Fiyat farkı için refund ödeme kaydı oluştur.
                var refundPayment = new Payment
                {
                    UserId = existingReservation.UserId,
                    ReservationId = existingReservation.Id,
                    Amount = refundAmount,
                    TransactionType = TransactionType.Refund,
                    Status = PaymentStatus.Refunded,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Payments.Add(refundPayment);

                _logger.LogInformation(
                    "Rezervasyon fiyat farkı iadesi oluşturuldu. ReservationId: {ReservationId}, RefundAmount: {RefundAmount}",
                    id,
                    refundAmount);
            }

             // 16. Rezervasyon bilgilerini güncelle.

            existingReservation.RoomId =
                reservation.RoomId;

            existingReservation.CheckInDate =
                reservation.CheckInDate;

            existingReservation.CheckOutDate =
                reservation.CheckOutDate;

            existingReservation.GuestCount =
                reservation.GuestCount;

            existingReservation.TotalPrice =
                newTotalPrice;

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Rezervasyon başarıyla güncellendi. ReservationId: {ReservationId}, UserId: {UserId}, NewTotalPrice: {NewTotalPrice}",
                    id,
                    userId,
                    newTotalPrice);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Rezervasyon güncellenirken veritabanı hatası oluştu. ReservationId: {ReservationId}",
                    id);

                return BadRequest(new
                {
                    message =
                        "Rezervasyon güncellenirken veritabanı hatası oluştu."
                });
            }

            return Ok(new
            {
                message =
                    "Rezervasyon başarıyla güncellendi.",

                reservationId =
                    existingReservation.Id,

                yeniMisafirSayisi =
                    existingReservation.GuestCount,

                yeniToplamFiyat =
                    existingReservation.TotalPrice,

                fiyatFarki =
                    priceDifference,

                guncelBakiye =
                    user.Balance
            });
        }

       // DELETE: api/reservations/5
        // Customer kendi rezervasyonunu iptal edebilir.
        [HttpDelete("{id}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> DeleteReservation(int id)
        {
            _logger.LogInformation(
                "Rezervasyon iptal işlemi başlatıldı. ReservationId: {ReservationId}",
                id);

             // 1. İptal edilecek rezervasyonu kullanıcı bilgisiyle birlikte getir.

            var reservation =
                await _context.Reservations
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
            {
                _logger.LogWarning(
                    "İptal edilecek rezervasyon bulunamadı. ReservationId: {ReservationId}",
                    id);

                return NotFound(new
                {
                    message =
                        "İptal edilecek rezervasyon bulunamadı."
                });
            }

            // 2. Kullanıcı ID'sini token'dan al.

            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdClaim == null ||
                !int.TryParse(userIdClaim, out int userId))
            {
                _logger.LogWarning(
                    "Rezervasyon iptali başarısız. Geçersiz token. ReservationId: {ReservationId}",
                    id);

                return Unauthorized(new
                {
                    message = "Geçersiz token."
                });
            }

            // 3. Sadece rezervasyon sahibi iptal edebilir.
            if (reservation.UserId != userId)
            {
                _logger.LogWarning(
                    "Yetkisiz rezervasyon iptal denemesi. ReservationId: {ReservationId}, UserId: {UserId}, OwnerId: {OwnerId}",
                    id,
                    userId,
                    reservation.UserId);

                return Forbid();
            }

            // 4. Rezervasyon zaten iptal edilmiş mi?
            if (reservation.Status ==
                ReservationStatus.Cancelled)
            {
                _logger.LogWarning(
                    "Rezervasyon zaten iptal edilmiş. ReservationId: {ReservationId}",
                    id);

                return BadRequest(new
                {
                    message =
                        "Bu rezervasyon zaten iptal edilmiş."
                });
            }

            // 5. Check-in tarihi geçmişse iptal edilemez.
            var today =
                DateTime.UtcNow.Date;

            var checkInDate =
                reservation.CheckInDate.Date;

            if (checkInDate < today)
            {
                _logger.LogWarning(
                    "Check-in tarihi geçmiş rezervasyon iptal edilmeye çalışıldı. ReservationId: {ReservationId}, CheckIn: {CheckIn}",
                    id,
                    reservation.CheckInDate);

                return BadRequest(new
                {
                    message =
                        "Check-in tarihi geçmiş rezervasyon iptal edilemez."
                });
            }

             // 6. İptal cezasını ve iade tutarını hesapla.
            var remainingDays =
                (checkInDate - today).Days;

            decimal refundAmount =
                reservation.TotalPrice;

            decimal penaltyFee = 0;

            // Check-in'e 2 gün veya daha az kaldıysa %20 ceza uygulanır.

            if (remainingDays <= 2)
            {
                penaltyFee =
                    reservation.TotalPrice * 0.20m;

                refundAmount =
                    reservation.TotalPrice -
                    penaltyFee;
            }

            _logger.LogInformation(
                "İptal ücreti hesaplandı. ReservationId: {ReservationId}, RemainingDays: {RemainingDays}, PenaltyFee: {PenaltyFee}, RefundAmount: {RefundAmount}",
                id,
                remainingDays,
                penaltyFee,
                refundAmount);

            // 7. Rezervasyonu iptal durumuna getir.
            reservation.PenaltyFee =
                penaltyFee;

            reservation.Status =
                ReservationStatus.Cancelled;

            // 8. İade tutarını kullanıcı bakiyesine ekle.
            if (reservation.User != null)
            {
                reservation.User.Balance +=
                    refundAmount;
            }

            // 9. İade işlemi için payment kaydı oluştur.
            var refundPayment = new Payment
            {
                UserId =
                    reservation.UserId,

                ReservationId =
                    reservation.Id,

                Amount =
                    refundAmount,

                TransactionType =
                    TransactionType.Refund,

                Status =
                    PaymentStatus.Refunded,

                CreatedAt =
                    DateTime.UtcNow
            };

            _context.Payments.Add(refundPayment);

             // 10. Rezervasyon, bakiye ve iade kaydını veritabanına kaydet.
            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Rezervasyon başarıyla iptal edildi. ReservationId: {ReservationId}, UserId: {UserId}, PenaltyFee: {PenaltyFee}, RefundAmount: {RefundAmount}, RefundPaymentId: {PaymentId}",
                    reservation.Id,
                    userId,
                    penaltyFee,
                    refundAmount,
                    refundPayment.Id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Rezervasyon iptal edilirken veritabanı hatası oluştu. ReservationId: {ReservationId}",
                    id);

                return BadRequest(new
                {
                    message =
                        "Rezervasyon iptal edilirken veritabanı hatası oluştu."
                });
            }

            // 11. İptal sonucunu response olarak döndür.

            return Ok(new
            {
                message =
                    "Rezervasyon başarıyla iptal edildi ve ödeme iadesi gerçekleştirildi.",

                kesilenCeza =
                    penaltyFee,

                iadeEdilenTutar =
                    refundAmount,

                guncelBakiye =
                    reservation.User?.Balance,

                reservationStatus =
                    reservation.Status,

                refundPaymentId =
                    refundPayment.Id
            });
        }
    }
}