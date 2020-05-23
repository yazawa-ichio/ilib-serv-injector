using UnityEngine;

namespace ILib.ServInject
{
	/// <summary>
	/// Tにサービスとして登録するインタフェースを指定してください。
	/// </summary>
	public abstract class ServiceMonoBehaviour<T> : MonoBehaviour where T : class
	{

		void Awake()
		{
			ServInjector.Bind<T>(this);
			OnAwake();
		}

		protected virtual void OnAwake() { }

		protected void OnDestroy()
		{
			ServInjector.Unbind<T>(this);
			OnDestroyEvent();
		}

		protected virtual void OnDestroyEvent() { }

	}
}