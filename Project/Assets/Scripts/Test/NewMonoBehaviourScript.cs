using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NewMonoBehaviourScript : MonoBehaviour
{
    public CustomImage customImage;
    void Start()
    {
        customImage.onClick.AddListener(HandleImageClick);
        //customImage.EnableGrayscale = true; // 启用灰度效果
       // GetComponent<Button>().onClick.AddListener(HandleImageClick);
    }

    private void HandleImageClick(PointerEventData arg0)
    {
        Debug.Log(customImage.gameObject.name + " 被点击了");
    }

   

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            customImage.GrayscaleAmount += 0.1f; 
            Debug.Log(" 当前灰度值: " + customImage.GrayscaleAmount);
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            customImage.GrayscaleAmount -= 0.1f; // 重置灰度值
            Debug.Log(" 当前灰度值: " + customImage.GrayscaleAmount);
        }
        else if (Input.GetKeyDown(KeyCode.T))
        {
            customImage.EnableGrayscale = true; // 启用灰度效果
            Debug.Log(" 当前灰度值: " + customImage.GrayscaleAmount);
        }
        else if (Input.GetKeyDown(KeyCode.G))
        {
            customImage.EnableGrayscale = false; // 禁用灰度效果
            Debug.Log(" 当前灰度值: " + customImage.GrayscaleAmount);
        }
        else if (Input.GetKeyDown(KeyCode.H))
        {
           
        }
    }
}
