using UnityEngine;

/// <summary>
/// Interface for Rose file formats.
/// </summary>
public interface IRoseFileFormat
{
    public string FormatName { get; }

    /// <summary>
    /// Load the file format.
    /// </summary>
    /// <param name="path">Path.</param>
    public abstract void Load(string path);
}
