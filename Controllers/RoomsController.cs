
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelManagementApi.Data;
using HotelManagementApi.Models;
using HotelManagementApi.DTOs;

namespace HotelManagementApi.Controllers
{
    // Oda görüntüleme ve oda yönetimi işlemlerini gerçekleştirir.
    [Route("api/[controller]")]
    [ApiController]
    public class RoomsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RoomsController> _logger;

        public RoomsController(
            AppDbContext context,
            ILogger<RoomsController> logger)
        {
            _context = context;
            _logger = logger;
        }


        // GET: api/rooms
        // Herkes aktif odaları görebilir.
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Room>>> GetRooms()
        {
            _logger.LogInformation(
                "Tüm aktif odaları listeleme işlemi başlatıldı.");

            var rooms = await _context.Rooms
                .Include(r => r.Hotel)
                .Where(r =>
                    !r.IsDeleted &&
                    r.Hotel != null &&
                    !r.Hotel.IsDeleted)
                .ToListAsync();

            _logger.LogInformation(
                "Aktif odalar başarıyla listelendi. Oda sayısı: {RoomCount}",
                rooms.Count);

            return Ok(rooms);
        }


        // GET: api/rooms/{id}
        // Herkes belirli aktif odayı görebilir.
        [HttpGet("{id}")]
        public async Task<ActionResult<Room>> GetRoom(Guid id)
        {
            _logger.LogInformation(
                "Oda bilgisi getirme işlemi başlatıldı. RoomId: {RoomId}",
                id);

            var room = await _context.Rooms
                .Include(r => r.Hotel)
                .FirstOrDefaultAsync(r =>
                    r.Id == id &&
                    !r.IsDeleted &&
                    r.Hotel != null &&
                    !r.Hotel.IsDeleted);

            if (room == null)
            {
                _logger.LogWarning(
                    "Oda bulunamadı veya silinmiş. RoomId: {RoomId}",
                    id);

                return NotFound(new
                {
                    message = "Oda bulunamadı."
                });
            }

            _logger.LogInformation(
                "Oda başarıyla getirildi. RoomId: {RoomId}, RoomNumber: {RoomNumber}",
                room.Id,
                room.RoomNumber);

            return Ok(room);
        }


        // GET: api/rooms/hotel/{hotelId}
        // Herkes aktif otelin aktif odalarını getirir.
        [HttpGet("hotel/{hotelId}")]
        public async Task<ActionResult<IEnumerable<Room>>> GetRoomsByHotel(
            Guid hotelId)
        {
            _logger.LogInformation(
                "Otele ait aktif odalar getiriliyor. HotelId: {HotelId}",
                hotelId);

            var hotelExists = await _context.Hotels
                .AnyAsync(h =>
                    h.Id == hotelId &&
                    !h.IsDeleted);

            if (!hotelExists)
            {
                _logger.LogWarning(
                    "Otel bulunamadı veya silinmiş. Oda listeleme başarısız. HotelId: {HotelId}",
                    hotelId);

                return NotFound(new
                {
                    message = "Otel bulunamadı."
                });
            }

            var rooms = await _context.Rooms
                .Include(r => r.Hotel)
                .Where(r =>
                    r.HotelId == hotelId &&
                    !r.IsDeleted &&
                    r.Hotel != null &&
                    !r.Hotel.IsDeleted)
                .ToListAsync();

            _logger.LogInformation(
                "Otele ait aktif odalar başarıyla listelendi. HotelId: {HotelId}, Oda sayısı: {RoomCount}",
                hotelId,
                rooms.Count);

            return Ok(rooms);
        }


