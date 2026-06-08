using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RestauranteAPI.Data;
using RestauranteAPI.DTOs;
using RestauranteAPI.Models;
using RestauranteAPI.Services.Interfaces;

namespace RestauranteAPI.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly MyAppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(MyAppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == loginDto.Username);

            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid username or password.");
            }

            // Verify password using BCrypt
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                throw new UnauthorizedAccessException("Invalid username or password.");
            }

            // Generate JWT Token
            string token = GenerateJwtToken(user);

            return new AuthResponseDto
            {
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Role = user.Role
                },
                Token = token
            };
        }

        public async Task<UserDto> RegisterAsync(LoginDto registerDto, string fullName, string email, string role)
        {
            var existingUser = await _context.Users
                .AnyAsync(u => u.Username == registerDto.Username || u.Email == email);

            if (existingUser)
            {
                throw new InvalidOperationException("Username or Email already exists.");
            }

            // Hash password using BCrypt
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

            var user = new User
            {
                Username = registerDto.Username,
                FullName = fullName,
                Email = email,
                PasswordHash = passwordHash,
                Role = role
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Role = user.Role
            };
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            string secret = _configuration["Jwt:Secret"] ?? "SuperSecretDefaultKeyMustBeLongEnough1234567890!";
            byte[] key = Encoding.ASCII.GetBytes(secret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim(ClaimTypes.Email, user.Email)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
