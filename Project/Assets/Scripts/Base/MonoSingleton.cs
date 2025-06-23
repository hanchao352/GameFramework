using System;
using UnityEngine;

public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<T>();
                if (instance == null)
                {
                    GameObject singleton = new GameObject();
                    instance = singleton.AddComponent<T>();
                    singleton.name = $"{typeof(T).ToString()} (Singleton)";
                }
            }
            else
            {
                
            }

            return instance;
        }
    }

    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = (T)this;
        }
        else if (instance != this)
        {
            Destroy(this.gameObject);
        }

        DontDestroyOnLoad(this.gameObject);
    }

    protected virtual void Start()
    {
        
    }
    
    public virtual void Update()
    {
       
    }
    protected virtual void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }


 
  

    public virtual void Initialize()
    {
        // Override in subclass to perform initialization
    }

    protected virtual void OnApplicationPause(bool pauseStatus)
    {
      
    }


    protected virtual void OnApplicationFocus(bool hasFocus)
    {
        
    }
    
    protected virtual void OnApplicationQuit()
    {
        
    }
    
}