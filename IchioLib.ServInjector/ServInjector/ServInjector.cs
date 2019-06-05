using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;

namespace ILib.ServInject
{
	public static class ServInjector
	{
		class Entry
		{
			public PropertyInfo[] Properties;
			public IHolder[] Holders;
		}

		static Dictionary<Type, IHolder> s_Holder = new Dictionary<Type, IHolder>();
		static Dictionary<Type, Entry> s_InjectEntry = new Dictionary<Type, Entry>();

		internal static void Register<T>(Holder<T> holder) where T : class
		{
			s_Holder.Add(typeof(T), holder);
		}

		public static void Bind<T>(object service) where T : class
		{
			Holder<T>.Instance.Service = (T)service;
			if (service is IServiceEventReceiver eventReceiver)
			{
				eventReceiver.OnBind();
			}
		}

		public static void Unbind<T>(object service) where T : class
		{
			if (Holder<T>.Instance.Service == service)
			{
				Holder<T>.Instance.Service = null;
				if (service is IServiceEventReceiver eventReceiver)
				{
					eventReceiver.OnUnbind();
				}
			}
		}

		public static T Resolve<T>() where T : class
		{
			return Holder<T>.Instance.Service;
		}

		public static void Inject(object obj)
		{
			Inject(obj.GetType(), obj);
		}

		public static void Inject(Type type, object obj)
		{
			Entry entry;
			if (!s_InjectEntry.TryGetValue(type, out entry))
			{
				s_InjectEntry[type] = entry = new Entry();
				entry.Holders = GetInjectors(type).ToArray();
				if (entry.Holders.Length == 0) entry.Holders = null;
				entry.Properties = GetPropertyInfos(type).ToArray();
				if (entry.Properties.Length == 0) entry.Properties = null;
			}
			if (entry.Holders != null)
			{
				foreach (var item in entry.Holders)
				{
					item.Inject(obj);
				}
			}
			if (entry.Properties != null)
			{
				foreach (var item in entry.Properties)
				{
					IHolder holder;
					if (s_Holder.TryGetValue(item.PropertyType, out holder))
					{
						holder.Inject(item, obj);
					}
				}
			}
		}

		public static T Create<T>(Type type = null) where T : class, new()
		{
			T item = new T();
			Inject(type ?? typeof(T), item);
			return item;
		}

		public static T AddComponent<T>(GameObject obj, Type type = null) where T : MonoBehaviour, new()
		{
			T item = obj.AddComponent<T>();
			Inject(type ?? typeof(T), item);
			return item;
		}

		public static void Clear()
		{
			foreach (var item in s_Holder.Values)
			{
				item.Clear();
			}
		}

		static IEnumerable<IHolder> GetInjectors(Type type)
		{
			foreach (var item in s_Holder.Values)
			{
				if (item.IsTarget(type))
				{
					yield return item;
				}
			}
		}

		static IEnumerable<PropertyInfo> GetPropertyInfos(Type type)
		{
			foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
			{
				if (Attribute.IsDefined(prop, typeof(InjectAttribute), true))
				{
					yield return prop;
				}
			}
		}

	}
}
