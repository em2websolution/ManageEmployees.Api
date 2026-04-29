using ManageEmployees.Domain;
using ManageEmployees.Domain.DTO;
using ManageEmployees.Domain.Entities;
using ManageEmployees.Domain.Exceptions;
using ManageEmployees.Domain.Interfaces.Repositories;
using ManageEmployees.Domain.Interfaces.Services;
using ManageEmployees.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Net;

namespace ManageEmployees.Services.Services;

public class UserService : IUserQueryService, IUserCommandService
{
    private const string ACCESS_TOKEN = "access_token";
    private const string REFRESH_TOKEN = "refresh_token";
    private const string USER = "user";

    private readonly ILogger<UserService> _logger;
    private readonly UserManager<User> _userManager;
    private readonly IAuthService _authService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserRepository _userRepository;

    public UserService(
        UserManager<User> userManager,
        IAuthService authService,
        ILogger<UserService> logger,
        IHttpContextAccessor httpContextAccessor,
        IUserRepository userRepository)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task<Token> SignInAsync(NetworkCredential credentials)
    {
        _logger.LogInformation("Signing in user {UserName}", credentials.UserName);

        var user = await _userManager.FindByNameAsync(credentials.UserName)
            ?? throw new NotFoundException($"User {credentials.UserName} not found!");

        var isAValidPwd = await _userManager.CheckPasswordAsync(user, credentials.Password);
        if (!isAValidPwd)
        {
            _logger.LogInformation("Invalid password for user {UserName}", credentials.UserName);
            throw new BusinessException("Invalid password!");
        }

        var userName = user.UserName;
        if (string.IsNullOrEmpty(userName))
            throw new BusinessException("User name is missing!");

        var token = await _authService.GenerateTokenAsync(userName.ToLower());
        token.Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault()!;
        token.FirstName = user.FirstName;
        token.UserId = user.Id;

        InsertTokenIntoCookies(user.Id, token);

        _logger.LogInformation("Signing successful for user {UserName}", credentials.UserName);

        return token;
    }

    public async Task<string> SignUpAsync(CreateUser createUser)
    {
        _logger.LogInformation("Creating new user...");

        if (!new[] { RoleName.Administrator, RoleName.Employee }.Contains(createUser.Role))
            throw new BusinessException($"Invalid role: {createUser.Role}");

        if (await Exists(createUser.Email.ToLower()))
            throw new BusinessException("User already exists!");

        var user = new User
        {
            Email = createUser.Email.ToLower(),
            UserName = createUser.Email.ToLower(),
            NormalizedEmail = createUser.Email.ToUpper(),
            FirstName = createUser.FirstName,
            LastName = createUser.LastName,
            PhoneNumber = createUser.PhoneNumber,
            DocNumber = createUser.DocNumber,
            EmailConfirmed = true,
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };

        var credentials = new NetworkCredential(createUser.Email.ToLower(), createUser.Password);
        var response = await _userManager.CreateAsync(user, credentials.Password);

        if (response.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, createUser.Role);

            _logger.LogInformation("User {UserName} created successfully with Role {Role}!", credentials.UserName, createUser.Role);

            return "User created successfully!";
        }

        var userErrors = response.Errors.ToList();
        throw new BusinessException(
            "Couldn't create a new user!",
            userErrors.Select(e => new Error { Code = e.Code, Message = e.Description }).ToList()
        );
    }

    private async Task<bool> Exists(string userName)
    {
        return await _userManager.FindByNameAsync(userName) is not null;
    }

    public async Task<bool> SignOutAsync()
    {
        try
        {
            var userId = _httpContextAccessor.HttpContext?.Request.Cookies[USER];

            if (string.IsNullOrEmpty(userId))
                throw new BusinessException("User ID not found in cookies!");

            _logger.LogInformation("Signing out! || User: {UserId}", userId[..8]);

            await _authService.RemoveRefreshTokenAsync(userId);

            _httpContextAccessor.HttpContext!.Response.Cookies.Delete(ACCESS_TOKEN);
            _httpContextAccessor.HttpContext.Response.Cookies.Delete(REFRESH_TOKEN);
            _httpContextAccessor.HttpContext.Response.Cookies.Delete(USER);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during sign out.");
            return false;
        }
    }

    private void InsertTokenIntoCookies(string userId, Token? token)
    {
        var cookie = GetCookieOptions();

        _httpContextAccessor.HttpContext!.Response.Cookies.Append(ACCESS_TOKEN, token?.AccessToken!, cookie);
        _httpContextAccessor.HttpContext.Response.Cookies.Append(REFRESH_TOKEN, token?.RefreshToken!, cookie);
        _httpContextAccessor.HttpContext.Response.Cookies.Append(USER, userId, cookie);
    }

    private static CookieOptions GetCookieOptions() =>
        new()
        {
            Expires = DateTimeOffset.Now.AddMinutes(15),
            HttpOnly = true,
            Path = "/",
            Secure = true,
            SameSite = SameSiteMode.None
        };

    public async Task<bool> UpdateUserAsync(string userId, UpdateUser updateUser)
    {
        var currentUser = await _userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException($"User with ID {userId} not found!");

        currentUser.FirstName = updateUser.FirstName;
        currentUser.LastName = updateUser.LastName;
        currentUser.Email = updateUser.Email.ToLower();
        currentUser.NormalizedEmail = updateUser.Email.ToUpper();
        currentUser.DocNumber = updateUser.DocNumber;
        currentUser.PhoneNumber = updateUser.PhoneNumber;

        var currentRoles = await _userManager.GetRolesAsync(currentUser);
        if (!currentRoles.Contains(updateUser.Role))
        {
            await _userManager.RemoveFromRolesAsync(currentUser, currentRoles);
            await _userManager.AddToRoleAsync(currentUser, updateUser.Role);
        }

        var passwordHash = _userManager.PasswordHasher.HashPassword(currentUser, updateUser.Password);
        currentUser.PasswordHash = passwordHash;

        var result = await _userManager.UpdateAsync(currentUser);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new BusinessException($"Failed to update user: {errors}");
        }

        return true;
    }

    public async Task<bool> DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException($"User with ID {userId} not found!");

        var result = await _userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new BusinessException($"Failed to delete user: {errors}");
        }

        return true;
    }

    public async Task<PagedResult<UserDto>> GetAllUsersAsync(int page, int pageSize, string? search = null, string? role = null)
    {
        return await _userRepository.GetAllWithRolesAsync(page, pageSize, search, role);
    }
}