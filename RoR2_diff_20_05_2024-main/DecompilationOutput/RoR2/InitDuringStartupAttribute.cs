using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HG.Reflection;

namespace RoR2;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public class InitDuringStartupAttribute : HG.Reflection.SearchableAttribute
{
	public static bool loadingIsComplete;

	public static IEnumerator Execute()
	{
		int currentPass = 0;
		int maxPassesBeforeYielding = 5;
		RoR2Application.PrintSW("InitDuringStartupAttribute before assembly scan");
		List<HG.Reflection.SearchableAttribute> instances = HG.Reflection.SearchableAttribute.GetInstances<InitDuringStartupAttribute>();
		RoR2Application.PrintSW("InitDuringStartupAttribute after assembly scan");
		int i = 0;
		foreach (InitDuringStartupAttribute item in instances)
		{
			Type declaringType = (item.target as MethodInfo).DeclaringType;
			RoR2Application.PrintSW("InitDuringStartupAttribute running attribute " + i + " " + declaringType.ToString());
			int num = i + 1;
			i = num;
			((Action)Delegate.CreateDelegate(typeof(Action), item.target as MethodInfo))();
			currentPass++;
			if (currentPass >= maxPassesBeforeYielding)
			{
				currentPass = 0;
				yield return null;
			}
		}
		loadingIsComplete = true;
	}
}
