using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ILib.ServInject
{
	internal interface IHolder
	{
		bool IsTarget(System.Type type);
		void Inject(object obj);
		void Inject(PropertyInfo item, object obj);
		object Get();
		void Clear();
	}

	internal class Holder<T> : IHolder where T : class
	{
		public static Holder<T> Instance = new Holder<T>();

		public T Service;

		List<TaskCompletionSource<T>> m_Future = new List<TaskCompletionSource<T>>();

		private Holder()
		{
			ServInjector.Register(this);
		}

		public bool IsTarget(System.Type type)
		{
			return typeof(IInject<T>).IsAssignableFrom(type);
		}

		public void Inject(object obj)
		{
			if (obj is IInject<T> inject)
			{
				inject.Install(Service);
			}
		}

		public void Inject(PropertyInfo item, object obj)
		{
			item.SetValue(obj, Service);
		}

		public void OnBind()
		{
			while (m_Future.Count > 0)
			{
				var fturue = m_Future[0];
				m_Future.RemoveAt(0);
				fturue.TrySetResult(Service);
			}
		}

		public object Get()
		{
			return Service;
		}

		public void Clear()
		{
			Service = null;
		}

		public Task<T> GetAsync(CancellationToken token)
		{
			TaskCompletionSource<T> future = new TaskCompletionSource<T>();
			if (token != CancellationToken.None)
			{
				token.Register(() =>
				{
					m_Future.Remove(future);
					future.TrySetCanceled(token);
				});
			}
			m_Future.Add(future);
			return future.Task;
		}
	}
}