using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ILib.ServInject
{
	/// <summary>
	/// Tにサービスとして登録するインタフェースを指定してください。
	/// </summary>
	public abstract class Service<T> : IDisposable where T : class
	{
		public Service()
		{
			ServInjector.Bind<T>(this);
		}

		public void Dispose()
		{
			ServInjector.Unbind<T>(this);
			OnDispose();
		}

		protected virtual void OnDispose()
		{

		}
	}
}