using Magnar.AI.Application.Configuration;
using Magnar.AI.Application.Helpers;
using Magnar.AI.Infrastructure.Persistence.Contexts;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace Magnar.AI.Infrastructure.Repositories;

public class ApiKeyRepository : BaseRepository<ApiKey>, IApiKeyRepository
{
    #region Members
    private readonly MagnarAIDbContext context;
    private readonly ApiKeysConfiguration configuration;
    #endregion

    #region Constructor
    public ApiKeyRepository(MagnarAIDbContext context, IOptions<ApiKeysConfiguration> configuration) : base(context)
    {
        this.context = context;
        this.configuration = configuration.Value;
    }
    #endregion

    /// <summary>
    /// Creates a new API key for a given user and tenant.
    /// 
    /// - Generates a public ID and a secret part.
    /// - Computes a secure HMAC-SHA256 hash of both (with server secret).
    /// - Persists the <see cref="ApiKey"/> entity in the database.
    /// - Returns both the full plain key string (for the user) and the stored entity.
    /// 
    /// The returned <c>plainKey</c> has the format: "ak_{publicId}.{secretPart}" (secret part is Base64Url encoded).
    /// Only the caller should ever see the plain key; only the hash is stored in DB.
    /// </summary>
    public async Task<(string plainKey, ApiKey entity)> CreateAsync(int ownerUserId, string tenantId, IEnumerable<string> scopes, TimeSpan? lifetime, string name, string metadataJson)
    {
        var publicId = GeneratePublicId();
        var secretPart = GenerateSecretPart(32);

        var hash = ApiKeyHashing.ComputeHash(ServerSecret, publicId, secretPart);

        var key = new ApiKey
        {
            PublicId = publicId,
            Hash = hash,
            OwnerUserId = ownerUserId,
            TenantId = tenantId,
            ScopesCsv = string.Join(",", scopes),
            ExpiresUtc = lifetime.HasValue ? DateTime.UtcNow.Add(lifetime.Value) : null,
            Name = name,
            MetadataJson = metadataJson
        };

        context.Set<ApiKey>().Add(key);
        await context.SaveChangesAsync();

        var full = $"ak_{publicId}.{ApiKeyHashing.Base64UrlEncode(Encoding.UTF8.GetBytes(secretPart))}";

        return (full, key);
    }

    /// <summary>
    /// Validates a provided full API key string.
    /// 
    /// - Splits into publicId and secretPart.
    /// - Decodes and recomputes the hash.
    /// - Looks up the stored <see cref="ApiKey"/> entity by publicId.
    /// - Ensures:
    ///   * Key exists
    ///   * Key is not expired or revoked
    ///   * Hash matches using constant-time comparison
    /// 
    /// If valid, returns the <see cref="ApiKey"/> entity (optionally updates last-used timestamp).
    /// Otherwise, returns null.
    /// </summary>
    public async Task<ApiKey> ValidateAsync(string fullKey, bool updateLastUsed = true)
    {
        var parts = fullKey.Split('.', 2);

        if (parts.Length != 2)
        {
            return null;
        }

        var publicId = parts[0];
        string secretPart;

        try
        {
            secretPart = Encoding.UTF8.GetString(ApiKeyHashing.Base64UrlDecode(parts[1]));
        }
        catch
        {
            return null;
        }

        var calcHash = ApiKeyHashing.ComputeHash(ServerSecret, publicId, secretPart);

        var entity = await context.Set<ApiKey>().FirstOrDefaultAsync(k => k.PublicId == publicId);


        if (entity is null || !entity.IsActive(DateTime.UtcNow) || !ApiKeyHashing.ConstantTimeEquals(calcHash, entity.Hash))
        {
            return null;
        }

        if (updateLastUsed)
        {
            entity.LastUsedUtc = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }

        return entity;
    }

    /// <summary>
    /// Revokes an API key by its publicId for a given user and tenant.
    /// 
    /// - Finds the key in the database.
    /// - If not found or already revoked, returns false.
    /// - Otherwise, sets <c>RevokedUtc</c> to the current UTC time and saves.
    /// 
    /// Returns true if the key was successfully revoked.
    /// </summary>
    public async Task<bool> RevokeAsync(string publicId, int ownerUserId, string tenantId, CancellationToken cancellationToken)
    {
        var key = await context.Set<ApiKey>().FirstOrDefaultAsync(k => k.PublicId == publicId && k.OwnerUserId == ownerUserId && k.TenantId == tenantId, cancellationToken: cancellationToken);

        if (key is null || key.RevokedUtc is not null)
        {
            return false;
        }

        key.RevokedUtc = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }

    #region Private Methods

    private string ServerSecret => configuration.HashSecret ?? throw new InvalidOperationException("ApiKeys:HashSecret missing.");

    /// <summary>
    /// Generates a cryptographically secure public identifier.
    /// This creates 10 random bytes (~80 bits of entropy) and encodes them in Base32,
    /// producing a short, human-readable, URL-safe string.
    /// Useful as a public-facing ID or API key prefix.
    /// </summary>
    private static string GeneratePublicId()
    {
        var raw = RandomNumberGenerator.GetBytes(10);
        return ApiKeyHashing.Base32(raw);
    }

    /// <summary>
    /// Generates the secret portion of an identifier or API key.
    /// Creates a cryptographically secure random byte array of the given size
    /// and encodes it as a Base64 string. 
    /// Useful as a private secret or token that should be kept confidential.
    /// </summary>
    private static string GenerateSecretPart(int sizeBytes)
    {
        var bytes = RandomNumberGenerator.GetBytes(sizeBytes);
        return Convert.ToBase64String(bytes);
    }

    #endregion
}
