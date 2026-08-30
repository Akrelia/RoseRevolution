using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingManager : MonoBehaviour
{
    /// <summary>
    /// Initialize.
    /// </summary>
    public void Initialize()
    {
        SceneLoaderManager.SetActiveScene();
    }

    /// <summary>
    /// Load a scene by its index in the build settings.
    /// </summary>
    /// <param name="sceneIndex">Scene index.</param>
    /// <param name="loadingInfos">Loading infos.</param>
    /// <param name="onLoaded">On loaded.</param>
    public void LoadScene(int sceneIndex, string loadingInfos, Action onLoaded = null)
    {
        StartCoroutine(SceneLoaderManager.Load(sceneIndex, loadingInfos, onLoaded));
    }

    /// <summary>
    /// Load a scene by its name.
    /// </summary>
    /// <param name="sceneIndex">Scene index.</param>
    /// <param name="loadingInfos">Loading infos.</param>
    /// <param name="onLoaded">On loaded.</param>
    public void LoadScene(string sceneName, string loadingInfos, Action onLoaded = null)
    {
        var sceneIndex = SceneUtility.GetBuildIndexByScenePath(sceneName);

        StartCoroutine(SceneLoaderManager.Load(sceneIndex, loadingInfos, onLoaded));
    }

    /// <summary>
    /// Load a scene additively by its index in the build settings.
    /// </summary>
    /// <param name="sceneIndex">Scene index.</param>
    /// <param name="onLoaded">On loaded.</param>
    public void LoadSceneAdditive(int sceneIndex, Action onLoaded = null)
    {
        SceneLoaderManager.LoadAdditive(sceneIndex, onLoaded);
    }
}

/// <summary>
/// Scene loader manager.
/// </summary>
public static class SceneLoaderManager
{
    public static Scene currentScene;
    public static Scene nexusScene;
    public const string LoadingSceneName = "Loading";

    private const float MinimumWaitingTime = 0.5f;

    public static void SetActiveScene()
    {
        currentScene = SceneManager.GetActiveScene();
    }

    public static void LoadAdditive(int index, Action onLoaded = null)
    {
        string sceneName = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(index));

        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);

        onLoaded?.Invoke();
    }

    public static void LoadAdditive(string sceneName, Action onLoaded = null)
    {
        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);

        onLoaded?.Invoke();
    }

    public static IEnumerator Load(string sceneName, string loadingInfos, Action onLoaded = null)
    {
        float startTime = Time.time;

        AudioListener previousListener = UnityEngine.Object.FindAnyObjectByType<AudioListener>();
        if (previousListener != null) previousListener.enabled = false;

        AsyncOperation loadingScene = SceneManager.LoadSceneAsync(LoadingSceneName, LoadSceneMode.Additive);
        yield return loadingScene;

        Scene loadingSceneRef = SceneManager.GetSceneByName(LoadingSceneName);
        SceneManager.SetActiveScene(loadingSceneRef);

        LoadingGUIController ui = null;

        foreach (var root in loadingSceneRef.GetRootGameObjects())
        {
            ui = root.GetComponentInChildren<LoadingGUIController>();
            if (ui != null) break;
        }

        if (ui != null)
        {
            ui.Initialize(loadingInfos);
        }

        AsyncOperation unloadPrevious = SceneManager.UnloadSceneAsync(currentScene);
        yield return unloadPrevious;

        float elapsed = Time.time - startTime;

        if (elapsed < MinimumWaitingTime)
            yield return new WaitForSeconds(MinimumWaitingTime - elapsed);

        AsyncOperation targetScene = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        targetScene.allowSceneActivation = false;

        float displayedProgress = 0f;

        while (targetScene.progress < 0.9f)
        {
            float realProgress = targetScene.progress / 0.9f;

            displayedProgress = Mathf.MoveTowards(displayedProgress, realProgress, Time.deltaTime * 0.5f);

            if (ui != null)
                ui.SetProgress(displayedProgress);

            yield return null;
        }

        while (displayedProgress < 1f)
        {
            displayedProgress = Mathf.MoveTowards(displayedProgress, 1f, Time.deltaTime * 1.5f);

            if (ui != null)
                ui.SetProgress(displayedProgress);

            yield return null;
        }

        if (ui != null)
            ui.SetProgress(1f);

        targetScene.allowSceneActivation = true;
        yield return targetScene;

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
        currentScene = SceneManager.GetActiveScene();

        onLoaded?.Invoke();

        AudioListener newListener = UnityEngine.Object.FindAnyObjectByType<AudioListener>();

        if (newListener != null) newListener.enabled = true;

        AsyncOperation unloadLoading = SceneManager.UnloadSceneAsync(LoadingSceneName);

        yield return unloadLoading;
    }

    public static IEnumerator Load(int sceneIndex, string loadingInfos, Action onLoaded = null)
    {
        string sceneName = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(sceneIndex));

        yield return Load(sceneName, loadingInfos, onLoaded);
    }
}
