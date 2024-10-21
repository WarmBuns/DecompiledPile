using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using HG.Reflection;
using JetBrains.Annotations;
using UnityEngine;

namespace RoR2;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
[MeansImplicitUse]
public class SystemInitializerAttribute : HG.Reflection.SearchableAttribute
{
	private class SystemInitializationLogHandler : ILogHandler
	{
		public ILogHandler underlyingLogHandler;

		private SystemInitializerAttribute _currentInitializer;

		private string logPrefix = string.Empty;

		public SystemInitializerAttribute currentInitializer
		{
			get
			{
				return _currentInitializer;
			}
			set
			{
				_currentInitializer = value;
				logPrefix = "[" + currentInitializer.associatedType.FullName + "] ";
			}
		}

		public void LogException(Exception exception, UnityEngine.Object context)
		{
			LogFormat(LogType.Exception, context, exception.Message, null);
		}

		public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
		{
			underlyingLogHandler.LogFormat(logType, context, logPrefix + format, args);
		}
	}

	private class SystemTiming : IComparable
	{
		public string systemName;

		public long systemInitDuration;

		public long longestSingleIteration;

		public long externalTimeDuration;

		public long totalTimeSpent => externalTimeDuration + systemInitDuration;

		public int CompareTo(object obj)
		{
			SystemTiming systemTiming = obj as SystemTiming;
			return -totalTimeSpent.CompareTo(systemTiming.totalTimeSpent);
		}
	}

	public static bool hasExecuted = false;

	public Type[] dependencies = Array.Empty<Type>();

	private MethodInfo methodInfo;

	private Type associatedType;

	private static List<Type> preInitializedTypes = new List<Type>(5);

	private static Thread initThread;

	private static Queue<SystemInitializerAttribute> initializerAttributes;

	private static long FRAME_TIME_THRESHOLD_MS = 22L;

	public SystemInitializerAttribute(params Type[] dependencies)
	{
		if (dependencies != null)
		{
			this.dependencies = dependencies;
		}
	}

	public static void RegisterPreInitializedType(Type _type)
	{
		preInitializedTypes.Add(_type);
	}

	public static void Execute()
	{
		Queue<SystemInitializerAttribute> queue = new Queue<SystemInitializerAttribute>();
		foreach (SystemInitializerAttribute instance in HG.Reflection.SearchableAttribute.GetInstances<SystemInitializerAttribute>())
		{
			MethodInfo methodInfo = instance.target as MethodInfo;
			if (methodInfo != null && methodInfo.IsStatic)
			{
				queue.Enqueue(instance);
				instance.methodInfo = methodInfo;
				instance.associatedType = methodInfo.DeclaringType;
			}
		}
		HashSet<Type> initializedTypes = new HashSet<Type>();
		int num = 0;
		while (queue.Count > 0)
		{
			SystemInitializerAttribute systemInitializerAttribute2 = queue.Dequeue();
			if (!InitializerDependenciesMet(systemInitializerAttribute2))
			{
				queue.Enqueue(systemInitializerAttribute2);
				num++;
				if (num >= queue.Count)
				{
					break;
				}
			}
			else
			{
				systemInitializerAttribute2.methodInfo.Invoke(null, Array.Empty<object>());
				initializedTypes.Add(systemInitializerAttribute2.associatedType);
				num = 0;
			}
		}
		hasExecuted = true;
		bool InitializerDependenciesMet(SystemInitializerAttribute initializerAttribute)
		{
			Type[] array = initializerAttribute.dependencies;
			foreach (Type item in array)
			{
				if (!initializedTypes.Contains(item))
				{
					return false;
				}
			}
			return true;
		}
	}

