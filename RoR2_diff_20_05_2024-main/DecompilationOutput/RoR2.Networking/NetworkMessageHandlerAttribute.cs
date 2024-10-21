using System;
using System.Collections.Generic;
using System.Reflection;
using HG.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Networking;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
[MeansImplicitUse]
public class NetworkMessageHandlerAttribute : HG.Reflection.SearchableAttribute
{
	public short msgType;

	public bool server;

	public bool client;

	private NetworkMessageDelegate messageHandler;

	private static List<NetworkMessageHandlerAttribute> clientMessageHandlers = new List<NetworkMessageHandlerAttribute>();

	private static List<NetworkMessageHandlerAttribute> serverMessageHandlers = new List<NetworkMessageHandlerAttribute>();

	[InitDuringStartup]
	private static void CollectHandlers()
	{
		clientMessageHandlers.Clear();
		serverMessageHandlers.Clear();
		HashSet<short> hashSet = new HashSet<short>();
		foreach (NetworkMessageHandlerAttribute instance in HG.Reflection.SearchableAttribute.GetInstances<NetworkMessageHandlerAttribute>())
		{
			MethodInfo methodInfo = instance.target as MethodInfo;
			if (methodInfo == null || !methodInfo.IsStatic)
			{
				continue;
			}
			instance.messageHandler = (NetworkMessageDelegate)Delegate.CreateDelegate(typeof(NetworkMessageDelegate), methodInfo);
			if (instance.messageHandler != null)
			{
				if (instance.client)
				{
					clientMessageHandlers.Add(instance);
					hashSet.Add(instance.msgType);
				}
				if (instance.server)
				{
					serverMessageHandlers.Add(instance);
					hashSet.Add(instance.msgType);
				}
			}
			if (instance.messageHandler == null)
			{
				Debug.LogWarningFormat("Could not register message handler for {0}. The function signature is likely incorrect.", methodInfo.Name);
			}
			if (!instance.client && !instance.server)
			{
				Debug.LogWarningFormat("Could not register message handler for {0}. It is marked as neither server nor client.", methodInfo.Name);
			}
		}
		for (short num = 48; num < 80; num++)
		{
			if (!hashSet.Contains(num))
			{
				Debug.LogWarningFormat("Network message MsgType.Highest + {0} is unregistered.", num - 47);
			}
		}
	}

	public static void RegisterServerMessages()
	{
		foreach (NetworkMessageHandlerAttribute serverMessageHandler in serverMessageHandlers)
		{
			NetworkServer.RegisterHandler(serverMessageHandler.msgType, serverMessageHandler.messageHandler);
		}
	}

	public static void RegisterClientMessages(NetworkClient client)
	{
		foreach (NetworkMessageHandlerAttribute clientMessageHandler in clientMessageHandlers)
		{
			client.RegisterHandler(clientMessageHandler.msgType, clientMessageHandler.messageHandler);
		}
	}
}
