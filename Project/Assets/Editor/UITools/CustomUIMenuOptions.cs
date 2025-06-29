using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEngine.EventSystems;

public static class CustomUIMenuOptions
{
    private const string kUILayerName = "UI";
    
    // 菜单优先级
    private const int kButtonPriority = 2001;
    private const int kImagePriority = 2002;
    private const int kRawImagePriority = 2003;
    
    #region 菜单项
    
    [MenuItem("GameObject/UI/Custom/Custom Button", false, kButtonPriority)]
    public static void CreateCustomButton(MenuCommand menuCommand)
    {
        GameObject go = CreateUIElement("Custom Button", 160, 30);
        
        // 添加必要的组件
        Image image = go.AddComponent<CustomImage>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = Image.Type.Sliced;
        
        CustomButton button = go.AddComponent<CustomButton>();
        button.targetGraphic = image;
        
        // 添加文本
        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        Text text = textGo.AddComponent<Text>();
        text.text = "Button";
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        text.color = Color.black;
        
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        PlaceUIElementRoot(go, menuCommand);
    }
    
    [MenuItem("GameObject/UI/Custom/Custom Image", false, kImagePriority)]
    public static void CreateCustomImage(MenuCommand menuCommand)
    {
        GameObject go = CreateUIElement("Custom Image", 100, 100);
        
        CustomImage image = go.AddComponent<CustomImage>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        image.type = Image.Type.Sliced;
        
        PlaceUIElementRoot(go, menuCommand);
    }
    
    [MenuItem("GameObject/UI/Custom/Custom RawImage", false, kRawImagePriority)]
    public static void CreateCustomRawImage(MenuCommand menuCommand)
    {
        GameObject go = CreateUIElement("Custom RawImage", 100, 100);
        
        go.AddComponent<CustomRawImage>();
        
        PlaceUIElementRoot(go, menuCommand);
    }
    
    // 右键菜单 - 转换现有组件
    [MenuItem("CONTEXT/Button/Convert to Custom Button")]
    private static void ConvertToCustomButton(MenuCommand menuCommand)
    {
        Button button = menuCommand.context as Button;
        if (button != null)
        {
            GameObject go = button.gameObject;
            
            // 保存按钮属性
            var targetGraphic = button.targetGraphic;
            var colors = button.colors;
            var onClick = button.onClick;
            
            // 移除原始Button
            Object.DestroyImmediate(button);
            
            // 添加CustomButton
            CustomButton customButton = go.AddComponent<CustomButton>();
            customButton.targetGraphic = targetGraphic;
            customButton.colors = colors;
            
            // 复制onClick事件
            if (onClick != null && onClick.GetPersistentEventCount() > 0)
            {
                for (int i = 0; i < onClick.GetPersistentEventCount(); i++)
                {
                    customButton.onClick.AddListener(() => {
                        onClick.Invoke();
                    });
                }
            }
            
            Debug.Log($"Successfully converted {go.name} to CustomButton");
        }
    }
    
    [MenuItem("CONTEXT/Image/Convert to Custom Image")]
    private static void ConvertToCustomImage(MenuCommand menuCommand)
    {
        Image image = menuCommand.context as Image;
        if (image != null && !(image is CustomImage))
        {
            GameObject go = image.gameObject;
            
            // 保存Image属性
            var sprite = image.sprite;
            var color = image.color;
            var material = image.material;
            var raycastTarget = image.raycastTarget;
            var imageType = image.type;
            var fillCenter = image.fillCenter;
            
            // 移除原始Image
            Object.DestroyImmediate(image);
            
            // 添加CustomImage
            CustomImage customImage = go.AddComponent<CustomImage>();
            customImage.sprite = sprite;
            customImage.color = color;
            customImage.material = material;
            customImage.raycastTarget = raycastTarget;
            customImage.type = imageType;
            customImage.fillCenter = fillCenter;
            
            Debug.Log($"Successfully converted {go.name} to CustomImage");
        }
    }
    
    [MenuItem("CONTEXT/RawImage/Convert to Custom RawImage")]
    private static void ConvertToCustomRawImage(MenuCommand menuCommand)
    {
        RawImage rawImage = menuCommand.context as RawImage;
        if (rawImage != null && !(rawImage is CustomRawImage))
        {
            GameObject go = rawImage.gameObject;
            
            // 保存RawImage属性
            var texture = rawImage.texture;
            var color = rawImage.color;
            var material = rawImage.material;
            var raycastTarget = rawImage.raycastTarget;
            var uvRect = rawImage.uvRect;
            
            // 移除原始RawImage
            Object.DestroyImmediate(rawImage);
            
            // 添加CustomRawImage
            CustomRawImage customRawImage = go.AddComponent<CustomRawImage>();
            customRawImage.texture = texture;
            customRawImage.color = color;
            customRawImage.material = material;
            customRawImage.raycastTarget = raycastTarget;
            customRawImage.uvRect = uvRect;
            
            Debug.Log($"Successfully converted {go.name} to CustomRawImage");
        }
    }
    
    #endregion
    
    #region 辅助方法
    
    private static GameObject CreateUIElement(string name, float width, float height)
    {
        GameObject go = new GameObject(name);
        RectTransform rectTransform = go.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(width, height);
        return go;
    }
    
    private static void PlaceUIElementRoot(GameObject element, MenuCommand menuCommand)
    {
        GameObject parent = menuCommand.context as GameObject;
        
        if (parent == null || parent.GetComponentInParent<Canvas>() == null)
        {
            parent = GetOrCreateCanvasGameObject();
        }
        
        string uniqueName = GameObjectUtility.GetUniqueNameForSibling(parent.transform, element.name);
        element.name = uniqueName;
        
        GameObjectUtility.SetParentAndAlign(element, parent);
        
        RectTransform rectTransform = element.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = Vector2.zero;
        
        // 设置层级
        SetLayerRecursively(element, parent.layer);
        
        Selection.activeGameObject = element;
        
        // 标记场景为脏
        if (!Application.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );
        }
    }
    
    private static GameObject GetOrCreateCanvasGameObject()
    {
        GameObject selectedGo = Selection.activeGameObject;
        
        // 查找现有Canvas
        Canvas canvas = (selectedGo != null) ? selectedGo.GetComponentInParent<Canvas>() : null;
        if (canvas != null && canvas.isRootCanvas)
        {
            return canvas.gameObject;
        }
        
        // 查找场景中的Canvas
        canvas = Object.FindObjectOfType<Canvas>();
        if (canvas != null && canvas.isRootCanvas)
        {
            return canvas.gameObject;
        }
        
        // 创建新Canvas
        GameObject canvasGo = new GameObject("Canvas");
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();
        
        // 设置层级
        canvasGo.layer = LayerMask.NameToLayer(kUILayerName);
        
        // 创建EventSystem
        CreateEventSystem();
        
        return canvasGo;
    }
    
    private static void CreateEventSystem()
    {
        EventSystem eventSystem = Object.FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<StandaloneInputModule>();
        }
    }
    
    private static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        Transform t = go.transform;
        for (int i = 0; i < t.childCount; i++)
        {
            SetLayerRecursively(t.GetChild(i).gameObject, layer);
        }
    }
    
    #endregion
}
