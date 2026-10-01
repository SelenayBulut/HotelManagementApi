using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelManagementApi.Data;
using HotelManagementApi.Models;

namespace HotelManagementApi.Controllers
{
    // Ödeme kayıtlarının görüntülenmesini yönetir.
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            AppDbContext context,
            ILogger<PaymentsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/payments
        // SADECE ADMIN
        // Silinmemiş tüm ödeme kayıtlarını getirir.
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPayments()
        {
            _logger.LogInformation(
                "Tüm aktif ödeme kayıtlarını listeleme işlemi başlatıldı.");

            try
            {
                // Sadece silinmemiş ödeme kayıtları alınır.
                // Kayıtlar en yeniden eskiye sıralanır.
                var payments = await _context.Payments
                    .AsNoTracking()
                    .Where(p => !p.IsDeleted)
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new
                    {
                        p.Id,
                        p.UserId,
                        p.ReservationId,
                        p.Amount,
                        p.TransactionType,
                        p.Status,
                        p.CreatedAt
                    })
                    .ToListAsync();

                _logger.LogInformation(
                    "Aktif ödeme kayıtları başarıyla listelendi. " +
                    "Ödeme sayısı: {PaymentCount}",
                    payments.Count);

                return Ok(payments);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Ödeme kayıtları listelenirken veritabanı hatası oluştu.");

                return StatusCode(500, new
                {
                    message = "Ödeme kayıtları alınırken veritabanı hatası oluştu."
                });
            }
        }

        // GET: api/payments/{id}
        // Admin herhangi bir aktif ödemeyi,
        // diğer kullanıcılar ise sadece kendi aktif ödemelerini görebilir.
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPayment(Guid id)
        {
            _logger.LogInformation(
                "Ödeme detayı görüntüleme işlemi başlatıldı. PaymentId: {PaymentId}",
                id);

            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdClaim == null ||
                !Guid.TryParse(userIdClaim, out Guid userId))
            {
                _logger.LogWarning(
                    "Ödeme görüntüleme işlemi başarısız. " +
                    "Geçersiz token. PaymentId: {PaymentId}",
                    id);

                return Unauthorized(new
                {
                    message = "Geçersiz token."
                });
            }

            try
            {
                // İstenen ödeme bulunur.
                // Soft delete edilmiş ödemeler getirilmez.
                var payment = await _context.Payments
                    .AsNoTracking()
                    .Where(p =>
                        p.Id == id &&
                        !p.IsDeleted)
                    .Select(p => new
                    {
                        p.Id,
                        p.UserId,
                        p.ReservationId,
                        p.Amount,
                        p.TransactionType,
                        p.Status,
                        p.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                if (payment == null)
                {
                    _logger.LogWarning(
                        "Ödeme kaydı bulunamadı veya silinmiş durumda. " +
                        "PaymentId: {PaymentId}",
                        id);

                    return NotFound(new
                    {
                        message = "Ödeme kaydı bulunamadı."
                    });
                }

                // Admin herhangi bir aktif ödeme kaydını görebilir.
                if (User.IsInRole("Admin"))
                {
                    _logger.LogInformation(
                        "Admin ödeme detayını görüntüledi. " +
                        "PaymentId: {PaymentId}, AdminUserId: {UserId}",
                        id,
                        userId);

                    return Ok(payment);
                }

                // Normal kullanıcı sadece kendi aktif ödemesini görebilir.
                if (payment.UserId != userId)
                {
                    _logger.LogWarning(
                        "Yetkisiz ödeme görüntüleme denemesi. " +
                        "PaymentId: {PaymentId}, UserId: {UserId}",
                        id,
                        userId);

                    return Forbid();
                }

                _logger.LogInformation(
                    "Kullanıcı kendi ödeme detayını görüntüledi. " +
                    "PaymentId: {PaymentId}, UserId: {UserId}",
                    id,
                    userId);

                return Ok(payment);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Ödeme detayı alınırken veritabanı hatası oluştu. " +
                    "PaymentId: {PaymentId}",
                    id);

                return StatusCode(500, new
                {
                    message = "Ödeme bilgisi alınırken veritabanı hatası oluştu."
                });
            }
        }

        // GET: api/payments/my-payments
        // Giriş yapan kullanıcının kendi aktif ödeme geçmişi
        [HttpGet("my-payments")]
        public async Task<IActionResult> GetMyPayments()
        {
            _logger.LogInformation(
                "Kullanıcının kendi aktif ödeme geçmişi görüntülenmek isteniyor.");

            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdClaim == null ||
                !Guid.TryParse(userIdClaim, out Guid userId))
            {
                _logger.LogWarning(
                    "Kendi ödeme geçmişi görüntülenemedi. " +
                    "Geçersiz token.");

                return Unauthorized(new
                {
                    message = "Geçersiz token."
                });
            }

            try
            {
                // Sadece giriş yapan kullanıcıya ait
                // ve silinmemiş ödeme kayıtları alınır.
                var payments = await _context.Payments
                    .AsNoTracking()
                    .Where(p =>
                        p.UserId == userId &&
                        !p.IsDeleted)
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new
                    {
                        p.Id,
                        p.ReservationId,
                        p.Amount,
                        p.TransactionType,
                        p.Status,
                        p.CreatedAt
                    })
                    .ToListAsync();

                _logger.LogInformation(
                    "Kullanıcının aktif ödeme geçmişi başarıyla listelendi. " +
                    "UserId: {UserId}, Ödeme sayısı: {PaymentCount}",
                    userId,
                    payments.Count);

                return Ok(payments);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Kullanıcının ödeme geçmişi alınırken " +
                    "veritabanı hatası oluştu. UserId: {UserId}",
                    userId);

                return StatusCode(500, new
                {
                    message = "Ödeme geçmişi alınırken veritabanı hatası oluştu."
                });
            }
        }
    }
}