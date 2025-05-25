using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR

public class CustomAssetPostProcessor : AssetPostprocessor
{
    void OnPreprocessAsset()
    {
        string extension = Path.GetExtension(assetPath).ToLower();

        // Si l'extension est .mov, empêcher l'importation
        if (extension == ".mov")
        {
            Debug.Log("Skipping import for MOV file: " + assetPath);
        }
    }
}

#endif