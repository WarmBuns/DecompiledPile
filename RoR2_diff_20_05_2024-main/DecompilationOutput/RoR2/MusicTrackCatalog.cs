using System;
using System.Collections.Generic;
using HG;
using RoR2.ContentManagement;
using RoR2.Modding;

namespace RoR2;

public static class MusicTrackCatalog
{
	private static MusicTrackDef[] musicTrackDefs = Array.Empty<MusicTrackDef>();

	[Obsolete("Use IContentPackProvider instead.")]
	public static event Action<List<MusicTrackDef>> collectEntries
	{
		add
		{
			LegacyModContentPackProvider.instance.HandleLegacyGetAdditionalEntries("RoR2.MusicTrackCatalog.collectEntries", value, LegacyModContentPackProvider.instance.registrationContentPack.musicTrackDefs);
		}
		remove
		{
		}
	}

	[SystemInitializer(new Type[] { })]
	private static void Init()
	{
		SetEntries(ContentManager.musicTrackDefs);
	}

	private static void SetEntries(MusicTrackDef[] newMusicTrackDefs)
	{
		MusicTrackDef[] array = musicTrackDefs;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].catalogIndex = MusicTrackIndex.Invalid;
		}
		ArrayUtils.CloneTo(newMusicTrackDefs, ref musicTrackDefs);
		Array.Sort(musicTrackDefs, (MusicTrackDef a, MusicTrackDef b) => string.CompareOrdinal(a.cachedName, b.cachedName));
		for (int j = 0; j < musicTrackDefs.Length; j++)
		{
			musicTrackDefs[j].catalogIndex = (MusicTrackIndex)j;
		}
	}

	public static MusicTrackDef GetMusicTrackDef(MusicTrackIndex musicTrackIndex)
	{
		return ArrayUtils.GetSafe(musicTrackDefs, (int)musicTrackIndex);
	}

	public static MusicTrackDef FindMusicTrackDef(string name)
	{
		return GetMusicTrackDef(FindMusicTrackIndex(name));
	}

	public static MusicTrackIndex FindMusicTrackIndex(string name)
	{
		for (int i = 0; i < musicTrackDefs.Length; i++)
		{
			if (string.CompareOrdinal(musicTrackDefs[i].cachedName, name) == 0)
			{
				return (MusicTrackIndex)i;
			}
		}
		return MusicTrackIndex.Invalid;
	}
}
