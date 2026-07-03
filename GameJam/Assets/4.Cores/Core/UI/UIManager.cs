using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;

public partial class ViewID
{

}

public partial class PopupId
{
    public static string PopupNotify = "PopupNotify";
    public static string DungeonCardSelection = "DungeonCardSelection";
    public static string DungeonSelectionPopup = "DungeonSelectionPopup";
    public static string UltimatePopup = "UltimatePopup";
    public static string IntroPopup = "IntroPopup";

    
}



public class UIManager : Singleton<UIManager>
{
    [SerializeField]
    private ViewBase _initialView;

    [SerializeField]
    private string initialView;

    [SerializeField]
    private string initialPopup;

    [SerializeField]
    private bool autoShowInitialView = true;

    [SerializeField]
    private Transform _viewContainer;

    [SerializeField]
    private Transform _popupContainer;

    private readonly Dictionary<string, ViewBase> viewCache = new();
    private readonly Dictionary<string, PopupBase> popupCache = new();
    private readonly List<PopupBase> activePopups = new();
    public List<PopupBase> ActivePopups => activePopups;

    private readonly string viewResourcePath = "UI/Views/";
    private readonly string popupResourcePath = "UI/Popups/";

    private void Start()
    {
        if (!autoShowInitialView) return;
        // if (_initialView == null && string.IsNullOrEmpty(initialPopup) && !string.IsNullOrEmpty(initialView))
        // {
        //     initialPopup = PopupId.IntroPopup;
        // }
        ShowInitialView();
    }

    private void ShowInitialView()
    {
        if (_initialView != null) return;

        if (!string.IsNullOrEmpty(initialPopup) && initialPopup == PopupId.IntroPopup)
        {
            OnShowPopup(initialPopup);
            Debug.Log($"[UIManager] Showing intro popup: {initialPopup}");
            return;
        }

        if (!string.IsNullOrEmpty(initialView))
        {
            OnShowView(initialView);
            Debug.Log($"[UIManager] Automatically showing initial view: {initialView}");
        }

        if (!string.IsNullOrEmpty(initialPopup))
        {
            OnShowPopup(initialPopup);
            Debug.Log($"[UIManager] Automatically showing initial popup: {initialPopup}");
        }
    }

    public void AddView(string viewId, ViewBase view)
    {
        if (viewCache.ContainsKey(viewId)) return;
        viewCache.Add(viewId, view);
    }

    // ---------------------- VIEWS ----------------------
    public async void OnShowView(string viewId, params object[] args)
    {
        Debug.LogWarning("[UIManager] OnShowView " + viewId);

        if (string.IsNullOrEmpty(viewId))
        {
            Debug.LogWarning("[UIManager] Attempted to show default view");
            return;
        }

        if (!viewCache.ContainsKey(viewId))
        {
            var viewPrefab = await LoadViewAsync(viewId);
            if (viewPrefab == null)
            {
                Debug.LogError($"[UIManager] Failed to load view prefab for {viewId}");
                return;
            }

            var viewInstance = Instantiate(viewPrefab, _viewContainer);
            viewCache[viewId] = viewInstance;
            Debug.Log($"[UIManager] Loaded and cached view: {viewId}");
        }


        foreach (var view in viewCache.Values) view.Hide();

        OnHidePopup();

        viewCache[viewId].Show(args);
    }

    public void OnHideView(string viewId)
    {
        if (!viewCache.ContainsKey(viewId))
        {
            Debug.LogWarning($"[UIManager] View {viewId} not found in cache");
            return;
        }

        viewCache[viewId].Hide();
        Debug.Log($"[UIManager] Hiding view: {viewId}");
    }

    // ---------------------- POPUPS ----------------------
    public async void OnShowPopup(string popupId, params object[] args)
    {
        Debug.LogWarning("[UIManager] OnShowPopup " + popupId);

        if (string.IsNullOrEmpty(popupId))
        {
            Debug.LogWarning("[UIManager] Attempted to show default popup");
            return;
        }

        if (!popupCache.ContainsKey(popupId))
        {
            var popupPrefab = await LoadPopupAsync(popupId);
            if (popupPrefab == null)
            {
                Debug.LogError($"[UIManager] Failed to load popup prefab for {popupId}");
                return;
            }

            var popupInstance = Instantiate(popupPrefab, _popupContainer);
            popupInstance.gameObject.SetActive(false);
            popupCache[popupId] = popupInstance;
            Debug.Log($"[UIManager] Loaded and cached popup: {popupId}");
        }

        PopupBase popup = popupCache[popupId];
        popup.Show(args);
        popup.transform.SetAsLastSibling();
        activePopups.Add(popup);
        Debug.Log($"[UIManager] Showing popup: {popupId}");
    }

    public void RemovePopup(PopupBase popup)
    {
        activePopups.Remove(popup);
    }

    public void OnHidePopup()
    {
        if (activePopups.Count == 0)
        {
            Debug.LogWarning("[UIManager] No active popups to hide");
            return;
        }

        var popupsToHide = new List<PopupBase>(activePopups);

        foreach (var popup in popupsToHide)
        {
            popup.Hide();
        }

        activePopups.Clear();
    }

    public void OnHidePopup(string popupId)
    {
        if (string.IsNullOrEmpty(popupId)) return;
        if (!popupCache.Keys.Contains(popupId)) return;
        popupCache[popupId].Hide();
    }

    // ---------------------- NOTIFY ----------------------
    public void ShowNoti(string title, Action onHide = null)
    {
        OnShowPopup(PopupId.PopupNotify, title, onHide);
    }

    public void HideNoti()
    {
        OnHidePopup();
    }

    // ---------------------- ASYNC LOAD HELPERS ----------------------
    private Task<ViewBase> LoadViewAsync(string viewId)
    {
        var tcs = new TaskCompletionSource<ViewBase>();
        StartCoroutine(LoadCoroutine($"{viewResourcePath}{viewId}", tcs));
        return tcs.Task;
    }

    private Task<PopupBase> LoadPopupAsync(string popupId)
    {
        var tcs = new TaskCompletionSource<PopupBase>();
        StartCoroutine(LoadCoroutine($"{popupResourcePath}{popupId}", tcs));
        return tcs.Task;
    }

    private IEnumerator LoadCoroutine<T>(string path, TaskCompletionSource<T> tcs) where T : UnityEngine.Object
    {
        var request = Resources.LoadAsync<T>(path);
        yield return request;
        tcs.SetResult(request.asset as T);
    }

    public PopupBase GetCurrentPopup()
    {
        if (activePopups.Count == 0) return null;

        return activePopups[activePopups.Count - 1];
    }

    // public bool IsTutoring()
    // {
    //     if (UIManager.Instance.ActivePopups.Count > 0)
    //         if ((UIManager.Instance.GetCurrentPopup() is TutorialPopup &&
    //              UIManager.Instance.GetCurrentPopup().gameObject.activeSelf) ||
    //             (UIManager.Instance.activePopups.Last() is TutorialPopup &&
    //              UIManager.Instance.activePopups.Last().gameObject.activeSelf))
    //             return true;
    //     return false;
    // }

}