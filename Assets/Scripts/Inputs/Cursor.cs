using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Game Cursor.
/// </summary>
public class GameCursor : MonoBehaviour
{
    public Texture2D cursorImage;

    private void Awake()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
    }
}
