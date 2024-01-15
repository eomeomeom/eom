using UnityEngine;

/// <summary>
/// 매니저 클래스 작성을 위한 싱글톤 클래스
/// </summary>
/// <typeparam name="T"></typeparam>
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instanceValue;

    private static object _lock = new object();

    public static bool IsDetroying { get; private set; }

    private void Awake()
    {
        IsDetroying = false;
    }

    public virtual void OnDestroy()
    {
        IsDetroying = true;
    }

    private void OnApplicationQuit()
    {
        IsDetroying = true;
    }

    public static T _instance
    {
	   get
	   {
		  if (IsDetroying)
			 return null;

		  lock (_lock)
		  {
			 if (_instanceValue == null)
			 {
				_instanceValue = FindObjectOfType<T>();
				T[] instanceList = FindObjectsOfType<T>();
				if (instanceList.Length > 1)
				{
				    for (int i = 0; i < instanceList.Length; i++)
				    {
					   if (instanceList[i] != _instanceValue)
					   {
						  Destroy(instanceList[i].gameObject);
						  i--;
					   }
				    }

				    return _instanceValue;
				}

				if (_instanceValue == null)
				{
				    GameObject singletonObj = new GameObject();
				    _instanceValue = singletonObj.AddComponent<T>();
				    singletonObj.name = $"[{typeof(T)}]";

				    DontDestroyOnLoad(singletonObj);
				}
			 }

			 return _instanceValue;
		  }
	   }
    }

    virtual public void Init() { }
}
