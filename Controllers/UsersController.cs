
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelManagementApi.Data;
using HotelManagementApi.Models;
using HotelManagementApi.DTOs;

namespace HotelManagementApi.Controllers
{
    // Kullanıcıların görüntülenmesi ve yönetilmesi işlemlerini gerçekleştirir.
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UsersController> _logger;
        private readonly PasswordHasher<User> _passwordHasher;

        public UsersController(
            AppDbContext context,
            ILogger<UsersController> logger)
        {
            _context = context;
            _logger = logger;
            _passwordHasher = new PasswordHasher<User>();
        }


        // GET: api/users
        // SADECE ADMIN
        // Sadece aktif kullanıcıları getirir.
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUsers()
        {
            _logger.LogInformation(
                "Tüm aktif kullanıcıları listeleme işlemi başlatıldı.");

            try
            {
                // Şifre hash'i gibi hassas bilgiler response'a dahil edilmez.
                // Soft delete yapılmış kullanıcılar listelenmez.
                var users = await _context.Users
                    .AsNoTracking()
                    .Where(u => !u.IsDeleted)
                    .Select(u => new
                    {
                        u.Id,
                        u.Username,
                        u.Email,
                        u.Role,
                        u.Balance,
                        u.CreatedAt
                    })
                    .ToListAsync();

                _logger.LogInformation(
                    "Aktif kullanıcılar başarıyla listelendi. " +
                    "Kullanıcı sayısı: {UserCount}",
                    users.Count);

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Kullanıcılar listelenirken beklenmeyen bir hata oluştu.");

                throw;
            }
        }


        // GET: api/users/{id}
        // Admin herkesi, normal kullanıcı sadece kendisini görebilir.
        // Silinmiş kullanıcılar görüntülenemez.
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(Guid id)
        {
            _logger.LogInformation(
                "Kullanıcı detayı görüntüleme işlemi başlatıldı. " +
                "UserId: {UserId}",
                id);

            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdClaim == null ||
                !Guid.TryParse(userIdClaim, out Guid currentUserId))
            {
                _logger.LogWarning(
                    "Kullanıcı detayı görüntülenemedi. Geçersiz token. " +
                    "İstenen UserId: {UserId}",
                    id);

                return Unauthorized(new
                {
                    message = "Geçersiz token."
                });
            }

            // Admin bütün kullanıcıları görebilir.
            if (!User.IsInRole("Admin") && currentUserId != id)
            {
                _logger.LogWarning(
                    "Yetkisiz kullanıcı görüntüleme denemesi. " +
                    "CurrentUserId: {CurrentUserId}, " +
                    "RequestedUserId: {RequestedUserId}",
                    currentUserId,
                    id);

                return Forbid();
            }

