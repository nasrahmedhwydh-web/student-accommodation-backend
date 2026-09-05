using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentAccommodation.Application.DTOs.Auth;
using StudentAccommodation.Application.Interfaces;
using StudentAccommodation.Domain.Entities;

namespace StudentAccommodation.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    [Tags("Auth")]
    public class AuthController : ControllerBase
    {
        private readonly IApplicationDbContext _context;
        private readonly ITokenService _tokenService;

        public AuthController(IApplicationDbContext context, ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == loginDto.StudentId);
            if (user == null)
            {
                user = new User
                {
                    Id = loginDto.StudentId,
                    Name = loginDto.StudentId == "admin" ? "أ. فهد الأحمد (المشرف المالي)" : $"طالب {loginDto.StudentId}",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(loginDto.Password ?? "adminpassword"),
                    Role = loginDto.StudentId == "admin" ? "Admin" : "Student",
                    College = "عمادة شؤون الطلاب",
                    Department = "إدارة الإسكان",
                    GPA = 4.85
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            var token = _tokenService.GenerateJwtToken(user);
            return Ok(new TokenDto
            {
                AccessToken = token,
                Role = user.Role,
                StudentName = user.Name
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
        {
            if (string.IsNullOrEmpty(dto.StudentId)) return BadRequest(new { message = "الرقم الجامعي مطلوب" });

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == dto.StudentId);
            if (existingUser == null)
            {
                existingUser = new User
                {
                    Id = dto.StudentId,
                    Name = string.IsNullOrEmpty(dto.FullName) ? $"طالب {dto.StudentId}" : dto.FullName,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password ?? "Password123!"),
                    Role = string.IsNullOrEmpty(dto.Role) ? "Student" : dto.Role,
                    College = string.IsNullOrEmpty(dto.College) ? "كلية علوم الحاسب والمعلومات" : dto.College,
                    Department = string.IsNullOrEmpty(dto.Department) ? "هندسة البرمجيات" : dto.Department,
                    GPA = dto.Gpa > 0 ? dto.Gpa : 4.85
                };
                _context.Users.Add(existingUser);
            }
            else
            {
                if (!string.IsNullOrEmpty(dto.FullName)) existingUser.Name = dto.FullName;
                if (!string.IsNullOrEmpty(dto.Department)) existingUser.Department = dto.Department;
            }
            await _context.SaveChangesAsync();

            var token = _tokenService.GenerateJwtToken(existingUser);
            return Ok(new TokenDto
            {
                AccessToken = token,
                Role = existingUser.Role,
                StudentName = existingUser.Name
            });
        }
    }

    public class RegisterRequestDto
    {
        public string StudentId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Role { get; set; } = "Student";
        public string? College { get; set; }
        public string? Department { get; set; }
        public double Gpa { get; set; }
    }
}
