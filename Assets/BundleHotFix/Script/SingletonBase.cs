using UnityEngine;
using System.Collections;

public abstract class SingletonBase<T> where T : new()
{
	private static T instance;
	
	public static T Instance
	{
		get
		{
			if(instance == null)
				instance = new T();
				
			return instance;
		}
	}

    public virtual void Init(){
    }
}
