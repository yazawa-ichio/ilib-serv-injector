//#define ILIB_DISABLE_SERV_REFLECTION_INJECT
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;

namespace ILib.ServInject
{
	internal class InjectEntry
	{
		Dictionary<Type, IHolder> m_Holder;
		IHolder[] m_Injectors;

#if !ILIB_DISABLE_SERV_REFLECTION_INJECT
		class MethodEntry
		{
			public MethodInfo Method;
			public Type[] Types;
		}

		static object[][] s_ValueTemporary;
		static object[] GetValueTemporary(int num)
		{
			if (s_ValueTemporary == null)
			{
				s_ValueTemporary = new object[num][];
			}
			else if (s_ValueTemporary.Length < num)
			{
				Array.Resize(ref s_ValueTemporary, num);
			}
			var ret = s_ValueTemporary[num - 1];
			if (ret == null)
			{
				ret = s_ValueTemporary[num - 1] = new object[num];
			}
			return ret;
		}
		PropertyInfo[] m_Properties;
		MethodEntry[] m_Methods;
#endif

		public InjectEntry(Type type, Dictionary<Type, IHolder> holder)
		{
			m_Holder = holder;
			m_Injectors = GetInjectors(type).ToArray();
#if !ILIB_DISABLE_SERV_REFLECTION_INJECT
			m_Properties = GetPropertyInfos(type).ToArray();
			m_Methods = GetMethodEntry(type).ToArray();
#endif
		}

		public void Inject(object obj)
		{
			for (int i = 0; i < m_Injectors.Length; i++)
			{
				IHolder item = m_Injectors[i];
				item.Inject(obj);
			}
#if !ILIB_DISABLE_SERV_REFLECTION_INJECT
			for (int i = 0; i < m_Properties.Length; i++)
			{
				PropertyInfo item = m_Properties[i];
				IHolder holder;
				if (m_Holder.TryGetValue(item.PropertyType, out holder))
				{
					holder.Inject(item, obj);
				}
			}
			for (int i = 0; i < m_Methods.Length; i++)
			{
				var item = m_Methods[i];
				var value = GetValueTemporary(item.Types.Length);
				for (int j = 0; j < item.Types.Length; j++)
				{
					IHolder holder;
					if (m_Holder.TryGetValue(item.Types[j], out holder))
					{
						value[j] = holder.Get();
					}
				}
				item.Method.Invoke(obj, value);
				Array.Clear(value, 0, value.Length);
			}
#endif
		}

		IEnumerable<IHolder> GetInjectors(Type type)
		{
			foreach (var item in m_Holder.Values)
			{
				if (item.IsTarget(type))
				{
					yield return item;
				}
			}
		}

		public void SetHolder(IHolder holder)
		{
			if (m_Injectors == null)
			{
				m_Injectors = new IHolder[1];
			}
			else
			{
				Array.Resize(ref m_Injectors, m_Injectors.Length + 1);
			}
			m_Injectors[m_Injectors.Length - 1] = holder;
		}

#if !ILIB_DISABLE_SERV_REFLECTION_INJECT
		IEnumerable<PropertyInfo> GetPropertyInfos(Type type)
		{
			foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
			{
				if (Attribute.IsDefined(prop, typeof(InjectAttribute), true))
				{
					yield return prop;
				}
			}
		}

		IEnumerable<MethodEntry> GetMethodEntry(Type type)
		{
			foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public))
			{
				if (Attribute.IsDefined(method, typeof(InjectAttribute), true))
				{
					var entry = new MethodEntry();
					entry.Method = method;
					entry.Types = method.GetParameters().Select(x => x.ParameterType).ToArray();
					yield return entry;
				}
			}
		}
#endif

	}

}
