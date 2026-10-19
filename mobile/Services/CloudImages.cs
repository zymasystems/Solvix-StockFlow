using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;

namespace Solvix.Sims.Services;
/// <summary>Retain the original pickers while making their images small enough for the backend profile/display records.</summary>
public static class CloudImages
{
    public static string Encode(byte[] bytes)
    {
        var png = bytes.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var jpeg = bytes.AsSpan().StartsWith(new byte[] { 255, 216, 255 });
        if (bytes.Length <= 180000 && (png || jpeg))
            return Url(bytes, png);
        using var stream = new MemoryStream(bytes);
        using var image = PlatformImage.FromStream(stream) ?? throw new InvalidOperationException("This image cannot be decoded.");
        foreach (var size in new[]
        {
            1024,
            768,
            512,
            256,
            128
        }

        )
        {
            var resized = image.Downsize(size, false);
            try
            {
                var data = resized.AsBytes(png ? ImageFormat.Png : ImageFormat.Jpeg, .8f);
                if (data.Length <= 180000)
                    return Url(data, png);
            }
            finally
            {
                if (!ReferenceEquals(image, resized))
                    resized.Dispose();
            }
        }

        throw new InvalidOperationException("Choose a smaller image.");
    }

    private static string Url(byte[] bytes, bool png) => $"data:image/{(png ? "png" : "jpeg")};base64,{Convert.ToBase64String(bytes)}";
}
