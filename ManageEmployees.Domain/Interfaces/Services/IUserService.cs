using ManageEmployees.Domain.DTO;
using ManageEmployees.Domain.Entities;
using ManageEmployees.Domain.Models;
using System.Net;

namespace ManageEmployees.Domain.Interfaces.Services;

public interface IUserService
{
    Task<Token> SignInAsync(NetworkCredential credentials);
    Task<string> SignUpAsync(NetworkCredential credentials, CreateUser createUser);
    Task<bool> SignOutAsync();
    Task<bool> CanCreateUserAsync(User currentUser, string requestedRole);
    Task<User> GetCurrentUserAsync(string userId);
    Task<bool> UpdateUserAsync(string userId, UpdateUser updateUser);
    Task<bool> DeleteUserAsync(string userId);
    Task<List<UserWithManagerDto>> GetAllUsersAsync();
}