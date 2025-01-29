using ManageEmployees.Domain;
using ManageEmployees.Domain.DTO;
using ManageEmployees.Domain.Entities;
using ManageEmployees.Domain.Exceptions;
using ManageEmployees.Domain.Interfaces.Services;
using ManageEmployees.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;

namespace ManageEmployees.Services.Services;

public class UserService : IUserService
{
    private const string ACCESS_TOKEN = "access_token";
    private const string REFRESH_TOKEN = "refresh_token";
    private const string USER = "user";

    private readonly ILogger<UserService> _logger;
    private readonly UserManager<User> _userManager;
    private readonly IAuthService _authService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IEncryptionService _encryptationService;

    public UserService(
        UserManager<User> userManager,
        IAuthService authService,
        ILogger<UserService> logger,
        IHttpContextAccessor httpContextAccessor,
        IEncryptionService encryptationService)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _encryptationService = encryptationService ?? throw new ArgumentNullException(nameof(encryptationService));

    }

    public async Task<Token> SignInAsync(NetworkCredential credentials)
    {
        _logger.LogInformation("Signing in user {UserName}", credentials.UserName);
        try
        {
            var user = await _userManager.FindByNameAsync(credentials.UserName) ?? throw new BusinessException($"User {credentials.UserName} not found!");
            var isLockedOut = await _userManager.GetLockoutEnabledAsync(user);

            if (isLockedOut)
                throw new BusinessException($"User {credentials.UserName} is blocked!");

            string password = _encryptationService.Decrypt(credentials.Password);

            var isAValidPwd = await _userManager.CheckPasswordAsync(user, password);
            if (!isAValidPwd)
            {
                _logger.LogInformation("Invalid password for user {UserName}", credentials.UserName);

                await _userManager.AccessFailedAsync(user);
                throw new BusinessException("Invalid password!");
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            var userName = user.UserName;
            if (string.IsNullOrEmpty(userName))
            {
                throw new BusinessException("User name is missing!");
            }

            var token = await _authService.GenerateTokenAsync(userName.ToLower());

            token.Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault()!;
            token.FirstName = user.FirstName;

            InsertTokenIntoCookies(user.Id, token);

            _logger.LogInformation("Signing successful for user {UserName}", credentials.UserName);

            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while signing in the user.");
            throw new BusinessException(ex.Message);
        }
    }

    public async Task<string> SignUpAsync(NetworkCredential credentials, CreateUser createUser)
    {
        _logger.LogInformation("Creating new user...");

        try
        {
            if (!new[] { RoleName.Director, RoleName.Leader, RoleName.Employee }.Contains(createUser.Role))
                throw new BusinessException($"Invalid role: {createUser.Role}");

            if (await Exists(createUser.Email.ToLower()))
                throw new BusinessException($"User already exists!");

            var user = new User
            {
                Email = createUser.Email.ToLower(),
                UserName = createUser.Email.ToLower(),
                FirstName = createUser.FirstName,
                LastName = createUser.LastName,
                PhoneNumber = createUser.PhoneNumber,
                DocNumber = createUser.DocNumber,
                ManagerId = string.IsNullOrEmpty(createUser.ManagerId) ? null : createUser.ManagerId,
                EmailConfirmed = true,
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };

            var response = await _userManager.CreateAsync(user, credentials.Password);

            if (response.Succeeded)
            {
                // Adicionar Role ao usuário
                await _userManager.AddToRoleAsync(user, createUser.Role);
                await _userManager.SetLockoutEnabledAsync(user, false);

                var token = await _authService.GenerateTokenAsync(credentials.UserName.ToLower());
                var confirmEmailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var encodeEmailToken = Encoding.UTF8.GetBytes(confirmEmailToken);
                var validEmailToken = WebEncoders.Base64UrlEncode(encodeEmailToken);

                _logger.LogInformation("User {UserName} created successfully with Role {Role}!", credentials.UserName, createUser.Role);

                return validEmailToken;
            }

            var userErrors = response.Errors.ToList();

            throw new BusinessException(
                $"Couldn't create a new user!",
                userErrors.Select(e => new Error()
                {
                    Code = e.Code,
                    Message = e.Description
                }).ToList()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating a new user.");

            throw new BusinessException(
                $"Couldn't create a new user! {ex.InnerException?.Message}"
            );
        }
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

            _logger.LogInformation($"Signing out! || User: {userId[..8]}");

            await _authService.RemoveRefreshTokenAsync(userId);

            _httpContextAccessor.HttpContext.Response.Cookies.Delete(ACCESS_TOKEN);
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

        _httpContextAccessor.HttpContext.Response.Cookies.Append(ACCESS_TOKEN, token?.AccessToken, cookie);
        _httpContextAccessor.HttpContext.Response.Cookies.Append(REFRESH_TOKEN, token?.RefreshToken, cookie);
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

    public async Task<bool> CanCreateUserAsync(User currentUser, string requestedRole)
    {
        var currentUserRoles = await _userManager.GetRolesAsync(currentUser);

        if (currentUserRoles.Contains(RoleName.Director))
            return true;

        if (currentUserRoles.Contains(RoleName.Leader) &&
            (requestedRole == RoleName.Leader || requestedRole == RoleName.Employee))
            return true;

        if (currentUserRoles.Contains(RoleName.Employee) && requestedRole == RoleName.Employee)
            return true;

        return false;
    }

    public async Task<User> GetCurrentUserAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            throw new BusinessException("User is not authenticated.");

        return await _userManager.FindByIdAsync(userId)
               ?? throw new BusinessException("User not found.");
    }

    public async Task<bool> UpdateUserAsync(string userId, UpdateUser updateUser)
    {
        var currentUser = await GetCurrentUserAsync(userId);

        if (currentUser == null)
            throw new BusinessException($"User with ID {userId} not found!");

        if (!await CanCreateUserAsync(currentUser, updateUser.Role))
            throw new BusinessException($"You do not have permission to assign the role '{updateUser.Role}'.");

        currentUser.FirstName = updateUser.FirstName;
        currentUser.LastName = updateUser.LastName;
        currentUser.Email = updateUser.Email.ToLower();
        currentUser.NormalizedEmail = updateUser.Email.ToUpper();
        currentUser.DocNumber = updateUser.DocNumber;
        currentUser.ManagerId = updateUser.ManagerId;
        currentUser.PhoneNumber = updateUser.PhoneNumber;

        var currentRoles = await _userManager.GetRolesAsync(currentUser);
        if (!currentRoles.Contains(updateUser.Role))
        {
            await _userManager.RemoveFromRolesAsync(currentUser, currentRoles);
            await _userManager.AddToRoleAsync(currentUser, updateUser.Role);
        }

        var result = await _userManager.UpdateAsync(currentUser);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new BusinessException($"Failed to update user: {errors}");
        }

        var resetPwdToken = await _userManager.GeneratePasswordResetTokenAsync(currentUser);
        var isChanged = await _userManager.ResetPasswordAsync(currentUser, resetPwdToken, updateUser.Password);

        if (!isChanged.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new BusinessException($"Couldn't update credentials!: {errors}");
        }

        return true;
    }

    public async Task<bool> DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            throw new BusinessException($"User with ID {userId} not found!");

        var result = await _userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new BusinessException($"Failed to delete user: {errors}");
        }

        return true;

    }

    public async Task<List<UserWithManagerDto>> GetAllUsersAsync()
    {
        var users = _userManager.Users.ToList();

        var result = users.Select(user => new UserWithManagerDto
        {
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            DocNumber = user.DocNumber,
            ManagerId = user.ManagerId,
            ManagerName = user.ManagerId != null
                ? users.FirstOrDefault(manager => manager.Id == user.ManagerId)?.FirstName
                : null,
            PhoneNumbers = user.PhoneNumber?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                             .Select(phone => phone.Trim()).ToList() ?? new List<string>(),
            Role = _userManager.GetRolesAsync(user).Result.FirstOrDefault()!

        }).ToList();

        return await Task.FromResult(result);
    }
}