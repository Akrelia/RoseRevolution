using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

/// <summary>
/// GM Panel.
/// </summary>
public class GMPanel : BaseWindow
{
    public Button defaultButton;
    public GameObject defaultPanel;
    public List<Button> buttons;
    public List<GameObject> panels;

    /// <summary>
    /// Start.
    /// </summary>
    private void Start()
    {
        PushButton(defaultButton);
        ShowPanel(defaultPanel);
    }

    /// <summary>
    /// Push button.
    /// </summary>
    /// <param name="button">Button.</param>
    public void PushButton(Button button)
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].interactable = true;
        }

        button.interactable = false;
    }

    /// <summary>
    /// Show panel.
    /// </summary>
    /// <param name="panel">Panel.</param>
    public void ShowPanel(GameObject panel)
    {
        for (int i = 0; i < panels.Count; i++)
        {
            panels[i].SetActive(false);
        }

        panel.SetActive(true);
    }
}