        // GET: api/rooms/available
        // Herkes aktif ve müsait odaları görebilir.
        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<Room>>> GetAvailableRooms(
            [FromQuery] DateTime? checkIn,
            [FromQuery] DateTime? checkOut)
        {
            _logger.LogInformation(
                "Müsait oda sorgulama işlemi başlatıldı. CheckIn: {CheckIn}, CheckOut: {CheckOut}",
                checkIn,
                checkOut);

            if (checkIn.HasValue != checkOut.HasValue)
            {
                _logger.LogWarning(
                    "Müsait oda sorgusu başarısız. Check-in ve check-out bilgilerinden yalnızca biri gönderildi.");

                return BadRequest(new
                {
                    message =
                        "Check-in ve check-out tarihleri birlikte gönderilmelidir."
                });
            }

            if (!checkIn.HasValue && !checkOut.HasValue)
            {
                var availableRooms = await _context.Rooms
                    .Include(r => r.Hotel)
                    .Where(r =>
                        !r.IsDeleted &&
                        r.IsAvailable &&
                        r.Hotel != null &&
                        !r.Hotel.IsDeleted)
                    .ToListAsync();

                _logger.LogInformation(
                    "Genel müsait oda listesi başarıyla getirildi. Müsait oda sayısı: {RoomCount}",
                    availableRooms.Count);

                return Ok(availableRooms);
            }

            DateTime checkInDate =
                checkIn.GetValueOrDefault();

            DateTime checkOutDate =
                checkOut.GetValueOrDefault();

            if (checkOutDate <= checkInDate)
            {
                _logger.LogWarning(
                    "Müsait oda sorgusu başarısız. Check-out tarihi check-in tarihinden önce veya eşit.");

                return BadRequest(new
                {
                    message =
                        "Check-out tarihi check-in tarihinden sonra olmalıdır."
                });
            }

            if (checkInDate.Date < DateTime.UtcNow.Date)
            {
                _logger.LogWarning(
                    "Müsait oda sorgusu başarısız. Geçmiş tarih için sorgulama yapıldı. CheckIn: {CheckIn}",
                    checkInDate);

                return BadRequest(new
                {
                    message =
                        "Geçmiş bir tarih için müsaitlik kontrolü yapılamaz."
                });
            }

            var availableRoomsByDate =
                await _context.Rooms
                    .Include(r => r.Hotel)
                    .Where(room =>
                        !room.IsDeleted &&
                        room.IsAvailable &&
                        room.Hotel != null &&
                        !room.Hotel.IsDeleted &&
                        !room.Reservations.Any(reservation =>
                            !reservation.IsDeleted &&
                            reservation.Status ==
                                ReservationStatus.Confirmed &&
                            checkInDate <
                                reservation.CheckOutDate &&
                            checkOutDate >
                                reservation.CheckInDate
                        )
                    )
                    .ToListAsync();

            _logger.LogInformation(
                "Tarih aralığına göre müsait odalar listelendi. CheckIn: {CheckIn}, CheckOut: {CheckOut}, Oda sayısı: {RoomCount}",
                checkInDate,
                checkOutDate,
                availableRoomsByDate.Count);

            return Ok(availableRoomsByDate);
        }


