using Goal.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Goal.Infrastructure.Storage;

/// <summary>
/// Dev-friendly file storage that writes uploads under wwwroot/uploads and returns a relative URL.
/// Swap for S3/MinIO/Azure Blob in production by providing another IFileStorage implementation.
/// </summary>
public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;
    private readonly string _publicBase;

    public LocalFileStorage(IConfiguration config)
    {
        _root = config["Storage:LocalRoot"] ?? Path.Combine(AppContext.BaseDirectory, "wwwroot", "uploads");
        _publicBase = config["Storage:PublicBaseUrl"] ?? "/uploads";
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        var safeName = $"{Guid.CreateVersion7()}{Path.GetExtension(fileName)}";
        var fullPath = Path.Combine(_root, safeName);
        await using var fs = File.Create(fullPath);
        await content.CopyToAsync(fs, ct);
        return $"{_publicBase}/{safeName}";
    }
}
