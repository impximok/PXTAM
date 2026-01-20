using Microsoft.AspNetCore.Http;

namespace SnomiAssignmentReal.Helpers;

public static class FileHelper
{
    public static string ValidateImage(IFormFile file)
    {
        if (file.Length > 2 * 1024 * 1024) // 2MB limit
            return "File size must be less than 2MB.";

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(ext))
            return "Only .jpg, .jpeg, .png, or .gif files are allowed.";

        return "";
    }

    public static string SaveFile(IFormFile file, string folder, string rootPath)
    {
        // Full path where the file should be saved
        string dirPath = Path.Combine(rootPath, folder);

        // Create the directory if it doesn’t exist
        Directory.CreateDirectory(dirPath);

        // Create a unique file name
        string fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        string filePath = Path.Combine(dirPath, fileName);

        // Save the file
        using var stream = new FileStream(filePath, FileMode.Create);
        file.CopyTo(stream);

        return fileName;
    }


    public static void DeleteFile(string? relativePath, string rootPath)
    {
        if (string.IsNullOrEmpty(relativePath)) return;

        string fullPath = Path.Combine(rootPath, relativePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
        if (File.Exists(fullPath)) File.Delete(fullPath);
    }

    /// Parse a data URL like "data:image/jpeg;base64,AAAA..."
    public static (byte[]? bytes, string extension, string? error) ParseDataUrl(string? dataUrl, int maxBytes = 2 * 1024 * 1024)
    {
        if (string.IsNullOrWhiteSpace(dataUrl))
            return (null, ".jpg", "No image data.");

        var parts = dataUrl.Split(',', 2);
        if (parts.Length != 2 || !parts[0].Contains("base64", StringComparison.OrdinalIgnoreCase))
            return (null, ".jpg", "Invalid image data.");

        var header = parts[0]; // e.g. data:image/png;base64
        var b64 = parts[1];

        var ext = ".jpg";
        if (header.Contains("image/png", StringComparison.OrdinalIgnoreCase)) ext = ".png";
        else if (headerContains(header, "image/jpeg") || headerContains(header, "image/jpg")) ext = ".jpg";
        else if (headerContains(header, "image/webp")) ext = ".webp";
        else if (headerContains(header, "image/gif")) ext = ".gif";

        try
        {
            var bytes = Convert.FromBase64String(b64);
            if (bytes.Length > maxBytes) return (null, ext, $"File size must be less than {maxBytes / (1024 * 1024)}MB.");
            return (bytes, ext, null);
        }
        catch
        {
            return (null, ext, "Invalid Base64 image data.");
        }

        static bool headerContains(string h, string needle) =>
            h?.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// Save raw bytes as an image file into a webroot subfolder.
    public static string SaveBytes(byte[] bytes, string folder, string rootPath, string extension = ".jpg")
    {
        var dirPath = Path.Combine(rootPath, folder.Replace("/", Path.DirectorySeparatorChar.ToString()));
        Directory.CreateDirectory(dirPath);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(dirPath, fileName);
        File.WriteAllBytes(filePath, bytes);

        return fileName;
    }
}
