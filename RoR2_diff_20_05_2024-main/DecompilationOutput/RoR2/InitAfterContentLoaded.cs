using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HG.Reflection;

namespace RoR2;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public class InitAfterContentLoaded : HG.Reflection.SearchableAttribute
{
	public static bool loadingIsComplete;

	public static IEnumerator Execute()
	{
		int currentPass = 0;
		int maxPassesBeforeYielding = 5;
		List<HG.Reflection.SearchableAttribute> instances = HG.Reflection.SearchableAttribute.GetInstances<InitAfterContentLoaded>();
		foreach (InitAfterContentLoaded item in instances)
		{
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
