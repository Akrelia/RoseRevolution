using RevolutionShared.Networking.Packets;
using RevolutionShared.Packets;
using System;
using TMPro;
using UnityEngine;

/// <summary>
/// GUI Controller.
/// </summary>
public class GUIController : MonoBehaviour
{
    [Header("Components")]
    public ChatController chatController;
    public CharacterPreview characterPreview;
    public DebugConsole debugConsole;
}
