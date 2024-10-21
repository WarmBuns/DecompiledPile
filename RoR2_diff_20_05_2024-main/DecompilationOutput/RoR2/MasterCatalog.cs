using System;
using System.Collections.Generic;
using System.Linq;
using HG;
using JetBrains.Annotations;
using RoR2.CharacterAI;
using RoR2.ContentManagement;
using RoR2.Modding;
using UnityEngine;

namespace RoR2;

public static class MasterCatalog
{
	public struct MasterIndex : IEquatable<MasterIndex>
	{
		private readonly int i;

		public static readonly MasterIndex none = new MasterIndex(-1);

		public bool isValid => i >= 0;

		public MasterIndex(int i)
		{
			this.i = i;
		}

		public static explicit operator int(MasterIndex masterIndex)
		{
			return masterIndex.i;
		}

		public static explicit operator MasterIndex(int value)
		{
			return new MasterIndex(value);
		}

		public bool Equals(MasterIndex other)
		{
			return i == other.i;
		}

		public override bool Equals(object obj)
		{
			if (obj is MasterIndex other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return i;
		}

		public static bool operator ==(MasterIndex a, MasterIndex b)
		{
			return a.i == b.i;
		}

		public static bool operator !=(MasterIndex a, MasterIndex b)
		{
			return a.i != b.i;
		}
	}

	[Serializable]
	public struct NetworkMasterIndex : IEquatable<NetworkMasterIndex>
	{
		public uint i;

		public static implicit operator NetworkMasterIndex(MasterIndex masterIndex)
		{
			NetworkMasterIndex result = default(NetworkMasterIndex);
			result.i = (uint)((int)masterIndex + 1);
			return result;
		}

		public static implicit operator MasterIndex(NetworkMasterIndex networkMasterIndex)
		{
			return new MasterIndex((int)(networkMasterIndex.i - 1));
		}

		public bool Equals(NetworkMasterIndex other)
		{
			return i == other.i;
		}

		public override bool Equals(object obj)
		{
			if (obj is MasterIndex masterIndex)
			{
				return Equals(masterIndex);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (int)i;
		}
	}

	private static bool isInitialized = false;

	private static GameObject[] masterPrefabs;

	private static CharacterMaster[] masterPrefabMasterComponents;

	private static CharacterMaster[] aiMasterPrefabs;

	private static readonly Dictionary<string, MasterIndex> nameToIndexMap = new Dictionary<string, MasterIndex>();

	public static IEnumerable<CharacterMaster> allMasters => masterPrefabMasterComponents;

	public static IEnumerable<CharacterMaster> allAiMasters => aiMasterPrefabs;

	[Obsolete("Use IContentPackProvider instead.")]
	public static event Action<List<GameObject>> getAdditionalEntries
	{
		add
		{
			LegacyModContentPackProvider.instance.HandleLegacyGetAdditionalEntries("RoR2.MasterCatalog.getAdditionalEntries", value, LegacyModContentPackProvider.instance.registrationContentPack.masterPrefabs);
		}
		remove
		{
		}
	}

	public static GameObject GetMasterPrefab(MasterIndex masterIndex)
	{
		return ArrayUtils.GetSafe(masterPrefabs, (int)masterIndex);
	}

	public static MasterIndex FindMasterIndex([NotNull] string masterName)
	{
		if (nameToIndexMap.TryGetValue(masterName, out var value))
		{
			return value;
		}
		return MasterIndex.none;
	}

	public static MasterIndex FindMasterIndex(GameObject masterObject)
	{
		if (!masterObject)
		{
			return MasterIndex.none;
		}
		return FindMasterIndex(masterObject.name);
	}

	public static GameObject FindMasterPrefab([NotNull] string bodyName)
	{
		MasterIndex masterIndex = FindMasterIndex(bodyName);
		if (masterIndex.isValid)
		{
			return GetMasterPrefab(masterIndex);
		}
		return null;
	}

	public static MasterIndex FindAiMasterIndexForBody(BodyIndex bodyIndex)
	{
		CharacterMaster[] array = aiMasterPrefabs;
		foreach (CharacterMaster characterMaster in array)
		{
			if (characterMaster.bodyPrefab.GetComponent<CharacterBody>().bodyIndex == bodyIndex)
			{
				return characterMaster.masterIndex;
			}
		}
		return MasterIndex.none;
	}

	[SystemInitializer(new Type[] { })]
	private static void Init()
	{
		if (!isInitialized)
		{
			SetEntries(ContentManager.masterPrefabs);
			isInitialized = true;
		}
	}

	private static void SetEntries(GameObject[] newEntries)
	{
		masterPrefabs = ArrayUtils.Clone(newEntries);
		Array.Sort(masterPrefabs, (GameObject a, GameObject b) => string.CompareOrdinal(a.name, b.name));
		masterPrefabMasterComponents = new CharacterMaster[masterPrefabs.Length];
		for (int i = 0; i < masterPrefabs.Length; i++)
		{
			MasterIndex masterIndex = new MasterIndex(i);
			GameObject obj = masterPrefabs[i];
			string name = obj.name;
			CharacterMaster component = obj.GetComponent<CharacterMaster>();
			nameToIndexMap.Add(name, masterIndex);
			nameToIndexMap.Add(name + "(Clone)", masterIndex);
			masterPrefabMasterComponents[i] = component;
			component.masterIndex = masterIndex;
		}
		aiMasterPrefabs = masterPrefabMasterComponents.Where((CharacterMaster master) => master.GetComponent<BaseAI>()).ToArray();
	}
}
