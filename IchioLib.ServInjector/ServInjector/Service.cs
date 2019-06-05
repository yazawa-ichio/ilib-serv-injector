using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ILib.ServInject
{
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
