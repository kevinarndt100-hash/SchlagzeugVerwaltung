using System.Windows.Media;

namespace SchlagzeugVerwaltung.Theme;

public static class DarkTheme
{
    public static Color Background = (Color)ColorConverter.ConvertFromString("#1E1E1E");
    public static Color CardBackground = (Color)ColorConverter.ConvertFromString("#2D2D2D");
    public static Color CardHover = (Color)ColorConverter.ConvertFromString("#3C3C3C");
    public static Color Accent = (Color)ColorConverter.ConvertFromString("#007ACC");
    public static Color AccentHover = (Color)ColorConverter.ConvertFromString("#1C97EA");
    public static Color Text = (Color)ColorConverter.ConvertFromString("#FFFFFF");
    public static Color TextSecondary = (Color)ColorConverter.ConvertFromString("#AAAAAA");
    public static Color Border = (Color)ColorConverter.ConvertFromString("#3F3F3F");
    public static Color Danger = (Color)ColorConverter.ConvertFromString("#E81123");
    public static Color Success = (Color)ColorConverter.ConvertFromString("#16C60C");

    public static SolidColorBrush BackgroundBrush = new SolidColorBrush(Background);
    public static SolidColorBrush CardBackgroundBrush = new SolidColorBrush(CardBackground);
    public static SolidColorBrush CardHoverBrush = new SolidColorBrush(CardHover);
    public static SolidColorBrush AccentBrush = new SolidColorBrush(Accent);
    public static SolidColorBrush AccentHoverBrush = new SolidColorBrush(AccentHover);
    public static SolidColorBrush TextBrush = new SolidColorBrush(Text);
    public static SolidColorBrush TextSecondaryBrush = new SolidColorBrush(TextSecondary);
    public static SolidColorBrush BorderBrush = new SolidColorBrush(Border);
    public static SolidColorBrush DangerBrush = new SolidColorBrush(Danger);
    public static SolidColorBrush SuccessBrush = new SolidColorBrush(Success);
}