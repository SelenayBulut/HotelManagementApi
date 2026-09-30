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
        // Tüm otelleri ve odalarını getirir.
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Hotel>>> GetHotels()
        {
            _logger.LogInformation(
                "Tüm otelleri listeleme işlemi başlatıldı.");


            // Otellerle birlikte bağlı odalar da getirilir.
            var hotels = await _context.Hotels
                .Include(h => h.Rooms)
                .ToListAsync();

            _logger.LogInformation(
                "Oteller başarıyla listelendi. Otel sayısı: {HotelCount}",
                hotels.Count);

            return hotels;
        }

        // GET: api/hotels/5
        // ID'si verilen oteli ve odalarını getirir.
        [HttpGet("{id}")]
        public async Task<ActionResult<Hotel>> GetHotel(int id)
        {
            _logger.LogInformation(
                "Otel bilgisi getirme işlemi başlatıldı. HotelId: {HotelId}",
                id);

            var hotel = await _context.Hotels
                .Include(h => h.Rooms)
                .FirstOrDefaultAsync(h => h.Id == id);

            // Otel bulunamazsa 404 döndürülür.
            if (hotel == null)
            {
                _logger.LogWarning(
                    "Otel bulunamadı. HotelId: {HotelId}",
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
                !int.TryParse(userIdClaim, out int userId))
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
                CreatedAt = DateTime.UtcNow
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

        // PUT: api/hotels/5
        // Mevcut otelin bilgilerini günceller.
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,HotelOwner")]
        public async Task<IActionResult> UpdateHotel(
            int id,
            [FromBody] UpdateHotelDto updateDto)
        {
            _logger.LogInformation(
                "Otel güncelleme işlemi başlatıldı. HotelId: {HotelId}",
                id);

            // Güncellenecek otel bulunur.
            var hotel =
                await _context.Hotels.FindAsync(id);

            if (hotel == null)
            {
                _logger.LogWarning(
                    "Güncellenecek otel bulunamadı. HotelId: {HotelId}",
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
                !int.TryParse(userIdClaim, out int userId))
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

        // DELETE: api/hotels/5
        // Belirtilen oteli siler.
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,HotelOwner")]
        public async Task<IActionResult> DeleteHotel(int id)
        {
            _logger.LogInformation(
                "Otel silme işlemi başlatıldı. HotelId: {HotelId}",
                id);

            // Silinecek otel bulunur
            var hotel =
                await _context.Hotels.FindAsync(id);

            if (hotel == null)
            {
                _logger.LogWarning(
                    "Silinecek otel bulunamadı. HotelId: {HotelId}",
                    id);

                return NotFound(new
                {
                    message =
                        "Silinecek otel bulunamadı."
                });
            }

            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdClaim == null ||
                !int.TryParse(userIdClaim, out int userId))
            {
                _logger.LogWarning(
                    "Otel silme başarısız. Geçersiz token. HotelId: {HotelId}",
                    id);

                return Unauthorized(new
                {
                    message = "Geçersiz token."
                });
            }

            // HotelOwner yalnızca kendi otelini silebilir.
            // Admin için bu sahiplik kontrolü uygulanmaz.
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
            // Otel silinmek üzere işaretlenir.
            _context.Hotels.Remove(hotel);

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Otel başarıyla silindi. HotelId: {HotelId}, UserId: {UserId}",
                    id,
                    userId);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Otel silinirken veritabanı hatası oluştu. HotelId: {HotelId}, UserId: {UserId}",
                    id,
                    userId);

                return BadRequest(new
                {
                    message =
                        "Bu otel ilişkili oda veya kayıtlar içerdiği için silinemiyor."
                });
            }

            return Ok(new
            {
                message = "Otel başarıyla silindi."
            });
        }
    }
}