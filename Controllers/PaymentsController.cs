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
        // Tüm ödeme kayıtlarını getirir
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPayments()
        {
            _logger.LogInformation(
                "Tüm ödeme kayıtlarını listeleme işlemi başlatıldı.");

            try
            {

                // Sadece gerekli alanlar alınır ve kayıtlar en yeniden eskiye sıralanır.
                var payments = await _context.Payments
                    .AsNoTracking()
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
                    "Tüm ödeme kayıtları başarıyla listelendi. " +
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

        // GET: api/payments/5
         // Admin herhangi bir ödemeyi, diğer kullanıcılar ise sadece kendi ödemelerini görebilir.
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPayment(int id)
        {
            _logger.LogInformation(
                "Ödeme detayı görüntüleme işlemi başlatıldı. PaymentId: {PaymentId}",
                id);

            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdClaim == null ||
                !int.TryParse(userIdClaim, out int userId))
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
                // İstenen ödeme bulunur ve sadece gerekli alanlar alınır.
                var payment = await _context.Payments
                    .AsNoTracking()
                    .Where(p => p.Id == id)
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
                        "Ödeme kaydı bulunamadı. PaymentId: {PaymentId}",
                        id);

                    return NotFound(new
                    {
                        message = "Ödeme kaydı bulunamadı."
                    });
                }

                // Admin herhangi bir ödeme kaydını görebilir.
                if (User.IsInRole("Admin"))
                {
                    _logger.LogInformation(
                        "Admin ödeme detayını görüntüledi. " +
                        "PaymentId: {PaymentId}, AdminUserId: {UserId}",
                        id,
                        userId);

                    return Ok(payment);
                }

                // Normal kullanıcı sadece kendi ödemesini görebilir.
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
        // Giriş yapan kullanıcının kendi ödeme geçmişi
        [HttpGet("my-payments")]
        public async Task<IActionResult> GetMyPayments()
        {
            _logger.LogInformation(
                "Kullanıcının kendi ödeme geçmişi görüntülenmek isteniyor.");

            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdClaim == null ||
                !int.TryParse(userIdClaim, out int userId))
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
                // Sadece giriş yapan kullanıcıya ait ödeme kayıtları alınır.
                var payments = await _context.Payments
                    .AsNoTracking()
                    .Where(p => p.UserId == userId)
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
                    "Kullanıcının ödeme geçmişi başarıyla listelendi. " +
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