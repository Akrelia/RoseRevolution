using RevolutionShared.Packets;
using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class PacketEvent : Attribute
{
    public int Value { get; set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="value">Value.</param>
    public PacketEvent(int value)
    {
        Value = value;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="value">Value.</param>
    public PacketEvent(ServerCommands value)
    {
        Value = Convert.ToInt32(value);
    }
}