        // POST: api/rooms
        // Sadece Admin ve HotelOwner oda oluşturabilir.
        [HttpPost]
        [Authorize(Roles = "Admin,HotelOwner")]
        public async Task<ActionResult<Room>> PostRoom(
            CreateRoomDto room)
        {
            _logger.LogInformation(
                "Yeni oda oluşturma işlemi başlatıldı. HotelId: {HotelId}, RoomNumber: {RoomNumber}",
                room.HotelId,
                room.RoomNumber);

            var hotel = await _context.Hotels
                .FirstOrDefaultAsync(h =>
                    h.Id == room.HotelId &&
                    !h.IsDeleted);

            if (hotel == null)
            {
                _logger.LogWarning(
                    "Oda oluşturma başarısız. Belirtilen otel bulunamadı veya silinmiş. HotelId: {HotelId}",
                    room.HotelId);

                return BadRequest(new
                {
                    message = "Belirtilen otel bulunamadı."
                });
            }

            // HotelOwner sadece kendi oteline oda ekleyebilir
            if (!User.IsInRole("Admin"))
            {
                var userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (userIdClaim == null ||
                    !Guid.TryParse(userIdClaim, out Guid userId))
                {
                    _logger.LogWarning(
                        "Oda oluşturma başarısız. Geçersiz token. HotelId: {HotelId}",
                        room.HotelId);

                    return Unauthorized(new
                    {
                        message = "Geçersiz token."
                    });
                }

                if (hotel.OwnerId != userId)
                {
                    _logger.LogWarning(
                        "Yetkisiz oda oluşturma denemesi. HotelId: {HotelId}, UserId: {UserId}, OwnerId: {OwnerId}",
                        room.HotelId,
                        userId,
                        hotel.OwnerId);

                    return Forbid();
                }
            }

            var newRoom = new Room
            {
                HotelId = room.HotelId,
                RoomNumber = room.RoomNumber,
                RoomType = room.RoomType,
                Capacity = room.Capacity,
                PricePerNight = room.PricePerNight,
                IsAvailable = room.IsAvailable,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _context.Rooms.Add(newRoom);

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Oda başarıyla oluşturuldu. RoomId: {RoomId}, HotelId: {HotelId}, RoomNumber: {RoomNumber}",
                    newRoom.Id,
                    newRoom.HotelId,
                    newRoom.RoomNumber);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Oda oluşturulurken veritabanı hatası oluştu. HotelId: {HotelId}, RoomNumber: {RoomNumber}",
                    room.HotelId,
                    room.RoomNumber);

