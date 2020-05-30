using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ILib.ServInject
{
	public static class ServInjector
	{
		static Dictionary<Type, IHolder> s_Holder = new Dictionary<Type, IHolder>();
		static Dictionary<Type, InjectEntry> s_InjectEntry = new Dictionary<Type, InjectEntry>();

		internal static void Register<T>(Holder<T> holder) where T : class
		{
			s_Holder.Add(typeof(T), holder);
			foreach (var kvp in s_InjectEntry)
			{
				if (holder.IsTarget(kvp.Key))
				{
					kvp.Value.SetHolder(holder);
				}
			}
		}

		/// <summary>
		/// サービスを登録します。
		/// 解放する際はUnbindを実行してください。
		/// </summary>
		public static void Bind<T>(object service) where T : class
		{
			Holder<T>.Instance.Service = (T)service;
			if (service is IServiceEventReceiver eventReceiver)
			{
				eventReceiver.OnBind();
			}
			Holder<T>.Instance.OnBind();
		}

		/// <summary>
		/// サービスを登録します。
		/// 指定のゲームオブジェクトが削除された際に自動でUnbindされます。
		/// </summary>
		public static void BindAndObserveDestroy<T>(MonoBehaviour service, GameObject obj = null) where T : class
		{
			Holder<T>.Instance.Service = (T)(service as object);
			if (service is IServiceEventReceiver eventReceiver)
			{
				eventReceiver.OnBind();
			}
			Holder<T>.Instance.OnBind();
			var observer = obj ?? service.gameObject;
			observer.AddComponent<DestroyObserver>().OnDestroyEvent = () =>
			{
				Unbind<T>(service);
			};
		}

		/// <summary>
		/// サービスの登録を解除します。
		/// </summary>
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

		/// <summary>
		/// 登録されているすべてのサービスを解除します。
		/// </summary>
		public static void Clear()
		{
			foreach (var item in s_Holder.Values)
			{
				item.Clear();
			}
		}

		/// <summary>
		/// 登録されているサービスを取り出します。
		/// 登録されていない場合にnullが返ります。
		/// </summary>
		public static T Resolve<T>() where T : class
		{
			return Holder<T>.Instance.Service;
		}

		/// <summary>
		/// 登録されているサービスを取り出します。
		/// 登録されていない場合はBindされるまで待機します
		/// </summary>
		public static async Task<T> ResolveAsync<T>(CancellationToken token) where T : class
		{
			if (Holder<T>.Instance.Service != null)
			{
				return Holder<T>.Instance.Service;
			}
			return await Holder<T>.Instance.GetAsync(token);
		}

		/// <summary>
		/// 登録されているサービスを取り出します。
		/// 登録されていない場合はBindされるまで待機します
		/// </summary>
		public static Task<T> ResolveAsync<T>() where T : class
		{
			return ResolveAsync<T>(CancellationToken.None);
		}

		/// <summary>
		/// サービスを注入します。
		/// </summary>
		public static void Inject(object obj)
		{
			Inject(obj.GetType(), obj);
		}

		/// <summary>
		/// 指定の方でサービスを注入します。
		/// 基底クラスのみインジェクトされる場合に効率的です
		/// </summary>
		public static void Inject(Type type, object obj)
		{
			InjectEntry entry;
			if (!s_InjectEntry.TryGetValue(type, out entry))
			{
				s_InjectEntry[type] = entry = new InjectEntry(type, s_Holder);
			}
			entry.Inject(obj);
		}

		/// <summary>
		/// インスタンスを生成し、サービスを注入した状態で返します
		/// </summary>
		public static T Create<T>(Type type = null) where T : class, new()
		{
			T item = new T();
			Inject(type ?? typeof(T), item);
			return item;
		}

		/// <summary>
		/// MonoBehaviourを追加し、サービスを注入した状態で返します
		/// </summary>
		public static T AddComponent<T>(GameObject obj, Type type = null) where T : MonoBehaviour, new()
		{
			T item = obj.AddComponent<T>();
			Inject(type ?? typeof(T), item);
			return item;
		}


	}
}