            try
            {
                // Sadece aktif kullanıcı bulunur.
                // Şifre hash'i response'a eklenmez.
                var user = await _context.Users
                    .AsNoTracking()
                    .Where(u =>
                        u.Id == id &&
                        !u.IsDeleted)
                    .Select(u => new
                    {
                        u.Id,
                        u.Username,
                        u.Email,
                        u.Role,
                        u.Balance,
                        u.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    _logger.LogWarning(
                        "Kullanıcı bulunamadı veya silinmiş. UserId: {UserId}",
                        id);

                    return NotFound(new
                    {
                        message = "Kullanıcı bulunamadı."
                    });
                }

                _logger.LogInformation(
                    "Kullanıcı detayı başarıyla görüntülendi. " +
                    "UserId: {UserId}",
                    id);

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Kullanıcı detayı alınırken beklenmeyen hata oluştu. " +
                    "UserId: {UserId}",
                    id);

                throw;
            }
        }


        // POST: api/users
        // SADECE ADMIN
        // Yeni kullanıcı oluşturur.
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PostUser(
            [FromBody] CreateUserDto dto)
        {
            _logger.LogInformation(
                "Yeni kullanıcı oluşturma işlemi başlatıldı. " +
                "Username: {Username}, Email: {Email}, Role: {Role}",
                dto.Username,
                dto.Email,
                dto.Role);

            if (string.IsNullOrWhiteSpace(dto.Username) ||
                string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Password))
            {
                _logger.LogWarning(
                    "Kullanıcı oluşturma başarısız. " +
                    "Zorunlu alanlardan biri boş.");

                return BadRequest(new
                {
                    message = "Username, Email ve Password alanları zorunludur."
                });
            }

            if (dto.Password.Length < 6)
            {
                _logger.LogWarning(
                    "Kullanıcı oluşturma başarısız. " +
                    "Şifre minimum uzunluk şartını sağlamıyor.");

                return BadRequest(new
                {
                    message = "Şifre en az 6 karakter olmalıdır."
                });
            }

            if (dto.Balance < 0)
            {
                _logger.LogWarning(
                    "Kullanıcı oluşturma başarısız. " +
                    "Negatif bakiye gönderildi. Email: {Email}",
                    dto.Email);

                return BadRequest(new
                {
                    message = "Bakiye negatif olamaz."
                });
            }

            try
            {
                // Soft delete edilmiş kullanıcılar da veritabanında tutulduğu
                // için email daha önce kullanılmışsa tekrar kullanılamaz.
                var emailExists = await _context.Users
                    .AnyAsync(u => u.Email == dto.Email);

                if (emailExists)
                {
                    _logger.LogWarning(
                        "Kullanıcı oluşturma başarısız. " +
                        "Email zaten kayıtlı: {Email}",
                        dto.Email);

                    return BadRequest(new
                    {
                        message =
                            "Bu e-posta adresi ile zaten bir kayıt mevcut."
                    });
                }

                // Soft delete edilmiş kullanıcılar da tutulduğu için
                // username daha önce kullanılmışsa tekrar kullanılamaz.
                var usernameExists = await _context.Users
                    .AnyAsync(u => u.Username == dto.Username);

                if (usernameExists)
                {
                    _logger.LogWarning(
                        "Kullanıcı oluşturma başarısız. " +
                        "Username zaten kayıtlı: {Username}",
                        dto.Username);

                    return BadRequest(new
                    {
                        message =
                            "Bu kullanıcı adı zaten kullanılmaktadır."
                    });
                }

                var user = new User
                {
                    Username = dto.Username,
                    Email = dto.Email,
                    Role = dto.Role,
                    Balance = dto.Balance,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                // Şifreyi hashliyoruz.
                user.PasswordHash =
                    _passwordHasher.HashPassword(
                        user,
                        dto.Password);

                _context.Users.Add(user);

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Yeni kullanıcı başarıyla oluşturuldu. " +
                    "UserId: {UserId}, Username: {Username}, Role: {Role}",
                    user.Id,
                    user.Username,
                    user.Role);

                return CreatedAtAction(
                    nameof(GetUser),
                    new { id = user.Id },
                    new
                    {
                        user.Id,
                        user.Username,
                        user.Email,
                        user.Role,
                        user.Balance,
                        user.CreatedAt
                    });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Yeni kullanıcı oluşturulurken veritabanı hatası oluştu. " +
                    "Email: {Email}",
                    dto.Email);

                return StatusCode(500, new
                {
                    message =
                        "Kullanıcı oluşturulurken veritabanı hatası oluştu."
                });
            }
        }


        // PUT: api/users/{id}
        // Sadece Admin kullanıcı güncelleyebilir.
        // Silinmiş kullanıcılar güncellenemez.
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutUser(
            Guid id,
            [FromBody] UpdateUserDto dto)
        {
            _logger.LogInformation(
                "Kullanıcı güncelleme işlemi başlatıldı. " +
                "UserId: {UserId}",
                id);

            try
            {
                // Sadece aktif kullanıcı bulunur.
                var existingUser =
                    await _context.Users
                        .FirstOrDefaultAsync(u =>
                            u.Id == id &&
                            !u.IsDeleted);

                if (existingUser == null)
                {
                    _logger.LogWarning(
                        "Kullanıcı güncellenemedi. " +
                        "Kullanıcı bulunamadı veya silinmiş. " +
                        "UserId: {UserId}",
                        id);

                    return NotFound(new
                    {
                        message = "Kullanıcı bulunamadı."
                    });
                }

                // Username değişiyorsa benzersizlik kontrolü
                if (!string.IsNullOrWhiteSpace(dto.Username) &&
                    dto.Username != existingUser.Username)
                {
                    var usernameExists = await _context.Users
                        .AnyAsync(u =>
                            u.Username == dto.Username &&
                            u.Id != id);

                    if (usernameExists)
                    {
                        _logger.LogWarning(
                            "Kullanıcı güncelleme başarısız. " +
                            "Username zaten kullanılıyor. UserId: {UserId}",
                            id);

                        return BadRequest(new
                        {
                            message =
                                "Bu kullanıcı adı başka bir kullanıcı tarafından kullanılmaktadır."
                        });
                    }

                    existingUser.Username = dto.Username;
                }

                // Email değişiyorsa yeni değerin benzersizliği kontrol edilir.
                if (!string.IsNullOrWhiteSpace(dto.Email) &&
                    dto.Email != existingUser.Email)
                {
                    var emailExists = await _context.Users
                        .AnyAsync(u =>
                            u.Email == dto.Email &&
                            u.Id != id);

                    if (emailExists)
                    {
                        _logger.LogWarning(
                            "Kullanıcı güncelleme başarısız. " +
                            "Email zaten kullanılıyor. UserId: {UserId}",
                            id);

                        return BadRequest(new
                        {
                            message =
                                "Bu e-posta adresi başka bir kullanıcı tarafından kullanılmaktadır."
                        });
                    }

                    existingUser.Email = dto.Email;
                }

                // Rol gönderilmişse kullanıcı rolü güncellenir.
                if (dto.Role.HasValue)
                {
                    existingUser.Role = dto.Role.Value;
                }

                // Bakiye değişikliği
                if (dto.Balance.HasValue)
                {
                    if (dto.Balance.Value < 0)
                    {
                        _logger.LogWarning(
                            "Kullanıcı güncelleme başarısız. " +
                            "Negatif bakiye gönderildi. UserId: {UserId}",
                            id);

                        return BadRequest(new
                        {
                            message = "Bakiye negatif olamaz."
                        });
                    }

                    existingUser.Balance = dto.Balance.Value;
                }

                // Şifre değişikliği
                if (!string.IsNullOrWhiteSpace(dto.Password))
                {
                    if (dto.Password.Length < 6)
                    {
                        _logger.LogWarning(
                            "Kullanıcı güncelleme başarısız. " +
                            "Şifre minimum uzunluk şartını sağlamıyor. " +
                            "UserId: {UserId}",
                            id);

                        return BadRequest(new
                        {
                            message =
                                "Şifre en az 6 karakter olmalıdır."
                        });
                    }

                    existingUser.PasswordHash =
                        _passwordHasher.HashPassword(
                            existingUser,
                            dto.Password);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Kullanıcı başarıyla güncellendi. UserId: {UserId}",
                    id);

                return Ok(new
                {
                    message = "Kullanıcı başarıyla güncellendi."
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Kullanıcı güncellenirken veritabanı hatası oluştu. " +
                    "UserId: {UserId}",
                    id);

                return StatusCode(500, new
                {
                    message =
                        "Kullanıcı güncellenirken veritabanı hatası oluştu."
                });
            }
        }


        // DELETE: api/users/{id}
        // Sadece Admin kullanıcı silebilir.
        // Eğer kullanıcı HotelOwner ise:
        // Kullanıcı -> Oteller -> Odalar -> Rezervasyonlar -> Ödemeler
        // zinciri soft delete yapılır.
        //
        // Eğer kullanıcı Customer ise:
        // Kullanıcının rezervasyonları ve bu rezervasyonlara ait
        // ödemeler soft delete yapılır.
        [HttpDelete("{id}")]
[Authorize]
public async Task<IActionResult> DeleteUser(Guid id)
{
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid currentUserId))
    {
        return Unauthorized("Kullanıcı kimliği alınamadı.");
    }

    // Kullanıcı sadece kendi hesabını silebilir.
    if (currentUserId != id)
    {
        return Forbid();
    }

    var user = await _context.Users
        .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

    if (user == null)
    {
        return NotFound("Kullanıcı bulunamadı.");
    }

    user.IsDeleted = true;

    // Kullanıcının otellerini soft delete
    var hotels = await _context.Hotels
        .Where(h => h.OwnerId == user.Id && !h.IsDeleted)
        .ToListAsync();

    foreach (var hotel in hotels)
    {
        hotel.IsDeleted = true;
    }

    var hotelIds = hotels.Select(h => h.Id).ToList();

    // Otellere ait odaları soft delete
    var rooms = await _context.Rooms
        .Where(r => hotelIds.Contains(r.HotelId) && !r.IsDeleted)
        .ToListAsync();

    foreach (var room in rooms)
    {
        room.IsDeleted = true;
    }

    var roomIds = rooms.Select(r => r.Id).ToList();

    // Kullanıcının rezervasyonları + otellerindeki odaların rezervasyonları
    var reservations = await _context.Reservations
        .Where(r =>
            (r.UserId == user.Id || roomIds.Contains(r.RoomId))
            && !r.IsDeleted)
        .ToListAsync();

    foreach (var reservation in reservations)
    {
        reservation.IsDeleted = true;
    }

    var reservationIds = reservations.Select(r => r.Id).ToList();

    // İlgili ödemeleri soft delete
    var payments = await _context.Payments
        .Where(p => reservationIds.Contains(p.ReservationId) && !p.IsDeleted)
        .ToListAsync();

    foreach (var payment in payments)
    {
        payment.IsDeleted = true;
    }

    await _context.SaveChangesAsync();

    return Ok(new
    {
        message = "Hesabınız başarıyla silindi.",
        userId = user.Id
    });
}

        // GET: api/users/profile
        // Giriş yapan kullanıcının kendi aktif profilini getirir.
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            _logger.LogInformation(
                "Kullanıcı profil görüntüleme işlemi başlatıldı.");

            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdClaim == null ||
                !Guid.TryParse(userIdClaim, out Guid userId))
            {
                _logger.LogWarning(
                    "Profil görüntülenemedi. Geçersiz token.");

                return Unauthorized(new
                {
                    message = "Geçersiz token."
                });
            }

            try
            {
                // Token'daki kullanıcı ID'sine ait aktif profil getirilir.
                var user = await _context.Users
                    .AsNoTracking()
                    .Where(u =>
                        u.Id == userId &&
                        !u.IsDeleted)
                    .Select(u => new
                    {
                        u.Id,
                        u.Username,
                        u.Email,
                        u.Role,
                        u.Balance,
                        u.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    _logger.LogWarning(
                        "Profil görüntülenemedi. " +
                        "Kullanıcı bulunamadı veya silinmiş. " +
                        "UserId: {UserId}",
                        userId);

                    return NotFound(new
                    {
                        message = "Kullanıcı bulunamadı."
                    });
                }

                _logger.LogInformation(
                    "Kullanıcı profili başarıyla görüntülendi. " +
                    "UserId: {UserId}",
                    userId);

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Profil bilgileri alınırken beklenmeyen hata oluştu. " +
                    "UserId: {UserId}",
                    userId);

                throw;
            }
        }


        // PUT: api/users/profile
        // Kullanıcı kendi aktif profilini günceller.
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(
            [FromBody] UpdateProfileDto updateDto)
        {
            _logger.LogInformation(
                "Kullanıcı profil güncelleme işlemi başlatıldı.");

            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdClaim == null ||
                !Guid.TryParse(userIdClaim, out Guid userId))
            {
                _logger.LogWarning(
                    "Profil güncellenemedi. Geçersiz token.");

                return Unauthorized(new
                {
                    message = "Geçersiz token."
                });
            }

            try
            {
                // Token'daki kullanıcıya ait aktif kayıt bulunur.
                var user =
                    await _context.Users
                        .FirstOrDefaultAsync(u =>
                            u.Id == userId &&
                            !u.IsDeleted);

                if (user == null)
                {
                    _logger.LogWarning(
                        "Profil güncellenemedi. " +
                        "Kullanıcı bulunamadı veya silinmiş. " +
                        "UserId: {UserId}",
                        userId);

                    return NotFound(new
                    {
                        message = "Kullanıcı bulunamadı."
                    });
                }


                // Email değiştirilecekse benzersizlik kontrolü yapılır.
                if (!string.IsNullOrWhiteSpace(updateDto.Email) &&
                    updateDto.Email != user.Email)
                {
                    bool emailExists =
                        await _context.Users.AnyAsync(
                            u =>
                                u.Email == updateDto.Email &&
                                u.Id != userId);

                    if (emailExists)
                    {
                        _logger.LogWarning(
                            "Profil güncelleme başarısız. " +
                            "Email başka kullanıcı tarafından kullanılıyor. " +
                            "UserId: {UserId}",
                            userId);

                        return BadRequest(new
                        {
                            message =
                                "Bu e-posta adresi başka bir kullanıcı tarafından kullanılmaktadır."
                        });
                    }

                    user.Email = updateDto.Email;
                }


                // Username değiştirilecekse benzersizlik kontrolü yapılır.
                if (!string.IsNullOrWhiteSpace(updateDto.Username) &&
                    updateDto.Username != user.Username)
                {
                    bool usernameExists =
                        await _context.Users.AnyAsync(
                            u =>
                                u.Username == updateDto.Username &&
                                u.Id != userId);

                    if (usernameExists)
                    {
                        _logger.LogWarning(
                            "Profil güncelleme başarısız. " +
                            "Username başka kullanıcı tarafından kullanılıyor. " +
                            "UserId: {UserId}",
                            userId);

                        return BadRequest(new
                        {
                            message =
                                "Bu kullanıcı adı başka bir kullanıcı tarafından kullanılmaktadır."
                        });
                    }

                    user.Username = updateDto.Username;
                }


                // Yeni şifre gönderilmişse kontrol edilip hashlenir.
                if (!string.IsNullOrWhiteSpace(updateDto.Password))
                {
                    if (updateDto.Password.Length < 6)
                    {
                        _logger.LogWarning(
                            "Profil güncelleme başarısız. " +
                            "Şifre minimum uzunluk şartını sağlamıyor. " +
                            "UserId: {UserId}",
                            userId);

                        return BadRequest(new
                        {
                            message =
                                "Şifre en az 6 karakter olmalıdır."
                        });
                    }

                    user.PasswordHash =
                        _passwordHasher.HashPassword(
                            user,
                            updateDto.Password);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Kullanıcı profili başarıyla güncellendi. " +
                    "UserId: {UserId}",
                    userId);

                return Ok(new
                {
                    message =
                        "Profil bilgileriniz başarıyla güncellendi."
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Profil güncellenirken veritabanı hatası oluştu. " +
                    "UserId: {UserId}",
                    userId);

                return StatusCode(500, new
                {
                    message =
                        "Profil güncellenirken veritabanı hatası oluştu."
                });
            }
        }
    }
}
