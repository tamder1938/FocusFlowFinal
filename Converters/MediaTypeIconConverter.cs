using Avalonia.Data.Converters;
using FocusFlowFinal.Models.Media;
using System;
using System.Globalization;

namespace FocusFlowFinal.Converters;

public class MediaTypeIconConverter : IValueConverter
{
    public static readonly MediaTypeIconConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            MediaType.Movie  => "🎬",
            MediaType.Series => "📺",
            MediaType.Anime  => "🍿",
            MediaType.Book   => "📖",
            MediaType.Manga  => "📚",
            _                => "🎬"
        };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
