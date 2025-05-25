using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Debug console.
/// </summary>
public class DebugConsole : MonoBehaviour
{
    [Header("Colors")]
    public Color timeStampColor;
    public Color errorColor;
    public Color warningColor;
    public Color packetColor;
    public Color generalColor;
    public Color unityColor;
    [Header("Texts")]
    public TextMeshProUGUI errorText;
    public TextMeshProUGUI warningText;
    public TextMeshProUGUI packetText;
    public TextMeshProUGUI generalText;
    public TextMeshProUGUI unityText;
    [Header("Components")]
    public Button defaultButton;
    public GameObject defaultPanel;
    public List<Button> buttons;
    public List<GameObject> panels;

    /// <summary>
    /// Awake.
    /// </summary>
    public void Awake()
    {
        Application.logMessageReceived += HandleUnityLog;

        RoseDebug.OnErrorLog += OnErrorLog;
        RoseDebug.OnGeneralLog += OnGeneralLog;
        RoseDebug.OnPacketLog += OnPacketLog;
        RoseDebug.OnUnityLog += OnUnityLog;
        RoseDebug.OnWarningLog += OnWarningLog;

        errorText.color = timeStampColor;
        warningText.color = timeStampColor;
        generalText.color = timeStampColor;
        packetText.color = timeStampColor;
        unityText.color = timeStampColor;

        PushButton(defaultButton);
        ShowPanel(defaultPanel);
    }

    public void PushButton(Button button)
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].interactable = true;
        }

        button.interactable = false;
    }

    public void ShowPanel(GameObject panel)
    {
        for (int i = 0; i < panels.Count; i++)
        {
            panels[i].SetActive(false);
        }

        panel.SetActive(true);
    }

    private void OnWarningLog(LogEntry entry)
    {
        UpdateText(warningText, warningColor, RoseDebug.warningLogs);
    }

    private void OnUnityLog(LogEntry entry)
    {
        UpdateText(unityText, unityColor, RoseDebug.unityLogs);
    }

    private void OnPacketLog(LogEntry entry)
    {
        UpdateText(packetText, packetColor, RoseDebug.packetLogs);
    }

    private void OnGeneralLog(LogEntry entry)
    {
        UpdateText(generalText, generalColor, RoseDebug.generalLogs);
    }

    private void OnErrorLog(LogEntry entry)
    {
        UpdateText(errorText, errorColor, RoseDebug.errorLogs);
    }

    /// <summary>
    /// Update the text.
    /// </summary>
    /// <param name="textMesh">Text mesh.</param>
    /// <param name="logs">Logs.</param>
    private void UpdateText(TextMeshProUGUI textMesh, Color color, List<LogEntry> logs)
    {
        StringBuilder sb = new();

        foreach (var log in logs)
        {
            sb.AppendLine(log.ToString(color));
        }

        textMesh.text = sb.ToString();
    }

    /// <summary>
    /// Hook the Unity logs to display them in-game.
    /// </summary>
    /// <param name="logString">Log string.</param>
    /// <param name="stackTrace">Stack trace.</param>
    /// <param name="type">Log's type.</param>
    private void HandleUnityLog(string logString, string stackTrace, LogType type)
    {
        RoseDebug.LogUnity(logString);
    }
}

/// <summary>
/// Rose debug.
/// </summary>
public static class RoseDebug
{
    public static int maxLogCount = 50;

    public static List<LogEntry> errorLogs = new List<LogEntry>();
    public static List<LogEntry> warningLogs = new List<LogEntry>();
    public static List<LogEntry> packetLogs = new List<LogEntry>();
    public static List<LogEntry> generalLogs = new List<LogEntry>();
    public static List<LogEntry> unityLogs = new List<LogEntry>();

    public static event Action<LogEntry> OnErrorLog;
    public static event Action<LogEntry> OnWarningLog;
    public static event Action<LogEntry> OnPacketLog;
    public static event Action<LogEntry> OnGeneralLog;
    public static event Action<LogEntry> OnUnityLog;

    /// <summary>
    /// Add a log.
    /// </summary>
    /// <param name="list">List.</param>
    /// <param name="message">Message.</param>
    /// <param name="logEvent">Log event.</param>
    private static void AddLog(List<LogEntry> list, string message, Action<LogEntry> logEvent)
    {
        if (list.Count >= maxLogCount)
            list.RemoveAt(0);

        var entry = new LogEntry(DateTime.Now, message);
        list.Add(entry);
        logEvent?.Invoke(entry);
    }

    public static void LogError(string message) => AddLog(errorLogs, message, OnErrorLog);
    public static void LogWarning(string message) => AddLog(warningLogs, message, OnWarningLog);
    public static void LogPacket(string message) => AddLog(packetLogs, message, OnPacketLog);
    public static void Log(string message) => AddLog(generalLogs, message, OnGeneralLog);
    public static void LogUnity(string message) => AddLog(unityLogs, message, OnUnityLog);
}

/// <summary>
/// Log entry.
/// </summary>
public class LogEntry
{
    public DateTime Timestamp;
    public string Message;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="timestamp">Timestamp.</param>
    /// <param name="message">Message.</param>
    public LogEntry(DateTime timestamp, string message)
    {
        Timestamp = timestamp;
        Message = message;
    }

    /// <summary>
    /// To string override.
    /// </summary>
    /// <returns></returns>
    public string ToString(Color logColor) => $"[{Timestamp:HH:mm}] {ChatController.ColorizeText(Message, logColor)}";
}