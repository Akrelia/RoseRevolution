using RevolutionShared.Networking.Packets;
using RevolutionShared.Packets;
using System;
using System.Collections.Generic;
using UnityEngine;

public static class NetworkEvents
{
    private static readonly Dictionary<ServerCommands, List<Action<Client, PacketIn>>> events = new();

    public static void RegisterEvent(ServerCommands command)
    {
        if (!events.ContainsKey(command))
            events[command] = new List<Action<Client, PacketIn>>();
    }

    public static void Subscribe(ServerCommands command, Action<Client, PacketIn> handler)
    {
        if (!events.ContainsKey(command))
            events[command] = new List<Action<Client, PacketIn>>();

        if (!events[command].Contains(handler))
            events[command].Add(handler);
    }

    public static void Unsubscribe(ServerCommands command, Action<Client, PacketIn> handler)
    {
        if (events.TryGetValue(command, out var handlers))
        {
            handlers.Remove(handler);
            if (handlers.Count == 0)
                events.Remove(command);
        }
    }

    public static void Raise(ServerCommands command, Client client, PacketIn packet)
    {
        if (events.TryGetValue(command, out var handlers))
        {
            foreach (var handler in handlers)
                handler(client, packet);
        }
    }
}
