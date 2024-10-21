using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HG;
using JetBrains.Annotations;
using RoR2.ContentManagement;
using RoR2.Modding;

namespace RoR2;

public static class ArtifactCatalog
{
	private static ArtifactDef[] artifactDefs = Array.Empty<ArtifactDef>();

	public static ResourceAvailability availability;

	public static int artifactCount => artifactDefs.Length;

	[Obsolete("Use IContentPackProvider instead.")]
	public static event Action<List<ArtifactDef>> getAdditionalEntries
	{
		add
		{
			LegacyModContentPackProvider.instance.HandleLegacyGetAdditionalEntries("RoR2.ArtifactCatalog.getAdditionalEntries", value, LegacyModContentPackProvider.instance.registrationContentPack.artifactDefs);
		}
		remove
		{
		}
	}

	private static void RegisterArtifact(ArtifactIndex artifactIndex, ArtifactDef artifactDef)
	{
		artifactDefs[(int)artifactIndex] = artifactDef;
	}

	[SystemInitializer(new Type[] { })]
	private static void Init()
	{
		SetArtifactDefs(ContentManager.artifactDefs);
		availability.MakeAvailable();
	}

	private static void SetArtifactDefs([NotNull] ArtifactDef[] newArtifactDefs)
	{
		artifactDefs = ArrayUtils.Clone(newArtifactDefs);
		Array.Sort(artifactDefs, (ArtifactDef a, ArtifactDef b) => string.CompareOrdinal(a.cachedName, b.cachedName));
		for (int i = 0; i < artifactDefs.Length; i++)
		{
			artifactDefs[i].artifactIndex = (ArtifactIndex)i;
		}
	}

	public static ArtifactIndex FindArtifactIndex(string artifactName)
	{
		for (int i = 0; i < artifactDefs.Length; i++)
		{
			if (string.CompareOrdinal(artifactName, artifactDefs[i].cachedName) == 0)
			{
				return (ArtifactIndex)i;
			}
		}
		return ArtifactIndex.None;
	}

	public static ArtifactDef FindArtifactDef(string artifactName)
	{
		return GetArtifactDef(FindArtifactIndex(artifactName));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ArtifactDef GetArtifactDef(ArtifactIndex artifactIndex)
	{
		return ArrayUtils.GetSafe(artifactDefs, (int)artifactIndex, (ArtifactDef)null);
	}
}
