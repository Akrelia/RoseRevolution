using System;
using TMPro;
using UnityEngine;

/// <summary>
/// GUI Controller.
/// </summary>
public class GUIController : MonoBehaviour
{
    [Header("Components")]
    public TextMeshProUGUI debugText;

    public void Awake()
    {
        Application.logMessageReceived += HandleLog;

    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        debugText.text += ($"{logString}{Environment.NewLine}");
    }
}
