using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityRose;
using UnityRose.Formats;

/// <summary>
/// Asset manager.
/// </summary>
public class AssetManager
{
    public Dictionary<int, Sprite> icons;
    public IDDatabase itemTypes;

    private static AssetManager instance;

    /// <summary>
    /// Constructor.
    /// </summary>
    private AssetManager()
    {
    }

    /// <summary>
    /// Load everythings.
    /// </summary>
    public void Load()
    {
      //  LoadIcons();
      //  LoadItemTypes();
    }

    public string GetItemCategory(int id)
    {
        if (itemTypes.ids.ContainsKey(id))
        {
            return itemTypes.ids[id].dataName;
        }

        return null;
    }

    public Sprite GetItemIcon(int id)
    {
        if (icons.ContainsKey(id))
        {
            return icons[id];
        }

        return icons[0];
    }

    public void LoadItemTypes()
    {
        try
        {
            var handle = Addressables.LoadAssetAsync<AddressableIndex>("index-ids");

            handle.WaitForCompletion();

            var index = handle.Result;

            if (index.groups.Count == 0 || index.groups[0].addresses.Count == 0)
            {
                Debug.LogWarning("Index is empty.");

                return;
            }

            var address = index.groups[0].addresses[0].address;

            var handle2 = Addressables.LoadAssetAsync<IDDatabase>(address);

            handle2.WaitForCompletion();

            itemTypes = handle2.Result;

            Debug.Log("Types loaded");
        }
        catch (Exception ex)
        {
            Debug.Log("Error loading types: " + ex.Message);
        }
    }

    /// <summary>
    /// Load the icons.
    /// </summary>
    private void LoadIcons()
    {
        icons = new Dictionary<int, Sprite>();

        var size = 40;
        IconsDatabase db = Resources.Load<IconsDatabase>("IconsDatabase");

        var textures = db.icons;

        int index = -1;

        for (int i = 0; i < textures.Count; i++)
        {
            for (int y = 0; y < 13; y++)
            {
                for (int x = 0; x < 13; x++)
                {
                    index++;

                    if (x != 12 && y != 12)
                    {
                        int spriteY = 512 - (y + 1) * size; // We must inverse because Sprite.Create use bottom-left as origin but in ROSE its top-left

                        var icon = Sprite.Create(textures[i], new Rect(x * size, spriteY, size, size), new Vector2(0.5F, 0.5F));

                        icons.Add(index, icon);
                    }
                }
            }
        }

        Debug.Log($"{icons.Count} Icons file(s) loaded");
    }

    /// <summary>
    /// Get or set the instance.
    /// </summary>
    public static AssetManager Instance => instance ??= new AssetManager();
}