                return BadRequest(new
                {
                    message =
                        "Oda oluşturulurken veritabanı hatası oluştu."
                });
            }

            return CreatedAtAction(
                nameof(GetRoom),
                new { id = newRoom.Id },
                newRoom
            );
        }


        // PUT: api/rooms/{id}
        // Admin her aktif odayı,
        // HotelOwner sadece kendi otelindeki aktif odayı değiştirebilir.
                
            [HttpDelete("{id}")]
            [Authorize(Roles = "Admin,HotelOwner")]
            public async Task<IActionResult> DeleteRoom(Guid id)
            {
                _logger.LogInformation(
                    "Oda soft delete ve rezervasyon iade işlemi başlatıldı. RoomId: {RoomId}",
                    id);

                // 1. Silinecek aktif odayı ve bağlı oteli bul.
                var room = await _context.Rooms
                    .Include(r => r.Hotel)
                    .FirstOrDefaultAsync(r =>
                        r.Id == id &&
                        !r.IsDeleted &&
                        r.Hotel != null &&
                        !r.Hotel.IsDeleted);

                if (room == null)
                {
                    _logger.LogWarning(
                        "Silinecek oda bulunamadı veya zaten silinmiş. RoomId: {RoomId}",
                        id);

                    return NotFound(new
                    {
                        message = "Oda bulunamadı."
                    });
                }

                // 2. HotelOwner sadece kendi otelindeki odayı silebilir.
                if (!User.IsInRole("Admin"))
                {
                    var userIdClaim =
                        User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    if (userIdClaim == null ||
                        !Guid.TryParse(userIdClaim, out Guid userId))
                    {
                        _logger.LogWarning(
                            "Oda silme başarısız. Geçersiz token. RoomId: {RoomId}",
                            id);

                        return Unauthorized(new
                        {
                            message = "Geçersiz token."
                        });
                    }

                    if (room.Hotel?.OwnerId != userId)
                    {
                        _logger.LogWarning(
                            "Yetkisiz oda silme denemesi. RoomId: {RoomId}, UserId: {UserId}, OwnerId: {OwnerId}",
                            id,
                            userId,
                            room.Hotel?.OwnerId);

                        return Forbid();
                    }
                }

                // 3. Odaya bağlı aktif rezervasyonları,
                // rezervasyon sahiplerinin kullanıcı bilgileriyle birlikte getir.
                var reservations = await _context.Reservations
                    .Include(r => r.User)
                    .Where(r =>
                        r.RoomId == room.Id &&
                        !r.IsDeleted)
                    .ToListAsync();

                // İade edilen rezervasyon sayısını takip etmek için.
                int refundedReservationCount = 0;

                // Toplam iade miktarını takip etmek için.
                decimal totalRefundAmount = 0;

                // 4. Her aktif rezervasyonu iptal et ve TAM iade yap.
                foreach (var reservation in reservations)
                {
                    // Oda otel tarafından silindiği için
                    // müşteriye herhangi bir iptal cezası uygulanmaz.
                    var refundAmount = reservation.TotalPrice;

                    // Ceza sıfırlanır.
                    reservation.PenaltyFee = 0;

                    // Rezervasyon iptal durumuna getirilir.
                    reservation.Status = ReservationStatus.Cancelled;

                    // Soft delete.
                    reservation.IsDeleted = true;

                    // 5. Rezervasyon sahibinin bakiyesine
                    // rezervasyonun tamamını iade et.
                    if (reservation.User != null &&
                        !reservation.User.IsDeleted)
                    {
                        reservation.User.Balance += refundAmount;

                        totalRefundAmount += refundAmount;
                        refundedReservationCount++;

                        _logger.LogInformation(
                            "Oda silme nedeniyle müşteriye tam iade yapıldı. " +
                            "ReservationId: {ReservationId}, UserId: {UserId}, RefundAmount: {RefundAmount}",
                            reservation.Id,
                            reservation.UserId,
                            refundAmount);
                    }

                    // 6. Rezervasyonun mevcut aktif ödeme kayıtlarını bul.
                    var existingPayments = await _context.Payments
                        .Where(p =>
                            p.ReservationId == reservation.Id &&
                            !p.IsDeleted)
                        .ToListAsync();

                    // Eski ödeme kayıtlarını soft delete yap.
                    foreach (var payment in existingPayments)
                    {
                        payment.IsDeleted = true;
                    }

                    // 7. İade işlemi için yeni aktif Payment kaydı oluştur.
                    var refundPayment = new Payment
                    {
                        UserId = reservation.UserId,
                        ReservationId = reservation.Id,
                        Amount = refundAmount,
                        TransactionType = TransactionType.Refund,
                        Status = PaymentStatus.Refunded,
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    _context.Payments.Add(refundPayment);
                }

                // 8. Odayı soft delete yap.
                room.IsDeleted = true;

                // 9. Tüm değişiklikleri tek seferde kaydet.
                try
                {
                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Oda soft delete ve iade işlemi başarıyla tamamlandı. " +
                        "RoomId: {RoomId}, HotelId: {HotelId}, ReservationCount: {ReservationCount}, " +
                        "RefundedReservationCount: {RefundedReservationCount}, TotalRefundAmount: {TotalRefundAmount}",
                        room.Id,
                        room.HotelId,
                        reservations.Count,
                        refundedReservationCount,
                        totalRefundAmount);
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(
                        ex,
                        "Oda silinirken veya rezervasyon iadeleri yapılırken veritabanı hatası oluştu. " +
                        "RoomId: {RoomId}, HotelId: {HotelId}",
                        room.Id,
                        room.HotelId);

                    return BadRequest(new
                    {
                        message =
                            "Oda silinirken veya rezervasyon iadeleri yapılırken veritabanı hatası oluştu."
                    });
                }

                // 10. İşlem sonucunu döndür.
                return Ok(new
                {
                    message =
                        "Oda başarıyla silindi. Odaya ait rezervasyonlar iptal edildi ve müşterilere tam ödeme iadesi yapıldı.",

                    roomId = room.Id,

                    isDeleted = room.IsDeleted,

                    cancelledReservationCount = reservations.Count,

                    refundedReservationCount = refundedReservationCount,

                    totalRefundAmount = totalRefundAmount
                });
            }
        }
    }

