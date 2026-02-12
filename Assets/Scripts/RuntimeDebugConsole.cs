using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class RuntimeDebugConsole : MonoBehaviour
{
    private static RuntimeDebugConsole instance;
    private List<string> logs = new List<string>();
    private bool isVisible = false;
    private GameObject consoleCanvas;
    private Text logText;
    private ScrollRect scrollRect;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        CreateConsoleUI();
        Application.logMessageReceived += HandleLog;
        Debug.Log("Runtime Debug Console Initialized");
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            Application.logMessageReceived -= HandleLog;
        }
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        string color = "white";
        switch (type)
        {
            case LogType.Error:
            case LogType.Exception:
                color = "red";
                break;
            case LogType.Warning:
                color = "yellow";
                break;
        }

        string newLog = $"<color={color}>[{System.DateTime.Now:HH:mm:ss}] {logString}</color>";
        logs.Add(newLog);
        
        // Keep only last 50 logs to prevent memory issues
        if (logs.Count > 50) logs.RemoveAt(0);

        if (isVisible && logText != null)
        {
            UpdateLogText();
        }
    }

    private void UpdateLogText()
    {
        logText.text = string.Join("\n", logs);
        // Scroll to bottom
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    private void CreateConsoleUI()
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("DebugConsoleCanvas");
        DontDestroyOnLoad(canvasObj);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // On top of everything
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.AddComponent<GraphicRaycaster>();
        consoleCanvas = canvasObj;

        // Create Panel (Background)
        GameObject panelObj = new GameObject("ConsolePanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f);
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(1, 1); // Full screen
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Create Scroll View
        GameObject scrollObj = new GameObject("ScrollView");
        scrollObj.transform.SetParent(panelObj.transform, false);
        scrollRect = scrollObj.AddComponent<ScrollRect>();
        RectTransform scrollRectTrans = scrollObj.GetComponent<RectTransform>();
        scrollRectTrans.anchorMin = new Vector2(0, 0);
        scrollRectTrans.anchorMax = new Vector2(1, 0.9f); // Leave space for toggle button
        scrollRectTrans.offsetMin = new Vector2(10, 10);
        scrollRectTrans.offsetMax = new Vector2(-10, -10);

        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollObj.transform, false);
        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        viewport.AddComponent<Image>().color = new Color(1,1,1,0.01f); // Needs image for mask

        // Content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 1000); // Height will be controlled by ContentSizeFitter
        content.AddComponent<VerticalLayoutGroup>().childControlHeight = true;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;

        // Text
        GameObject textObj = new GameObject("LogText");
        textObj.transform.SetParent(content.transform, false);
        logText = textObj.AddComponent<Text>();
        logText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        logText.fontSize = 24;
        logText.color = Color.white;
        logText.horizontalOverflow = HorizontalWrapMode.Wrap;
        logText.verticalOverflow = VerticalWrapMode.Overflow;
        logText.alignment = TextAnchor.LowerLeft;

        // Toggle Button (Visible always)
        GameObject btnObj = new GameObject("ToggleConsoleBtn");
        btnObj.transform.SetParent(canvasObj.transform, false);
        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(1, 0, 0, 0.5f); // Red semi-transparent
        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(ToggleVisibility);
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.95f);
        btnRect.anchorMax = new Vector2(0.5f, 0.95f);
        btnRect.sizeDelta = new Vector2(200, 50);
        btnRect.anchoredPosition = new Vector2(0, -25);

        // Button Text
        GameObject btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        Text btnText = btnTextObj.AddComponent<Text>();
        btnText.text = "Debug Console";
        btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnText.fontSize = 20;
        btnText.color = Color.white;
        btnText.alignment = TextAnchor.MiddleCenter;
        RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = Vector2.zero;
        btnTextRect.offsetMax = Vector2.zero;

        // Initial State
        panelObj.SetActive(false);
        isVisible = false;
    }

    public void ToggleVisibility()
    {
        isVisible = !isVisible;
        consoleCanvas.transform.Find("ConsolePanel").gameObject.SetActive(isVisible);
        if (isVisible) UpdateLogText();
    }
}
