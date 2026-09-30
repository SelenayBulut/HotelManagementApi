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
        // Herkes odaları görebilir
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Room>>> GetRooms()
        {
            _logger.LogInformation(
                "Tüm odaları listeleme işlemi başlatıldı.");

            // Odalarla birlikte bağlı oldukları oteller de getirilir.
            var rooms = await _context.Rooms
                .Include(r => r.Hotel)
                .ToListAsync();

            _logger.LogInformation(
                "Odalar başarıyla listelendi. Oda sayısı: {RoomCount}",
                rooms.Count);

            return Ok(rooms);
        }


        // GET: api/rooms/5
        // Herkes belirli odayı görebilir
        [HttpGet("{id}")]
        public async Task<ActionResult<Room>> GetRoom(int id)
        {
            _logger.LogInformation(
                "Oda bilgisi getirme işlemi başlatıldı. RoomId: {RoomId}",
                id);

            // Oda ve bağlı olduğu otel aranır.
            var room = await _context.Rooms
                .Include(r => r.Hotel)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null)
            {
                _logger.LogWarning(
                    "Oda bulunamadı. RoomId: {RoomId}",
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


        // GET: api/rooms/hotel/5
        // Herkes otelin odalarını getirir.
        [HttpGet("hotel/{hotelId}")]
        public async Task<ActionResult<IEnumerable<Room>>> GetRoomsByHotel(
            int hotelId)
        {
            _logger.LogInformation(
                "Otele ait odalar getiriliyor. HotelId: {HotelId}",
                hotelId);

            var hotelExists = await _context.Hotels
                .AnyAsync(h => h.Id == hotelId);

            if (!hotelExists)
            {
                _logger.LogWarning(
                    "Otel bulunamadı. Oda listeleme başarısız. HotelId: {HotelId}",
                    hotelId);

                return NotFound(new
                {
                    message = "Otel bulunamadı."
                });
            }

            // Sadece belirtilen otele ait odalar getirilir.
            var rooms = await _context.Rooms
                .Include(r => r.Hotel)
                .Where(r => r.HotelId == hotelId)
                .ToListAsync();

            _logger.LogInformation(
                "Otele ait odalar başarıyla listelendi. HotelId: {HotelId}, Oda sayısı: {RoomCount}",
                hotelId,
                rooms.Count);

            return Ok(rooms);
        }


        // GET: api/rooms/available
        // Herkes müsait odaları görebilir
        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<Room>>> GetAvailableRooms(
            [FromQuery] DateTime? checkIn,
            [FromQuery] DateTime? checkOut)
        {
            _logger.LogInformation(
                "Müsait oda sorgulama işlemi başlatıldı. CheckIn: {CheckIn}, CheckOut: {CheckOut}",
                checkIn,
                checkOut);

            // Check-in ve check-out bilgilerinden yalnızca biri gönderilmişse hata döndürülür.
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

            // Tarih verilmemişse IsAvailable değeri true olan odalar getirilir.
            if (!checkIn.HasValue && !checkOut.HasValue)
            {
                var availableRooms = await _context.Rooms
                    .Include(r => r.Hotel)
                    .Where(r => r.IsAvailable)
                    .ToListAsync();

                _logger.LogInformation(
                    "Genel müsait oda listesi başarıyla getirildi. Müsait oda sayısı: {RoomCount}",
                    availableRooms.Count);

                return Ok(availableRooms);
            }

            // Nullable tarihler normal DateTime değerlerine çevrilir.
            DateTime checkInDate =
                checkIn.GetValueOrDefault();

            DateTime checkOutDate =
                checkOut.GetValueOrDefault();

            // Check-out tarihi check-in tarihinden sonra olmalıdır.
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

            // Geçmiş tarih için müsaitlik sorgulanmasına izin verilmez.
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

            // İstenen tarih aralığıyla çakışan onaylanmış rezervasyonu olmayan odalar getirilir.
            var availableRoomsByDate =
                await _context.Rooms
                    .Include(r => r.Hotel)
                    .Where(room =>
                        room.IsAvailable &&
                        !room.Reservations.Any(reservation =>
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
        public async Task<ActionResult<Room>> PostRoom(CreateRoomDto room)
        {
            _logger.LogInformation(
                "Yeni oda oluşturma işlemi başlatıldı. HotelId: {HotelId}, RoomNumber: {RoomNumber}",
                room.HotelId,
                room.RoomNumber);

            var hotel = await _context.Hotels
                .FirstOrDefaultAsync(h => h.Id == room.HotelId);

            if (hotel == null)
            {
                _logger.LogWarning(
                    "Oda oluşturma başarısız. Belirtilen otel bulunamadı. HotelId: {HotelId}",
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
                    !int.TryParse(userIdClaim, out int userId))
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

            // DTO'dan Room modeli oluşturulur.
            var newRoom = new Room
            {
                HotelId = room.HotelId,
                RoomNumber = room.RoomNumber,
                RoomType = room.RoomType,
                Capacity = room.Capacity,
                PricePerNight = room.PricePerNight,
                IsAvailable = room.IsAvailable,
                CreatedAt = DateTime.UtcNow
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


        // PUT: api/rooms/5
// Admin her odayı,
// HotelOwner sadece kendi otelindeki odayı değiştirebilir.
//
// Sadece gönderilen alanlar güncellenir.
// Gönderilmeyen alanlar mevcut değerini korur.
[HttpPut("{id}")]
[Authorize(Roles = "Admin,HotelOwner")]
public async Task<IActionResult> PutRoom(
    int id,
    [FromBody] UpdateRoomDto room)
{
    _logger.LogInformation(
        "Oda güncelleme işlemi başlatıldı. RoomId: {RoomId}",
        id);

    // Güncellenecek oda bulunur.
    var existingRoom = await _context.Rooms
        .Include(r => r.Hotel)
        .FirstOrDefaultAsync(r => r.Id == id);

    if (existingRoom == null)
    {
        _logger.LogWarning(
            "Güncellenecek oda bulunamadı. RoomId: {RoomId}",
            id);

        return NotFound(new
        {
            message = "Oda bulunamadı."
        });
    }

    // Token içerisinden giriş yapan kullanıcının ID'si alınır.
    var userIdClaim =
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (userIdClaim == null ||
        !int.TryParse(userIdClaim, out int userId))
    {
        _logger.LogWarning(
            "Oda güncelleme başarısız. Geçersiz token. RoomId: {RoomId}",
            id);

        return Unauthorized(new
        {
            message = "Geçersiz token."
        });
    }

    // HotelOwner sadece kendi otelindeki odayı güncelleyebilir.
    // Admin için sahiplik kontrolü yapılmaz.
    if (!User.IsInRole("Admin") &&
        existingRoom.Hotel?.OwnerId != userId)
    {
        _logger.LogWarning(
            "Yetkisiz oda güncelleme denemesi. RoomId: {RoomId}, UserId: {UserId}, OwnerId: {OwnerId}",
            id,
            userId,
            existingRoom.Hotel?.OwnerId);

        return Forbid();
    }

    // -------------------------------------------------
    // SADECE GÖNDERİLEN ALANLAR GÜNCELLENİR
    // -------------------------------------------------

    if (!string.IsNullOrEmpty(room.RoomNumber))
    {
        existingRoom.RoomNumber = room.RoomNumber;
    }

    if (room.RoomType.HasValue)
    {
        existingRoom.RoomType = room.RoomType.Value;
    }

    if (room.Capacity.HasValue)
    {
        if (room.Capacity.Value <= 0)
        {
            return BadRequest(new
            {
                message = "Oda kapasitesi en az 1 olmalıdır."
            });
        }

        existingRoom.Capacity = room.Capacity.Value;
    }

    if (room.PricePerNight.HasValue)
    {
        if (room.PricePerNight.Value < 0)
        {
            return BadRequest(new
            {
                message = "Oda fiyatı negatif olamaz."
            });
        }

        existingRoom.PricePerNight =
            room.PricePerNight.Value;
    }

    if (room.IsAvailable.HasValue)
    {
        existingRoom.IsAvailable =
            room.IsAvailable.Value;
    }

    // HotelId güncellenmez.
    // Oda mevcut oteline bağlı kalır.

    try
    {
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Oda başarıyla güncellendi. RoomId: {RoomId}, UserId: {UserId}",
            id,
            userId);
    }
    catch (DbUpdateException ex)
    {
        _logger.LogError(
            ex,
            "Oda güncellenirken veritabanı hatası oluştu. RoomId: {RoomId}, UserId: {UserId}",
            id,
            userId);

        return BadRequest(new
        {
            message =
                "Oda güncellenirken veritabanı hatası oluştu."
        });
    }

    return Ok(new
    {
        message = "Oda başarıyla güncellendi.",
        room = existingRoom
    });
}


        // DELETE: api/rooms/5
        // Admin her odayı,
        // HotelOwner sadece kendi otelindeki odayı silebilir.
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,HotelOwner")]
        public async Task<IActionResult> DeleteRoom(int id)
        {
            _logger.LogInformation(
                "Oda silme işlemi başlatıldı. RoomId: {RoomId}",
                id);

            var room = await _context.Rooms
                .Include(r => r.Hotel)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null)
            {
                _logger.LogWarning(
                    "Silinecek oda bulunamadı. RoomId: {RoomId}",
                    id);

                return NotFound(new
                {
                    message = "Oda bulunamadı."
                });
            }

            // HotelOwner sadece kendi odasını silebilir
            if (!User.IsInRole("Admin"))
            {
                var userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (userIdClaim == null ||
                    !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning(
                        "Oda silme başarısız. Geçersiz token. RoomId: {RoomId}",
                        id);

                    return Unauthorized(new
                    {
                        message = "Geçersiz token."
                    });
                }

                // Odanın bağlı olduğu otelin sahibi kontrol edilir.
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

            _context.Rooms.Remove(room);

            try
            {
                // Silme işlemi veritabanına uygulanır.
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Oda başarıyla silindi. RoomId: {RoomId}, HotelId: {HotelId}",
                    id,
                    room.HotelId);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Oda silinirken veritabanı hatası oluştu. RoomId: {RoomId}, HotelId: {HotelId}",
                    id,
                    room.HotelId);

                return BadRequest(new
                {
                    message =
                        "Bu oda ilişkili rezervasyonlar içerdiği için silinemiyor."
                });
            }

            return Ok(new
            {
                message = "Oda başarıyla silindi."
            });
        }
    }
}
