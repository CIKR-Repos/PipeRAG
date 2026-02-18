using PipeRAG.Core.Interfaces;

namespace PipeRAG.Infrastructure.Services;

/// <summary>
/// Stores files on the local filesystem under an uploads/ directory.
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;

    public LocalFileStorageService(string basePath = "uploads")
    {
        _basePath = basePath;
    }

    /// <inheritdoc />
    public async Task<string> SaveFileAsync(Guid projectId, Guid documentId, string fileName, Stream content, CancellationToken ct = default)
    {
        var relativePath = Path.Combine(projectId.ToString(), documentId.ToString(), fileName);
        var fullPath = Path.Combine(_basePath, relativePath);
        var dir = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(dir);

        await using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        await content.CopyToAsync(fs, ct);

        return relativePath;
    }

    /// <inheritdoc />
    public Task<Stream> GetFileAsync(string storagePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, storagePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("File not found", fullPath);

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return Task.FromResult(stream);
    }

    /// <inheritdoc />
    public Task DeleteFileAsync(string storagePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, storagePath);
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        // Clean up empty parent directories
        var dir = Path.GetDirectoryName(fullPath);
        while (dir != null && dir != _basePath && Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
        {
            Directory.Delete(dir);
            dir = Path.GetDirectoryName(dir);
        }

        return Task.CompletedTask;
    }
}
