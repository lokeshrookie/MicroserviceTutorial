using AuthService.Models;

namespace AuthService.Services;

public interface IAuthService
{
    Task<string> Register(string username, string password, string role);
    Task<string> Login(string username, string password);
    Task<bool> ValidateToken(string token);
    Task<List<User>> GetUsersAsync();
}