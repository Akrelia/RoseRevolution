using RevolutionShared.Data;
using Unity.VisualScripting;
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

    /// <summary>
    /// To Vector3.
    /// </summary>
    /// <param name="position">Current world position.</param>
    /// <returns>Position.</returns>
    public static Vector3 ToVector3(this WorldPosition position)
    {
        return new Vector3(position.x, position.y, position.z);
    }

    /// <summary>
    /// To World position.
    /// </summary>
    /// <param name="position">Current vector3 position.</param>
    /// <returns>Position.</returns>
    public static WorldPosition ToWorldPosition(this Vector3 position)
    {
        return new WorldPosition(position.x, position.y, position.z);
    }
}
