using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 自定义UI基础类 - 提供置灰控制
/// </summary>
public abstract class CustomUIBase : MonoBehaviour
{
    private static Material grayscaleMaterial;
    private static readonly int GrayAmountProperty = Shader.PropertyToID("_GrayAmount");
    
    [Header("置灰控制")]
    [SerializeField] protected bool enableGrayscale = false;
    [SerializeField] [Range(0f, 1f)] protected float grayscaleAmount = 0.5f;
    
    [Header("交互控制")]
    [SerializeField] protected bool canReceiveEvents = true;
    
    protected Graphic targetGraphic;
    protected Material originalMaterial;
    protected Material instanceMaterial;

    #region 属性
    /// <summary>
    /// 是否启用置灰效果
    /// </summary>
    public bool EnableGrayscale
    {
        get => enableGrayscale;
        set
        {
            if (enableGrayscale != value)
            {
                enableGrayscale = value;
                UpdateGrayscaleEffect();
            }
        }
    }

    /// <summary>
    /// 置灰程度 (0-1)
    /// </summary>
    public float GrayscaleAmount
    {
        get => grayscaleAmount;
        set
        {
            grayscaleAmount = Mathf.Clamp01(value);
            if (enableGrayscale)
            {
                UpdateGrayscaleEffect();
            }
        }
    }

    /// <summary>
    /// 是否可以接收事件（独立于置灰状态）
    /// </summary>
    public bool CanReceiveEvents
    {
        get => canReceiveEvents;
        set => canReceiveEvents = value;
    }
    #endregion

    #region Unity生命周期
    protected virtual void Awake()
    {
        InitializeGraphic();
        InitializeGrayscaleMaterial();
    }

    protected virtual void OnDestroy()
    {
        if (instanceMaterial != null)
        {
            DestroyImmediate(instanceMaterial);
        }
    }

    protected virtual void OnValidate()
    {
        if (Application.isPlaying && targetGraphic != null)
        {
            UpdateGrayscaleEffect();
        }
    }
    #endregion

    #region 保护方法
    protected abstract void InitializeGraphic();

    protected void SetTargetGraphic(Graphic graphic)
    {
        targetGraphic = graphic;
        if (targetGraphic != null)
        {
            originalMaterial = targetGraphic.material;
        }
    }

    protected void UpdateGrayscaleEffect()
    {
        if (targetGraphic == null) return;

        if (enableGrayscale && grayscaleAmount > 0)
        {
            if (instanceMaterial == null)
            {
                instanceMaterial = new Material(GetGrayscaleMaterial());
            }
            
            instanceMaterial.SetFloat(GrayAmountProperty, grayscaleAmount);
            targetGraphic.material = instanceMaterial;
        }
        else
        {
            targetGraphic.material = originalMaterial;
            
            if (instanceMaterial != null)
            {
                DestroyImmediate(instanceMaterial);
                instanceMaterial = null;
            }
        }
    }
    #endregion

    #region 静态方法
    private static void InitializeGrayscaleMaterial()
    {
        if (grayscaleMaterial == null)
        {
            Shader shader = Shader.Find("UI/Grayscale");
            if (shader == null)
            {
                Debug.LogError("未找到 UI/Grayscale Shader！请确保Shader已添加到项目中。");
                return;
            }
            grayscaleMaterial = new Material(shader);
        }
    }

    private static Material GetGrayscaleMaterial()
    {
        InitializeGrayscaleMaterial();
        return grayscaleMaterial;
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 设置置灰状态和事件接收状态
    /// </summary>
    public void SetState(bool enableGray, bool canReceiveEvts)
    {
        EnableGrayscale = enableGray;
        CanReceiveEvents = canReceiveEvts;
    }

    /// <summary>
    /// 快速设置为禁用状态（置灰且不可交互）
    /// </summary>
    public void SetDisabled(bool disabled)
    {
        EnableGrayscale = disabled;
        CanReceiveEvents = !disabled;
    }
    #endregion
}
