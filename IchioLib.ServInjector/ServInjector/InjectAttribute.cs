using System;
namespace ILib.ServInject
{
#if !ILIB_DISABLE_SERV_REFLECTION_INJECT
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
	public class InjectAttribute : Attribute
	{

	}
#endif
}
