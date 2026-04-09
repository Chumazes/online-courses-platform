using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OnlineCourses.Desktop.Infrastructure;

public static class ImageSourceFactory
{
    public static ImageSource? Create(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return null;
        }

        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
        {
            return null;
        }

        try
        {
            var image = new BitmapImage(uri);
            image.Freeze();
            return image;
        }
        catch
        {
            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = uri;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch
            {
                return null;
            }
        }
    }
}
