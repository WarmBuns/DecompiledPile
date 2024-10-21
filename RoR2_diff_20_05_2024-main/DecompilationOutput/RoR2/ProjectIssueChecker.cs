using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace RoR2;

public class ProjectIssueChecker
{
	private struct LogMessage
	{
		public bool error;

		public string message;

		public UnityEngine.Object context;

		public string assetPath;
	}

	private Dictionary<Type, List<MethodInfo>> assetCheckMethods;

	private List<MethodInfo> allChecks;

	private Dictionary<MethodInfo, bool> enabledChecks;

	private bool checkScenes = true;

	private List<string> scenesToCheck = new List<string>();

	private string currentAssetPath = "";

	private readonly Stack<UnityEngine.Object> assetStack = new Stack<UnityEngine.Object>();

	private UnityEngine.Object currentAsset;

	private List<LogMessage> log = new List<LogMessage>();

	private string currentSceneName = "";

	private static IEnumerable<Assembly> GetAssemblies()
	{
		List<string> list = new List<string>();
		Stack<Assembly> stack = new Stack<Assembly>();
		stack.Push(Assembly.GetEntryAssembly());
		do
		{
			Assembly asm = stack.Pop();
			yield return asm;
			AssemblyName[] referencedAssemblies = asm.GetReferencedAssemblies();
			foreach (AssemblyName assemblyName in referencedAssemblies)
			{
				if (!list.Contains(assemblyName.FullName))
				{
					stack.Push(Assembly.Load(assemblyName));
					list.Add(assemblyName.FullName);
				}
			}
		}
		while (stack.Count > 0);
	}

	private ProjectIssueChecker()
	{
		assetCheckMethods = new Dictionary<Type, List<MethodInfo>>();
		allChecks = new List<MethodInfo>();
		enabledChecks = new Dictionary<MethodInfo, bool>();
		Assembly[] source = new Assembly[4]
		{
			typeof(GameObject).Assembly,
			typeof(Canvas).Assembly,
			typeof(RoR2Application).Assembly,
			typeof(TMP_Text).Assembly
		};
		Type[] types = source.SelectMany((Assembly a) => a.GetTypes()).ToArray();
		Type[] array = types;
		for (int i = 0; i < array.Length; i++)
		{
			MethodInfo[] methods = array[i].GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MethodInfo methodInfo2 in methods)
			{
				foreach (AssetCheckAttribute customAttribute in CustomAttributeExtensions.GetCustomAttributes<AssetCheckAttribute>(methodInfo2))
				{
					Type assetType = customAttribute.assetType;
					AddMethodForTypeDescending(assetType, methodInfo2);
					if (!allChecks.Contains(methodInfo2))
					{
						allChecks.Add(methodInfo2);
						enabledChecks.Add(methodInfo2, value: true);
					}
				}
			}
		}
		void AddMethodForTypeDescending(Type t, MethodInfo methodInfo)
		{
			foreach (Type item in types.Where(t.IsAssignableFrom))
			{
				AddMethodForType(item, methodInfo);
				void AddMethodForType(Type t, MethodInfo methodInfo)
				{
					List<MethodInfo> value = null;
					assetCheckMethods.TryGetValue(t, out value);
					if (value == null)
					{
						value = new List<MethodInfo>();
						assetCheckMethods[t] = value;
					}
					value.Add(methodInfo);
				}
			}
		}
	}

	private string GetCurrentAssetFullPath()
	{
		GameObject gameObject = null;
		string arg = "";
		if (currentAsset is GameObject)
		{
			gameObject = (GameObject)currentAsset;
		}
		else if (currentAsset is Component)
		{
			gameObject = ((Component)currentAsset).gameObject;
		}
		string arg2 = (currentAsset ? currentAsset.name : "NULL ASSET");
		if ((bool)gameObject)
		{
			arg2 = Util.GetGameObjectHierarchyName(gameObject);
		}
		string arg3 = (currentAsset ? currentAsset.GetType().Name : "VOID");
		return $"{arg}:{arg2}({arg3})";
	}

	public void Log(string message, UnityEngine.Object context = null)
	{
		log.Add(new LogMessage
		{
			error = false,
			message = message,
			assetPath = GetCurrentAssetFullPath(),
			context = context
		});
	}

	public void LogError(string message, UnityEngine.Object context = null)
	{
		log.Add(new LogMessage
		{
			error = true,
			message = message,
			assetPath = GetCurrentAssetFullPath(),
			context = context
		});
	}

	public void LogFormat(UnityEngine.Object context, string format, params object[] args)
	{
		log.Add(new LogMessage
		{
			error = false,
			message = string.Format(format, args),
			assetPath = GetCurrentAssetFullPath(),
			context = context
		});
	}

	public void LogErrorFormat(UnityEngine.Object context, string format, params object[] args)
	{
		log.Add(new LogMessage
		{
			error = true,
			message = string.Format(format, args),
			assetPath = GetCurrentAssetFullPath(),
			context = context
		});
	}

	private void FlushLog()
	{
		bool flag = false;
		for (int i = 0; i < log.Count; i++)
		{
			if (log[i].error)
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			foreach (LogMessage item in log)
			{
				if (item.error)
				{
					Debug.LogErrorFormat(item.context, "[\"{0}\"] {1}", item.assetPath, item.message);
				}
				else
				{
					Debug.LogFormat(item.context, "[\"{0}\"] {1}", item.assetPath, item.message);
				}
			}
		}
		log.Clear();
	}
}