	public static void ExecuteStatic()
	{
		int num = 0;
		Type[] assemblyTypes = RoR2Application.AssemblyTypes;
		foreach (Type type in assemblyTypes)
		{
			num++;
			MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MethodInfo element in methods)
			{
				SystemInitializerAttribute customAttribute = CustomAttributeExtensions.GetCustomAttribute<SystemInitializerAttribute>(element);
				if (customAttribute != null)
				{
					initializerAttributes.Enqueue(customAttribute);
					customAttribute.methodInfo = element;
					customAttribute.associatedType = type;
				}
			}
		}
	}

	public static IEnumerator ExecuteCoroutine()
	{
		initializerAttributes = new Queue<SystemInitializerAttribute>();
		initThread = new Thread(ExecuteStatic);
		initThread.Start();
		yield return null;
		while (initThread.IsAlive)
		{
			yield return null;
		}
		initThread = null;
		HashSet<Type> initializedTypes = new HashSet<Type>();
		foreach (Type preInitializedType in preInitializedTypes)
		{
			initializedTypes.Add(preInitializedType);
		}
		preInitializedTypes.Clear();
		new Stopwatch().Start();
		Stopwatch multiLoopTimer = new Stopwatch();
		multiLoopTimer.Start();
		int yields = 0;
		int num = 0;
		while (initializerAttributes.Count > 0)
		{
			SystemInitializerAttribute initializerAttribute2 = initializerAttributes.Dequeue();
			if (!InitializerDependenciesMet(initializerAttribute2))
			{
				initializerAttributes.Enqueue(initializerAttribute2);
				num++;
				if (num >= initializerAttributes.Count)
				{
					UnityEngine.Debug.LogFormat("SystemInitializerAttribute infinite loop detected. currentMethod={0}", initializerAttribute2.associatedType.FullName + initializerAttribute2.methodInfo.Name);
					break;
				}
				continue;
			}
			IEnumerator thisStep = initializerAttribute2.methodInfo.Invoke(null, Array.Empty<object>()) as IEnumerator;
			List<IEnumerator> coroutineStack = new List<IEnumerator> { thisStep };
			multiLoopTimer.Stop();
			long accumulatedTime = multiLoopTimer.ElapsedMilliseconds;
			multiLoopTimer.Start();
			if (accumulatedTime > FRAME_TIME_THRESHOLD_MS)
			{
				accumulatedTime = 0L;
				multiLoopTimer.Stop();
				multiLoopTimer.Reset();
				multiLoopTimer.Start();
				yields++;
				yield return null;
			}
			while (thisStep != null)
			{
				while (accumulatedTime < FRAME_TIME_THRESHOLD_MS && thisStep != null)
				{
					if (!thisStep.MoveNext())
					{
						coroutineStack.RemoveAt(coroutineStack.Count - 1);
						thisStep = ((coroutineStack.Count <= 0) ? null : coroutineStack[coroutineStack.Count - 1]);
					}
					else if (thisStep.Current != null)
					{
						coroutineStack.Add(thisStep.Current as IEnumerator);
						thisStep = coroutineStack[coroutineStack.Count - 1];
					}
					multiLoopTimer.Stop();
					accumulatedTime = multiLoopTimer.ElapsedMilliseconds;
					multiLoopTimer.Start();
				}
				if (accumulatedTime > FRAME_TIME_THRESHOLD_MS)
				{
					accumulatedTime = 0L;
					multiLoopTimer.Stop();
					multiLoopTimer.Reset();
					multiLoopTimer.Start();
				}
				if (thisStep != null)
				{
					yields++;
					accumulatedTime = 0L;
					multiLoopTimer.Stop();
					multiLoopTimer.Reset();
					multiLoopTimer.Start();
					yield return null;
				}
			}
			initializedTypes.Add(initializerAttribute2.associatedType);
			num = 0;
		}
		initializerAttributes = null;
		hasExecuted = true;
		bool InitializerDependenciesMet(SystemInitializerAttribute initializerAttribute)
		{
			Type[] array = initializerAttribute.dependencies;
			foreach (Type item in array)
			{
				initializedTypes.Contains(item);
				if (!initializedTypes.Contains(item))
				{
					return false;
				}
			}
			return true;
		}
	}
}
