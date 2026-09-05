namespace StudentAccommodation.Application.DTOs.Auth
{
    public class LoginDto
    {
        public string StudentId { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class TokenDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
    }
}
