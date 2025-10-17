namespace BarberNetBooking.Helpers;

public static class DateExtensions
{
    /// <summary>
    /// Formata DateOnly para o padrão brasileiro (dd/MM/yyyy)
    /// </summary>
    public static string ToBrazilianDate(this DateOnly date)
    {
        return date.ToString("dd/MM/yyyy");
    }

    /// <summary>
    /// Formata DateTime para o padrão brasileiro (dd/MM/yyyy)
    /// </summary>
    public static string ToBrazilianDate(this DateTime date)
    {
        return date.ToString("dd/MM/yyyy");
    }

    /// <summary>
    /// Formata DateOnly para input HTML5 date (yyyy-MM-dd)
    /// </summary>
    public static string ToHtmlDate(this DateOnly date)
    {
        return date.ToString("yyyy-MM-dd");
    }

    /// <summary>
    /// Formata DateTime para input HTML5 date (yyyy-MM-dd)
    /// </summary>
    public static string ToHtmlDate(this DateTime date)
    {
        return date.ToString("yyyy-MM-dd");
    }

    /// <summary>
    /// Formata TimeOnly para exibição (HH:mm)
    /// </summary>
    public static string ToDisplayTime(this TimeOnly time)
    {
        return time.ToString("HH:mm");
    }

    /// <summary>
    /// Formata DateTime para exibição com hora (dd/MM/yyyy HH:mm)
    /// </summary>
    public static string ToBrazilianDateTime(this DateTime date)
    {
        return date.ToString("dd/MM/yyyy HH:mm");
    }
}