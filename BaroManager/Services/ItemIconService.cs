using System.Collections.Concurrent;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BaroManager.Models;
using DrawingIcon = System.Drawing.Icon;

namespace BaroManager.Services;

public static class ItemIconService
{
    private static readonly ConcurrentDictionary<string, ImageSource> IconCache =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly Lazy<ImageSource> FallbackIcon = new(CreateFallbackIcon);

    public static ImageSource GetIcon(ManagedItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.IconPath) && File.Exists(item.IconPath))
        {
            var customIcon = GetOrLoad($"custom:{item.IconPath}", () => LoadBitmap(item.IconPath));

            if (customIcon is not null)
                return customIcon;
        }

        if (File.Exists(item.Path))
        {
            var associatedIcon = GetOrLoad($"associated:{item.Path}", () => LoadAssociatedIcon(item.Path));

            if (associatedIcon is not null)
                return associatedIcon;
        }

        var bundledIconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.png");

        if (File.Exists(bundledIconPath))
        {
            var bundledIcon = GetOrLoad($"bundled:{bundledIconPath}", () => LoadBitmap(bundledIconPath));

            if (bundledIcon is not null)
                return bundledIcon;
        }

        return FallbackIcon.Value;
    }

    private static ImageSource? GetOrLoad(string key, Func<ImageSource?> loader)
    {
        if (IconCache.TryGetValue(key, out var cached))
            return cached;

        var loaded = loader();

        if (loaded is not null)
            IconCache.TryAdd(key, loaded);

        return loaded;
    }

    private static ImageSource? LoadBitmap(string path)
    {
        try
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = 128;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();

            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    private static ImageSource? LoadAssociatedIcon(string path)
    {
        try
        {
            using var icon = DrawingIcon.ExtractAssociatedIcon(path);

            if (icon is null)
                return null;

            var source = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(128, 128));

            source.Freeze();
            return source;
        }
        catch
        {
            return null;
        }
    }

    private static ImageSource CreateFallbackIcon()
    {
        var background = new GeometryDrawing(
            new SolidColorBrush(System.Windows.Media.Color.FromRgb(124, 92, 255)),
            null,
            new RectangleGeometry(new Rect(2, 2, 60, 60), 12, 12));

        var foreground = new GeometryDrawing(
            new SolidColorBrush(Colors.White),
            null,
            Geometry.Parse("M18,16 H42 A6,6 0 0 1 48,22 V42 A6,6 0 0 1 42,48 H18 A6,6 0 0 1 12,42 V22 A6,6 0 0 1 18,16 M22,25 H38 V29 H22 Z M22,35 H34 V39 H22 Z"));

        var drawing = new DrawingGroup();
        drawing.Children.Add(background);
        drawing.Children.Add(foreground);
        drawing.Freeze();

        var image = new DrawingImage(drawing);
        image.Freeze();
        return image;
    }
}
