using System;
using System.Collections.Generic;
using System.Xml.Linq;
using HG;
using JetBrains.Annotations;
using RoR2.ContentManagement;
using RoR2.Modding;

namespace RoR2;

public static class GameEndingCatalog
{
	private static GameEndingDef[] gameEndingDefs = Array.Empty<GameEndingDef>();

	public static int endingCount => gameEndingDefs.Length;

	[Obsolete("Use IContentPackProvider instead.")]
	public static event Action<List<GameEndingDef>> collectEntries
	{
		add
		{
			LegacyModContentPackProvider.instance.HandleLegacyGetAdditionalEntries("RoR2.GameEndingCatalog.collectEntries", value, LegacyModContentPackProvider.instance.registrationContentPack.gameEndingDefs);
		}
		remove
		{
		}
	}

	[SystemInitializer(new Type[] { })]
	private static void Init()
	{
		HGXml.Register(delegate(XElement element, GameEndingDef contents)
		{
			element.Value = contents?.cachedName ?? "";
		}, delegate(XElement element, ref GameEndingDef contents)
		{
			contents = FindGameEndingDef(element.Value);
			return true;
		});
		SetGameEndingDefs(ContentManager.gameEndingDefs);
	}

	private static void SetGameEndingDefs(GameEndingDef[] newGameEndingDefs)
	{
		GameEndingDef[] array = gameEndingDefs;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].gameEndingIndex = GameEndingIndex.Invalid;
		}
		ArrayUtils.CloneTo(newGameEndingDefs, ref gameEndingDefs);
		Array.Sort(gameEndingDefs, (GameEndingDef a, GameEndingDef b) => string.CompareOrdinal(a.cachedName, b.cachedName));
		for (int j = 0; j < gameEndingDefs.Length; j++)
		{
			gameEndingDefs[j].gameEndingIndex = (GameEndingIndex)j;
		}
	}

	public static GameEndingDef GetGameEndingDef(GameEndingIndex gameEndingIndex)
	{
		return ArrayUtils.GetSafe(gameEndingDefs, (int)gameEndingIndex);
	}

	public static GameEndingIndex FindGameEndingIndex(string gameEndingName)
	{
		for (int i = 0; i < gameEndingDefs.Length; i++)
		{
			if (string.CompareOrdinal(gameEndingName, gameEndingDefs[i].cachedName) == 0)
			{
				return (GameEndingIndex)i;
			}
		}
		return GameEndingIndex.Invalid;
	}

	public static GameEndingDef FindGameEndingDef(string gameEndingName)
	{
		return GetGameEndingDef(FindGameEndingIndex(gameEndingName));
	}

	public static GameEndingIndex GetGameEndingIndex([CanBeNull] GameEndingDef gameEndingDef)
	{
		if (!gameEndingDef)
		{
			return GameEndingIndex.Invalid;
		}
		return gameEndingDef.gameEndingIndex;
	}
}
