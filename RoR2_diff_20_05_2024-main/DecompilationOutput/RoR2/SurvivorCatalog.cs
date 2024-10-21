using System;
using System.Collections.Generic;
using System.Linq;
using HG;
using JetBrains.Annotations;
using RoR2.ContentManagement;
using RoR2.Modding;
using UnityEngine;

namespace RoR2;

public static class SurvivorCatalog
{
	public static int survivorMaxCount = 10;

	private static SurvivorDef[] survivorDefs = Array.Empty<SurvivorDef>();

	private static SurvivorDef[] _orderedSurvivorDefs = Array.Empty<SurvivorDef>();

	private static SurvivorIndex[] bodyIndexToSurvivorIndex = Array.Empty<SurvivorIndex>();

	private static BodyIndex[] survivorIndexToBodyIndex = Array.Empty<BodyIndex>();

	private static int[] survivorOrderPositions = Array.Empty<int>();

	private static string[] cachedSurvivorNames = Array.Empty<string>();

	public static SurvivorDef defaultSurvivor;

	public static int survivorCount => survivorDefs.Length;

	public static SurvivorIndex endIndex => (SurvivorIndex)survivorDefs.Length;

	public static IEnumerable<SurvivorDef> allSurvivorDefs => survivorDefs;

	public static IEnumerable<SurvivorDef> orderedSurvivorDefs => _orderedSurvivorDefs;

	[Obsolete("Use IContentPackProvider instead.")]
	public static event Action<List<SurvivorDef>> getAdditionalSurvivorDefs
	{
		add
		{
			LegacyModContentPackProvider.instance.HandleLegacyGetAdditionalEntries("RoR2.SurvivorCatalog.getAdditionalSurvivorDefs", value, LegacyModContentPackProvider.instance.registrationContentPack.survivorDefs);
		}
		remove
		{
		}
	}

	private static void ValidateEntry(SurvivorDef survivorDef)
	{
		if ((bool)survivorDef.bodyPrefab)
		{
			CharacterBody component = survivorDef.bodyPrefab.GetComponent<CharacterBody>();
			if ((bool)component)
			{
				if (string.IsNullOrEmpty(survivorDef.cachedName))
				{
					string text = BodyCatalog.GetBodyName(component.bodyIndex);
					string text2 = "Body";
					if (text.EndsWith(text2))
					{
						text = text.Substring(0, text.Length - text2.Length);
					}
					survivorDef.cachedName = text;
				}
				if (survivorDef.displayNameToken == null)
				{
					survivorDef.displayNameToken = component.baseNameToken;
				}
			}
		}
		survivorDef.displayNameToken = survivorDef.displayNameToken ?? "";
	}

	[CanBeNull]
	public static SurvivorDef GetSurvivorDef(SurvivorIndex survivorIndex)
	{
		return ArrayUtils.GetSafe(survivorDefs, (int)survivorIndex);
	}

	public static SurvivorIndex GetSurvivorIndexFromBodyIndex(BodyIndex bodyIndex)
	{
		return ArrayUtils.GetSafe(bodyIndexToSurvivorIndex, (int)bodyIndex, SurvivorIndex.None);
	}

	public static BodyIndex GetBodyIndexFromSurvivorIndex(SurvivorIndex survivorIndex)
	{
		return ArrayUtils.GetSafe(survivorIndexToBodyIndex, (int)survivorIndex, BodyIndex.None);
	}

	public static bool SurvivorIsUnlockedOnThisClient(SurvivorIndex survivorIndex)
	{
		foreach (LocalUser readOnlyLocalUsers in LocalUserManager.readOnlyLocalUsersList)
		{
			_ = readOnlyLocalUsers.userProfile;
		}
		return LocalUserManager.readOnlyLocalUsersList.Any((LocalUser localUser) => localUser.userProfile.HasSurvivorUnlocked(survivorIndex));
	}

	[CanBeNull]
	public static SurvivorDef FindSurvivorDefFromBody([CanBeNull] GameObject characterBodyPrefab)
	{
		for (int i = 0; i < survivorDefs.Length; i++)
		{
			SurvivorDef survivorDef = survivorDefs[i];
			GameObject gameObject = ((survivorDef != null) ? survivorDef.bodyPrefab : null);
			if (characterBodyPrefab == gameObject)
			{
				return survivorDef;
			}
		}
		return null;
	}

