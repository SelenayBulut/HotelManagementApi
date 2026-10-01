using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HotelManagementApi.Models;
using Microsoft.IdentityModel.Tokens;

namespace HotelManagementApi.Services
{

    // Kullanıcı için JWT token oluşturma işlemini gerçekleştirir.
    public class TokenService
    {
        // appsettings.json içerisindeki JWT ayarlarına erişmek için 
        private readonly IConfiguration _configuration;

         // JWT ayarlarını Dependency Injection ile alır.
        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        // Giriş yapan kullanıcı için JWT token oluşturur.
        public string CreateToken(User user)
        {

            // Token içerisinde kullanıcıyı tanımlamak ve yetkilendirmek için
            // kullanılacak bilgiler hazırlanır.
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            // appsettings.json'daki Secret kullanılarak token'ın güvenlik anahtarı oluşturulur.
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);


            // Token'ın içeriği ve geçerlilik süresi gibi temel ayarlar
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(double.Parse(_configuration["JwtSettings:DurationInDays"] ?? "7")),
                Issuer = _configuration["JwtSettings:Issuer"],
                Audience = _configuration["JwtSettings:Audience"],
                SigningCredentials = creds
            };

            // JWT oluşturur ve string formatına çevirir
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}