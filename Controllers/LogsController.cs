using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")] // Sadece yöneticiler erişebilir
public class LogsController : ControllerBase
{
    private readonly string _logsDirectory;

    public LogsController(IWebHostEnvironment env)
    {
        // Proje dizinindeki Logs klasörünün yolu (Görselindeki yapıya uygun)
        _logsDirectory = Path.Combine(env.ContentRootPath, "Logs");
    }

    // 1. Tüm log dosyalarını listeleme
    [HttpGet]
    public IActionResult GetLogFiles()
    {
        if (!Directory.Exists(_logsDirectory))
        {
            return Ok(new List<object>());
        }

        var files = Directory.GetFiles(_logsDirectory, "*.txt")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.CreationTime)
            .Select(f => new {
                name = f.Name,
                date = f.CreationTime.ToString("MMM dd, yyyy"),
                size = $"{Math.Round(f.Length / 1024.0, 1)} KB"
            });

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

        var lines = System.IO.File.ReadAllLines(filePath);
        var logEntries = new List<object>();

        int id = 1;
        foreach (var line in lines)
        {
            // Örnek Serilog formatı ayrıştırma: "2026-10-07 09:07:29.981 +03:00 [INF] Mesaj..."
            string time = line.Length >= 23 ? line.Substring(0, 23) : "N/A";
            string level = "INF";
            string text = line;

            if (line.Contains("[WRN]")) level = "WRN";
            else if (line.Contains("[ERR]")) level = "ERR";

            logEntries.Add(new {
                id = id++,
                time = time.Trim(),
                level = level,
                text = text
            });
        }

        return Ok(logEntries);
    }
}