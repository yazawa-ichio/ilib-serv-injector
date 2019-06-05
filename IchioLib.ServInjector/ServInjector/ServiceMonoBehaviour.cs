using UnityEngine;

namespace ILib.ServInject
{
	public abstract class ServiceMonoBehaviour<T> : MonoBehaviour where T : class
	{

		protected virtual void Awake()
		{
			ServInjector.Bind<T>(this);
		}

		protected virtual void OnDestroy()
		{
			ServInjector.Unbind<T>(this);
		}

	}
}
