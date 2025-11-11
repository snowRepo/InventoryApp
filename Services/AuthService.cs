using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System;
using InventoryApp.Data;
using InventoryApp.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Services;

public interface IAuthService
{
    Task<(bool ok, string? error)> RegisterAsync(string username, string password, string masterPin);
    Task<(bool ok, string? error, User? user)> LoginAsync(string username, string password);
    Task<(bool ok, string? error)> VerifyMasterPinAsync(string username, string pin);
    Task<(bool ok, string? error)> ResetPasswordAsync(string username, string newPassword);
}

public sealed class AuthService : IAuthService
{
    public async Task<(bool ok, string? error)> RegisterAsync(string username, string password, string masterPin)
    {
        username = username.Trim();
        if (string.IsNullOrWhiteSpace(username)) return (false, "Username is required");
        if (!ValidatePassword(password, out var pwdErr)) return (false, pwdErr);
        if (!ValidatePin(masterPin, out var pinErr)) return (false, pinErr);

        using var db = new AppDbContext();
        if (await db.Users.AnyAsync(u => u.Username == username))
            return (false, "Username already exists");

        var (pwdHash, pwdSalt) = HashSecret(password);
        var (pinHash, pinSalt) = HashSecret(masterPin);

        var user = new User
        {
            Username = username,
            PasswordHash = pwdHash,
            PasswordSalt = pwdSalt,
            MasterPinHash = pinHash,
            MasterPinSalt = pinSalt,
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool ok, string? error, User? user)> LoginAsync(string username, string password)
    {
        username = username.Trim();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            return (false, "Username and password are required", null);

        using var db = new AppDbContext();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user is null) return (false, "Invalid credentials", null);

        if (!VerifySecret(password, user.PasswordHash, user.PasswordSalt))
            return (false, "Invalid credentials", null);

        return (true, null, user);
    }

    public async Task<(bool ok, string? error)> VerifyMasterPinAsync(string username, string pin)
    {
        username = username.Trim();
        using var db = new AppDbContext();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user is null) return (false, "User not found");
        var ok = VerifySecret(pin, user.MasterPinHash, user.MasterPinSalt);
        return ok ? (true, null) : (false, "Invalid PIN");
    }

    public async Task<(bool ok, string? error)> ResetPasswordAsync(string username, string newPassword)
    {
        if (!ValidatePassword(newPassword, out var err)) return (false, err);
        username = username.Trim();
        using var db = new AppDbContext();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user is null) return (false, "User not found");
        var (hash, salt) = HashSecret(newPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        await db.SaveChangesAsync();
        return (true, null);
    }

    private static (byte[] hash, byte[] salt) HashSecret(string secret)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        using var pbkdf2 = new Rfc2898DeriveBytes(secret, salt, 100_000, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);
        return (hash, salt);
    }

    private static bool VerifySecret(string secret, byte[] expectedHash, byte[] salt)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(secret, salt, 100_000, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);
        return CryptographicOperations.FixedTimeEquals(hash, expectedHash);
    }

    private static bool ValidatePassword(string password, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            error = "Password must be at least 6 characters";
            return false;
        }
        return true;
    }

    private static bool ValidatePin(string pin, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(pin) || pin.Length < 4 || pin.Length > 8 || !pin.All(char.IsDigit))
        {
            error = "PIN must be 4-8 digits";
            return false;
        }
        return true;
    }
}
