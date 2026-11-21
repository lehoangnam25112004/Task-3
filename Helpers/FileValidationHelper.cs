using System.IO.Compression;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;

namespace ConstructionProjectManagement.Helpers;

public static class FileValidationHelper
{
    public const long MaxFileSize = 25 * 1024 * 1024;
    public const string DefaultAllowedExtensions = ".pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx,.jpg,.jpeg,.png";

    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".jpg", ".jpeg", ".png"
    };

    public static (bool Valid, string? Error) Validate(IFormFile file, string? allowedExtensions = null)
    {
        if (file is null) return (false, "Tệp không được để trống.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = string.IsNullOrWhiteSpace(allowedExtensions)
            ? Allowed
            : allowedExtensions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => value.StartsWith('.') ? value : "." + value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!allowed.Contains(extension) || !Allowed.Contains(extension))
            return (false, "Định dạng tệp không được hỗ trợ.");

        using var stream = file.OpenReadStream();
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        long length = 0;
        int read;
        while ((read = stream.Read(chunk, 0, chunk.Length)) > 0)
        {
            length += read;
            if (length > MaxFileSize)
                return (false, "Dung lượng tệp không được vượt quá 25 MB.");
            buffer.Write(chunk, 0, read);
        }
        if (length == 0) return (false, "Tệp không được để trống.");
        if (file.Length != length)
            return (false, "Kích thước tệp tải lên không hợp lệ.");

        var contents = buffer.ToArray();
        var header = contents.AsSpan(0, Math.Min(contents.Length, 8));
        var validSignature = extension switch
        {
            ".pdf" => header.Length >= 5 && header[..5].SequenceEqual("%PDF-"u8),
            ".png" => header.Length >= 8 && header.SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".jpg" or ".jpeg" => header.Length >= 3 && header[..3].SequenceEqual(new byte[] { 0xFF, 0xD8, 0xFF }),
            ".doc" or ".xls" or ".ppt" => header.Length >= 8 &&
                header.SequenceEqual(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }),
            ".docx" or ".xlsx" or ".pptx" => header.Length >= 4 &&
                header[..4].SequenceEqual(new byte[] { 0x50, 0x4B, 0x03, 0x04 }),
            _ => false
        };
        if (!validSignature) return (false, "Nội dung tệp không khớp với phần mở rộng.");

        if (extension is ".docx" or ".xlsx" or ".pptx")
        {
            try
            {
                buffer.Position = 0;
                using var archive = new ZipArchive(buffer, ZipArchiveMode.Read, true);
                var requiredParts = extension == ".docx"
                    ? new[] { "[Content_Types].xml", "word/document.xml" }
                    : extension == ".xlsx"
                        ? new[] { "[Content_Types].xml", "xl/workbook.xml" }
                        : new[] { "[Content_Types].xml", "ppt/presentation.xml" };
                if (requiredParts.Any(part => archive.GetEntry(part) is null))
                    return (false, "Cấu trúc tệp OOXML không hợp lệ.");
            }
            catch (InvalidDataException)
            {
                return (false, "Cấu trúc tệp OOXML không hợp lệ.");
            }
        }

        return (true, null);
    }

    public static string GetContentType(string extension)
        => extension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".xls" => "application/vnd.ms-excel",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            _ => "application/octet-stream"
        };

    public static async Task<string> ComputeSha256Async(
        IFormFile file, CancellationToken cancellationToken = default)
    {
        await using var stream = file.OpenReadStream();
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
