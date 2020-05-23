using System.Reflection;

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

		public object Get()
		{
			return Service;
		}

		public void Clear()
		{
			Service = null;
		}
	}
}