using System;

namespace NMJToolBox
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public class EnumFlagsAttribute : DrawerAttribute
	{
	}
}
