namespace PipeRAG.Core.Interfaces;

/// <summary>
/// Abstraction for file storage operations.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Saves a file to storage and returns the storage path.
    /// </summary>
    Task<string> SaveFileAsync(Guid projectId, Guid documentId, string fileName, Stream content, CancellationToken ct = default);

    /// <summary>
    /// Gets a file stream from storage.
    /// </summary>
    Task<Stream> GetFileAsync(string storagePath, CancellationToken ct = default);

    /// <summary>
    /// Deletes a file from storage.
    /// </summary>
    Task DeleteFileAsync(string storagePath, CancellationToken ct = default);
}
