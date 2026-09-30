using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelManagementApi.Data;
using HotelManagementApi.Models;
using HotelManagementApi.DTOs;
using HotelManagementApi.Services;

namespace HotelManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    // Kullanıcıların kayıt ve giriş işlemlerini yöneten Controller'dır.
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context; // Kullanıcı bilgilerine veritabanından erişmek için
        private readonly TokenService _tokenService; // Başarılı giriş sonrasında JWT token oluşturmak için
        private readonly ILogger<AuthController> _logger; // Kayıt ve giriş işlemlerini loglamak için


        // Gerekli servisler Dependency Injection ile Controller'a aktarılır.
        public AuthController(
            AppDbContext context,
            TokenService tokenService,
            ILogger<AuthController> logger)
        {
            _context = context;
            _tokenService = tokenService;
            _logger = logger;
        }

        // Yeni kullanıcı kaydı oluşturur.
        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterDto registerDto)
        {
            _logger.LogInformation(
                "Yeni kullanıcı kayıt işlemi başlatıldı. Username: {Username}, Role: {Role}",
                registerDto.Username,
                registerDto.Role);

            // Email kullanımda mı kontrol et
            if (await _context.Users.AnyAsync(
                u => u.Email == registerDto.Email))
            {
                _logger.LogWarning(
                    "Kayıt başarısız. Email zaten kullanımda. Username: {Username}",
                    registerDto.Username);

                return BadRequest(new
                {
                    message = "Bu e-posta adresi zaten kullanımda."
                });
            }

            // Admin olarak kayıt olunmasını engelle
            if (registerDto.Role == UserRole.Admin)
            {
                _logger.LogWarning(
                    "Admin rolü ile kayıt olma denemesi engellendi. Username: {Username}",
                    registerDto.Username);

                return BadRequest(new
                {
                    message = "Kayıt sırasında Admin rolü seçilemez."
                });
            }

            // Şifreyi güvenli bir şekilde hash'le
            var passwordHasher =
                new Microsoft.AspNetCore.Identity.PasswordHasher<User>();


            // RegisterDto'dan alınan bilgilerle User Entity'si oluşturulur.
            var user = new User
            {
                Username = registerDto.Username,
                Email = registerDto.Email,
                Role = registerDto.Role,
                Balance = 0
            };

            user.PasswordHash =
                passwordHasher.HashPassword(
                    user,
                    registerDto.Password);

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Kullanıcı kaydı başarıyla tamamlandı. UserId: {UserId}, Username: {Username}, Role: {Role}",
                user.Id,
                user.Username,
                user.Role);

            return Ok(new
            {
                message = "Kayıt işlemi başarıyla gerçekleşti."
            });
        }

        // Kullanıcının giriş bilgilerini doğrular ve başarılı girişte JWT token üretir.
        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginDto loginDto)
        {
            _logger.LogInformation(
                "Kullanıcı giriş denemesi başladı. Email: {Email}",
                loginDto.Email);


            // Girilen e-posta adresine ait kullanıcıyı veritabanında arar.
            var user = await _context.Users
                .FirstOrDefaultAsync(
                    u => u.Email == loginDto.Email);


             // Kullanıcı bulunamazsa giriş işlemi reddedilir.
            if (user == null)
            {
                _logger.LogWarning(
                    "Giriş başarısız. Kullanıcı bulunamadı. Email: {Email}",
                    loginDto.Email);

                return Unauthorized(new
                {
                    message = "Geçersiz e-posta veya şifre."
                });
            }

            // Veritabanındaki hashlenmiş şifreyi doğrulamak için PasswordHasher oluşturulur.
            var passwordHasher =
                new Microsoft.AspNetCore.Identity.PasswordHasher<User>();

            //karşılaştırma
            var result =
                passwordHasher.VerifyHashedPassword(
                    user,
                    user.PasswordHash,
                    loginDto.Password);

            if (result ==
                Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed)
            {
                _logger.LogWarning(
                    "Giriş başarısız. Şifre doğrulaması başarısız. UserId: {UserId}",
                    user.Id);

                return Unauthorized(new
                {
                    message = "Geçersiz e-posta veya şifre."
                });
            }

            // Başarılı girişte Token üret
            var token = _tokenService.CreateToken(user);

            _logger.LogInformation(
                "Kullanıcı girişi başarılı. UserId: {UserId}, Username: {Username}, Role: {Role}",
                user.Id,
                user.Username,
                user.Role);

            return Ok(new
            {
                message = "Giriş başarılı",
                token = token,
                username = user.Username,
                role = user.Role.ToString()
            });
        }
    }
}