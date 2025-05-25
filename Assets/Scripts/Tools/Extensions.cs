using UnityEngine;

/// <summary>
/// Extensions.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Get the enum value's name in a string name.
    /// </summary>
    /// <typeparam name="T">Type of enum.</typeparam>
    /// <param name="enumValue">Enum instance.</param>
    /// <returns>Name in string format.</returns>
    public static string GetName<T>(this T enumValue) where T : System.Enum
    {
        return System.Enum.GetName(typeof(T), enumValue);
    }
}
