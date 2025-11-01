using LiaXP.Domain.Common;

namespace LiaXP.Domain.Entities;

/// <summary>
/// Company aggregate root
/// Represents a tenant in the multi-tenant system
/// </summary>
public class Company : BaseEntity
{
    /// <summary>
    /// Business identifier (natural key)
    /// Used for external APIs, URL slugs, and user-friendly identification
    /// Must be unique across all companies
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>
    /// Company display name
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Optional company description
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Company active status
    /// </summary>
    public bool IsActive { get; private set; } = true;

    // Navigation properties
    public virtual ICollection<Store> Stores { get; set; } = new List<Store>();
    public virtual ICollection<Seller> Sellers { get; set; } = new List<Seller>();
    public virtual ICollection<User> Users { get; set; } = new List<User>();

    // EF Core constructor
    private Company() { }

    /// <summary>
    /// Create a new company
    /// </summary>
    /// <param name="code">Unique business code (e.g., "ACME", "CONTOSO")</param>
    /// <param name="name">Company name</param>
    /// <param name="description">Optional description</param>
    public Company(string code, string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Company code cannot be empty", nameof(code));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Company name cannot be empty", nameof(name));

        // Normalize code to uppercase and remove spaces
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Description = description?.Trim();
        IsActive = true;
    }

    /// <summary>
    /// Update company information
    /// </summary>
    public void Update(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Company name cannot be empty", nameof(name));

        Name = name.Trim();
        Description = description?.Trim();
    }

    /// <summary>
    /// Activate the company
    /// </summary>
    public void Activate() => IsActive = true;

    /// <summary>
    /// Deactivate the company
    /// </summary>
    public void Deactivate() => IsActive = false;
}