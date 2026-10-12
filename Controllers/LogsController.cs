using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelManagementApi.Models.DTOs;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")] // Sadece yöneticiler erişebilir
public class LogsController : ControllerBase
{
    private readonly string _logsDirectory;
    private readonly ILogger<LogsController> _logger;

    public LogsController(IWebHostEnvironment env, ILogger<LogsController> logger)
    {
        // Proje dizinindeki Logs klasörünün yolu
        _logsDirectory = Path.Combine(env.ContentRootPath, "Logs");
        _logger = logger;
    }

    // 1. Tüm log dosyalarını listeleme
    [HttpGet]
    public IActionResult GetLogFiles()
    {
        if (!Directory.Exists(_logsDirectory))
        {
            return Ok(new List<LogFileDto>());
        }

        var files = Directory.GetFiles(_logsDirectory, "*.txt")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.CreationTime)
            .Select(f => new LogFileDto {
                Name = f.Name,
                Date = f.CreationTime.ToString("MMM dd, yyyy"),
                Size = $"{Math.Round(f.Length / 1024.0, 1)} KB"
            })
            .ToList();

        return Ok(files);
    }

    // 2. Belirli bir log dosyasının içeriğini satır satır okuyup döndürme
    [HttpGet("{fileName}")]
    public IActionResult GetLogContent(string fileName)
    {
        // Güvenlik kontrolü (Path Traversal engelleme)
        var safeFileName = Path.GetFileName(fileName);
        var filePath = Path.Combine(_logsDirectory, safeFileName);

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound(new { message = "Log file not found." });
        }

        var logEntries = new List<LogLineDto>();

        try
        {
            // Serilog'un kilitlediği dosyayı ReadWrite izniyle güvenle açıyoruz
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fileStream);

            int id = 1;
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (line == null) continue;

                // Örnek Serilog formatı ayrıştırma: "2026-10-07 09:07:29.981 +03:00 [INF] Mesaj..."
                string time = line.Length >= 23 ? line.Substring(0, 23) : "N/A";
                string level = "INF";
                string text = line;

                if (line.Contains("[WRN]")) level = "WRN";
                else if (line.Contains("[ERR]")) level = "ERR";

                logEntries.Add(new LogLineDto {
                    Id = id++,
                    Time = time.Trim(),
                    Level = level,
                    Text = text
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Log dosyası okunurken hata oluştu. Dosya: {FileName}", safeFileName);
            return StatusCode(500, new { message = "Log dosyası okunamadı.", details = ex.Message });
        }

        return Ok(logEntries);
    }
}