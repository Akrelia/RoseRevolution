using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;
using System.Collections;
using static AddressableIndex;
using System.Linq;
using RevolutionShared.Utils;

/// <summary>
/// GM Panel.
/// </summary>
public class GMPanel : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject iconPrefab;
    [Header("Components")]
    public TMP_Dropdown dropdownSTB;
    public TMP_Dropdown dropdownTypes;
    public RectTransform itemContainer;

    List<DataID> itemDatas = new List<DataID>();
    List<string> stbData = new List<string>();
    int lastSTBIndex = 0;
    int lastCategoryIndex = 0;
    AddressableIndex addressable;

    /// <summary>
    /// Start.
    /// </summary>
    private void Start()
    {
        dropdownSTB.ClearOptions();
        dropdownTypes.ClearOptions();

        InitializeData();
    }

    /// <summary>
    /// Load STBs.
    /// </summary>
    private void InitializeData()
    {
        Addressables.LoadAssetAsync<AddressableIndex>("index-ids").Completed += IDLoaded;
    }

    private void STBLoaded(AsyncOperationHandle<AddressableIndex> handle)
    {
        stbData.Clear();

        for (int i = 0; i < handle.Result.groups.Count;i++)
        {
            var stb = handle.Result.groups[i];

            stbData.Add(stb.groupName);
        }

        dropdownSTB.AddOptions(stbData);

        LoadSTB();
    }

    /// <summary>
    /// When STB are loaded.
    /// </summary>
    /// <param name="handle">Handle.</param>
    private void IDLoaded(AsyncOperationHandle<AddressableIndex> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            AddressableIndex index = handle.Result;

            if (index.groups != null && index.groups.Count > 0)
            {
                addressable = index;
            }
        }

        Addressables.LoadAssetAsync<AddressableIndex>("index-items").Completed += STBLoaded;

        Addressables.Release(handle);
    }

    /// <summary>
    /// Load an item.
    /// </summary>
    /// <param name="address">Address.</param>
    private void LoadSTB()
    {
        itemDatas.Clear();

        dropdownTypes.ClearOptions();

        foreach (var pair in AssetManager.Instance.itemTypes.ids)
        {
            if (pair.Value.meta.ToLower() == stbData[lastSTBIndex].ToLower())
            {
                itemDatas.Add(pair.Value);
            }
        }

        dropdownTypes.AddOptions(itemDatas.Select(d => d.dataName).ToList());

        lastCategoryIndex = 0;

        LoadItems();
    }

    /// <summary>
    /// Load items.
    /// </summary>
    private void LoadItems()
    {
        Utils.DestroyChildren(itemContainer.gameObject);

        var label = itemDatas[lastCategoryIndex].dataName;

        Addressables.LoadAssetsAsync<EquipmentItemData>(label).Completed += OnAllAssetsLoaded;
    }

    /// <summary>
    /// When all assets are loaded.
    /// </summary>
    /// <param name="handle"></param>
    private void OnAllAssetsLoaded(AsyncOperationHandle<IList<EquipmentItemData>> handle)
    {
       var assets = handle.Result.OrderBy(asset => asset.id).ToList();

        for (int i = 0; i < assets.Count;i++)
        {
            var icon = Instantiate(iconPrefab, itemContainer).GetComponent<Image>();

            icon.sprite = AssetManager.Instance.GetItemIcon(assets[i].iconID);
        }
    }

    /// <summary>
    /// When item dropdown selection changed.
    /// </summary>
    /// <param name="selectedIndex">Selected index.</param>
    public void OnDropdownSTBChanged(int selectedIndex)
    {
        lastSTBIndex = selectedIndex;

        lastCategoryIndex = 0;

        LoadSTB();

        LoadItems();
    }

    /// <summary>
    /// When item dropdown selection changed.
    /// </summary>
    /// <param name="selectedIndex">Selected index.</param>
    public void OnDropdownCategoryChanged(int selectedIndex)
    {
        lastCategoryIndex = selectedIndex;

        LoadItems();
    }
}
