using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class SavePrefab
{
    [MenuItem("ROSE Online/Tools/Save To Prefab")]
    static void SaveSelectedAsOrganizedPrefab()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogWarning("No GameObject selected.");
            return;
        }

        string folder = EditorUtility.SaveFolderPanel("Select Base Folder for Prefab Export", "Assets", "");
        if (string.IsNullOrEmpty(folder))
        {
            return;
        }

        // Validate folder inside Assets
        string projectPath = Application.dataPath;
        if (!folder.StartsWith(projectPath))
        {
            Debug.LogError("Please select a folder inside your Assets directory.");
            return;
        }

        string baseFolder = "Assets" + folder.Substring(projectPath.Length);
        string meshFolder = Path.Combine(baseFolder, "Meshes");
        string matFolder = Path.Combine(baseFolder, "Materials");
        string prefabFolder = Path.Combine(baseFolder, "Prefabs");

        Directory.CreateDirectory(meshFolder);
        Directory.CreateDirectory(matFolder);
        Directory.CreateDirectory(prefabFolder);

        //Avoid duplicates
        Dictionary<Mesh, Mesh> savedMeshes = new Dictionary<Mesh, Mesh>();
        Dictionary<Material, Material> savedMaterials = new Dictionary<Material, Material>();

        //MeshFilters
        foreach (MeshFilter mf in selected.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null)
            {
                continue;
            }
            Mesh mesh = mf.sharedMesh;

            if (!savedMeshes.ContainsKey(mesh))
            {
                Mesh meshCopy = Object.Instantiate(mesh);
                string meshPath = Path.Combine(meshFolder, mf.gameObject.name + "_Mesh.asset");
                meshPath = AssetDatabase.GenerateUniqueAssetPath(meshPath);
                AssetDatabase.CreateAsset(meshCopy, meshPath);
                savedMeshes[mesh] = meshCopy;
                Debug.Log($"Saved Mesh: {meshPath}");
            }
            mf.sharedMesh = savedMeshes[mesh];
        }

        //SkinnedMeshRenderers
        foreach (SkinnedMeshRenderer smr in selected.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (smr.sharedMesh == null)
            {
                continue;
            }
            Mesh mesh = smr.sharedMesh;

            if (!savedMeshes.ContainsKey(mesh))
            {
                Mesh meshCopy = Object.Instantiate(mesh);
                string meshPath = Path.Combine(meshFolder, smr.gameObject.name + "_SkinnedMesh.asset");
                meshPath = AssetDatabase.GenerateUniqueAssetPath(meshPath);
                AssetDatabase.CreateAsset(meshCopy, meshPath);
                savedMeshes[mesh] = meshCopy;
                Debug.Log($"Saved Skinned Mesh: {meshPath}");
            }
            smr.sharedMesh = savedMeshes[mesh];
        }

        //MeshRenderers and SkinnedMeshRenderer Materials
        foreach (Renderer renderer in selected.GetComponentsInChildren<Renderer>(true))
        {
            AssignAndSaveMaterials(renderer, savedMaterials, matFolder);
        }

        //MeshColliders
        foreach (MeshCollider mc in selected.GetComponentsInChildren<MeshCollider>(true))
        {
            if (mc.sharedMesh == null)
            {
                continue;
            }
            Mesh mesh = mc.sharedMesh;

            if (savedMeshes.TryGetValue(mesh, out Mesh savedMesh))
            {
                mc.sharedMesh = savedMesh;
                Debug.Log($"Linked existing mesh for collider on {mc.gameObject.name}");
            }
            else
            {
                Mesh meshCopy = Object.Instantiate(mesh);
                string meshPath = Path.Combine(meshFolder, mc.gameObject.name + "_ColliderMesh.asset");
                meshPath = AssetDatabase.GenerateUniqueAssetPath(meshPath);
                AssetDatabase.CreateAsset(meshCopy, meshPath);
                savedMeshes[mesh] = meshCopy;
                mc.sharedMesh = meshCopy;
                Debug.Log($"Saved new collider mesh: {meshPath}");
            }
        }

        //Save Prefab
        string prefabPath = Path.Combine(prefabFolder, selected.name + ".prefab");
        prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);
        PrefabUtility.SaveAsPrefabAssetAndConnect(selected, prefabPath, InteractionMode.UserAction);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Organized prefab created successfully at: {prefabPath}");
    }

    //Helper to save and assign materials
    static void AssignAndSaveMaterials(Renderer renderer, Dictionary<Material, Material> savedMaterials, string matFolder)
    {
        if (renderer == null)
        {
            return;
        }
        Material[] mats = renderer.sharedMaterials;
        if (mats == null || mats.Length == 0)
        {
            return;
        }

        Material[] newMats = new Material[mats.Length];
        for (int i = 0; i < mats.Length; i++)
        {
            Material mat = mats[i];
            if (mat == null) 
            {
                continue;
            }

            if (!savedMaterials.ContainsKey(mat))
            {
                Material matCopy = new Material(mat);
                string matPath = Path.Combine(matFolder, renderer.gameObject.name + "_Mat" + i + ".mat");
                matPath = AssetDatabase.GenerateUniqueAssetPath(matPath);
                AssetDatabase.CreateAsset(matCopy, matPath);
                savedMaterials[mat] = matCopy;
                Debug.Log($"Saved Material: {matPath}");
            }

            newMats[i] = savedMaterials[mat];
        }

        renderer.sharedMaterials = newMats;
    }
}
