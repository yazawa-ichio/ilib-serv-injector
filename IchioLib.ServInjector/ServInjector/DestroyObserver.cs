using UnityEngine;

namespace ILib.ServInject
{
	internal class DestroyObserver : MonoBehaviour
	{
		public System.Action OnDestroyEvent;
		private void OnDestroy()
		{
			OnDestroyEvent();
		}
	}
}