	[CanBeNull]
	public static Texture GetSurvivorPortrait(SurvivorIndex survivorIndex)
	{
		SurvivorDef survivorDef = GetSurvivorDef(survivorIndex);
		if (survivorDef.bodyPrefab != null)
		{
			CharacterBody component = survivorDef.bodyPrefab.GetComponent<CharacterBody>();
			if ((bool)component)
			{
				return component.portraitIcon;
			}
		}
		return null;
	}

	public static SurvivorIndex FindSurvivorIndex([CanBeNull] string survivorName)
	{
		return (SurvivorIndex)Array.IndexOf(cachedSurvivorNames, survivorName);
	}

	public static SurvivorDef FindSurvivorDef([CanBeNull] string survivorName)
	{
		return GetSurvivorDef(FindSurvivorIndex(survivorName));
	}

	public static int GetSurvivorOrderedPosition(SurvivorIndex survivorIndex)
	{
		return ArrayUtils.GetSafe(survivorOrderPositions, (int)survivorIndex, -1);
	}

	[SystemInitializer(new Type[] { typeof(BodyCatalog) })]
	private static void Init()
	{
		SetSurvivorDefs(ContentManager.survivorDefs);
	}

	private static void SetSurvivorDefs(SurvivorDef[] newSurvivorDefs)
	{
		SurvivorDef[] array = survivorDefs;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].survivorIndex = SurvivorIndex.None;
		}
		ArrayUtils.CloneTo(newSurvivorDefs, ref survivorDefs);
		Array.Resize(ref survivorIndexToBodyIndex, survivorDefs.Length);
		Array.Resize(ref cachedSurvivorNames, survivorDefs.Length);
		Array.Resize(ref _orderedSurvivorDefs, survivorDefs.Length);
		Array.Resize(ref survivorOrderPositions, survivorDefs.Length);
		Array.Resize(ref bodyIndexToSurvivorIndex, BodyCatalog.bodyCount);
		ArrayUtils.SetAll(bodyIndexToSurvivorIndex, SurvivorIndex.None);
		for (SurvivorIndex survivorIndex = (SurvivorIndex)0; (int)survivorIndex < survivorDefs.Length; survivorIndex++)
		{
			SurvivorDef survivorDef = survivorDefs[(int)survivorIndex];
			survivorDef.survivorIndex = survivorIndex;
			ValidateEntry(survivorDef);
			cachedSurvivorNames[(int)survivorIndex] = survivorDef.cachedName;
			BodyIndex bodyIndex = BodyCatalog.FindBodyIndex(survivorDef.bodyPrefab);
			survivorIndexToBodyIndex[(int)survivorIndex] = bodyIndex;
			bodyIndexToSurvivorIndex[(int)bodyIndex] = survivorIndex;
		}
		ArrayUtils.CloneTo(survivorDefs, ref _orderedSurvivorDefs);
		Array.Sort(_orderedSurvivorDefs, (SurvivorDef a, SurvivorDef b) => a.desiredSortPosition.CompareTo(b.desiredSortPosition));
		for (int j = 0; j < _orderedSurvivorDefs.Length; j++)
		{
			SurvivorIndex survivorIndex2 = _orderedSurvivorDefs[j].survivorIndex;
			survivorOrderPositions[(int)survivorIndex2] = j;
		}
		ViewablesCatalog.Node node = new ViewablesCatalog.Node("Survivors", isFolder: true);
		foreach (SurvivorDef survivorDef2 in allSurvivorDefs)
		{
			ViewablesCatalog.Node survivorEntryNode = new ViewablesCatalog.Node(survivorDef2.cachedName, isFolder: false, node);
			survivorEntryNode.shouldShowUnviewed = (UserProfile userProfile) => !userProfile.HasViewedViewable(survivorEntryNode.fullName) && (bool)survivorDef2.unlockableDef && userProfile.HasUnlockable(survivorDef2.unlockableDef);
		}
		ViewablesCatalog.AddNodeToRoot(node);
		defaultSurvivor = FindSurvivorDef("Commando");
	}
}
