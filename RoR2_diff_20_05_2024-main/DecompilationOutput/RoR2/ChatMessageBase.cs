using System;
using System.Collections.Generic;
using RoR2.ConVar;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public abstract class ChatMessageBase : MessageBase
{
	private static readonly BoolConVar cvChatDebug;

	private static readonly Dictionary<Type, byte> chatMessageTypeToIndex;

	private static readonly List<Type> chatMessageIndexToType;

	static ChatMessageBase()
	{
		cvChatDebug = new BoolConVar("chat_debug", ConVarFlags.None, "0", "Enables logging of chat network messages.");
		chatMessageTypeToIndex = new Dictionary<Type, byte>();
		chatMessageIndexToType = new List<Type>();
		BuildMessageTypeNetMap();
	}

	public abstract string ConstructChatString();

	public virtual void OnProcessed()
	{
	}

	private static void BuildMessageTypeNetMap()
	{
		Type[] types = typeof(ChatMessageBase).Assembly.GetTypes();
		foreach (Type type in types)
		{
			if (type.IsSubclassOf(typeof(ChatMessageBase)))
			{
				chatMessageTypeToIndex.Add(type, (byte)chatMessageIndexToType.Count);
				chatMessageIndexToType.Add(type);
			}
		}
	}

	protected string GetObjectName(GameObject namedObject)
	{
		string result = "???";
		if ((bool)namedObject)
		{
			result = namedObject.name;
			NetworkUser networkUser = namedObject.GetComponent<NetworkUser>();
			if (!networkUser)
			{
				networkUser = Util.LookUpBodyNetworkUser(namedObject);
			}
			if ((bool)networkUser)
			{
				result = Util.EscapeRichTextForTextMeshPro(networkUser.userName);
			}
		}
		return result;
	}

	public byte GetTypeIndex()
	{
		return chatMessageTypeToIndex[GetType()];
	}

	public static ChatMessageBase Instantiate(byte typeIndex)
	{
		Type type = chatMessageIndexToType[typeIndex];
		if (cvChatDebug.value)
		{
			Debug.LogFormat("Received chat message typeIndex={0} type={1}", typeIndex, type?.Name);
		}
		if (type != null)
		{
			return (ChatMessageBase)Activator.CreateInstance(type);
		}
		return null;
	}

	public override void Serialize(NetworkWriter writer)
	{
	}

	public override void Deserialize(NetworkReader reader)
	{
	}
}
