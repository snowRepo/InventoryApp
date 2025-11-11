using System;
namespace InventoryApp.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;

    public byte[] PasswordHash { get; set; } = []; 
    public byte[] PasswordSalt { get; set; } = [];

    public byte[] MasterPinHash { get; set; } = [];
    public byte[] MasterPinSalt { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
