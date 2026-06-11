using Microsoft.Windows.ApplicationModel.Resources;

namespace WinMensa;

internal static class Strings
{
    private static readonly ResourceLoader _loader = new();

    public static string Get(string key) => _loader.GetString(key);
    public static string Format(string key, params object[] args) => string.Format(Get(key), args);
}
