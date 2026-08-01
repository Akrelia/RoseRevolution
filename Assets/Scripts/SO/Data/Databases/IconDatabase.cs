using System;
using System.Collections.Generic;
using UnityEngine;

public class IconDatabase : ScriptableObject
{
    public List<IconAtlasData> entries;

    public Sprite GetIcon(int iconId, int iconSize = 40)
    {
        if (entries.Count == 0)
        {
            return null;
        }

        int index = -1;

        for (int i = 0; i < entries.Count; i++)
        {
            for (int y = 0; y < 13; y++)
            {
                for (int x = 0; x < 13; x++)
                {
                    index++;

                    if (x != 12 && y != 12)
                    {
                        if (index == iconId)
                        {
                            int spriteY = entries[i].texture.height - (y + 1) * iconSize;

                            return Sprite.Create(entries[i].texture, new Rect(x * iconSize, spriteY, iconSize, iconSize), new Vector2(0.5f, 0.5f));
                        }
                    }
                }
            }
        }

        int defaultSpriteY = entries[0].texture.height - iconSize;

        return Sprite.Create(entries[0].texture, new Rect(0, defaultSpriteY, iconSize, iconSize), new Vector2(0.5f, 0.5f));
    }
}

[Serializable]
public class IconAtlasData
{
    public string name;
    public Texture2D texture;
}