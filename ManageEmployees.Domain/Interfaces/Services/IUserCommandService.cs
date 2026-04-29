using ManageEmployees.Domain.DTO;
using ManageEmployees.Domain.Models;
using System.Net;

namespace ManageEmployees.Domain.Interfaces.Services;

public interface IUserCommandService
{
    Task<Token> SignInAsync(NetworkCredential credentials);
    Task<string> SignUpAsync(CreateUser createUser);
    Task<bool> SignOutAsync();
    Task<bool> UpdateUserAsync(string userId, UpdateUser updateUser);
    Task<bool> DeleteUserAsync(string userId);
}
