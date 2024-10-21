using System;
using System.Collections.Generic;
using HG;
using RoR2.ContentManagement;
using RoR2.Modding;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Audio;

public static class NetworkSoundEventCatalog
{
	private static NetworkSoundEventDef[] entries = Array.Empty<NetworkSoundEventDef>();

	private static readonly Dictionary<string, NetworkSoundEventIndex> eventNameToIndexTable = new Dictionary<string, NetworkSoundEventIndex>();

	private static readonly Dictionary<uint, NetworkSoundEventIndex> eventIdToIndexTable = new Dictionary<uint, NetworkSoundEventIndex>();

	[Obsolete("Use IContentPackProvider instead.")]
	public static event Action<List<NetworkSoundEventDef>> getSoundEventDefs
	{
		add
		{
			LegacyModContentPackProvider.instance.HandleLegacyGetAdditionalEntries("RoR2.Audio.NetworkSoundCatalog.getSoundEventDefs", value, LegacyModContentPackProvider.instance.registrationContentPack.networkSoundEventDefs);
		}
		remove
		{
		}
	}

	[SystemInitializer(new Type[] { })]
	private static void Init()
	{
		SetNetworkSoundEvents(ContentManager.networkSoundEventDefs);
	}

	public static void SetNetworkSoundEvents(NetworkSoundEventDef[] newEntries)
	{
		eventNameToIndexTable.Clear();
		eventIdToIndexTable.Clear();
		ArrayUtils.CloneTo(newEntries, ref entries);
		Array.Sort(entries, (NetworkSoundEventDef a, NetworkSoundEventDef b) => string.CompareOrdinal(a.name, b.name));
		for (int i = 0; i < entries.Length; i++)
		{
			NetworkSoundEventDef networkSoundEventDef = entries[i];
			networkSoundEventDef.index = (NetworkSoundEventIndex)i;
			networkSoundEventDef.akId = AkSoundEngine.GetIDFromString(networkSoundEventDef.eventName);
			if (networkSoundEventDef.akId == 0)
			{
				Debug.LogErrorFormat("Error during network sound registration: Wwise event \"{0}\" does not exist.", networkSoundEventDef.eventName);
			}
		}
		for (int j = 0; j < entries.Length; j++)
		{
			NetworkSoundEventDef networkSoundEventDef2 = entries[j];
			eventNameToIndexTable[networkSoundEventDef2.eventName] = networkSoundEventDef2.index;
			eventIdToIndexTable[networkSoundEventDef2.akId] = networkSoundEventDef2.index;
		}
	}

	public static NetworkSoundEventIndex FindNetworkSoundEventIndex(string eventName)
	{
		if (eventNameToIndexTable.TryGetValue(eventName, out var value))
		{
			return value;
		}
		return NetworkSoundEventIndex.Invalid;
	}

	public static NetworkSoundEventIndex FindNetworkSoundEventIndex(uint akEventId)
	{
		if (eventIdToIndexTable.TryGetValue(akEventId, out var value))
		{
			return value;
		}
		return NetworkSoundEventIndex.Invalid;
	}

	public static uint GetAkIdFromNetworkSoundEventIndex(NetworkSoundEventIndex eventIndex)
	{
		if (eventIndex == NetworkSoundEventIndex.Invalid)
		{
			return 0u;
		}
		return entries[(int)eventIndex].akId;
	}

	public static void WriteNetworkSoundEventIndex(this NetworkWriter writer, NetworkSoundEventIndex networkSoundEventIndex)
	{
		writer.WritePackedUInt32((uint)(networkSoundEventIndex + 1));
	}

	public static NetworkSoundEventIndex ReadNetworkSoundEventIndex(this NetworkReader reader)
	{
		return (NetworkSoundEventIndex)(reader.ReadPackedUInt32() - 1);
	}

	public static string GetEventNameFromId(uint akEventId)
	{
		if (eventIdToIndexTable.TryGetValue(akEventId, out var value))
		{
			return entries[(int)value].eventName;
		}
		return null;
	}

	public static string GetEventNameFromNetworkIndex(NetworkSoundEventIndex networkSoundEventIndex)
	{
		return ArrayUtils.GetSafe(entries, (int)networkSoundEventIndex)?.eventName;
	}
}
