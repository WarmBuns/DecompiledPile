using System;
using UnityEngine;

namespace RoR2;

[Serializable]
public struct ArtifactMask
{
	[SerializeField]
	public ushort a;

	public static readonly ArtifactMask none;

	public static ArtifactMask all;

	public bool HasArtifact(ArtifactIndex artifactIndex)
	{
		if (artifactIndex < (ArtifactIndex)0 || (int)artifactIndex >= ArtifactCatalog.artifactCount)
		{
			return false;
		}
		return (a & (1 << (int)artifactIndex)) != 0;
	}

	public void AddArtifact(ArtifactIndex artifactIndex)
	{
		if (artifactIndex >= (ArtifactIndex)0 && (int)artifactIndex < ArtifactCatalog.artifactCount)
		{
			a |= (ushort)(1 << (int)artifactIndex);
		}
	}

	public void ToggleArtifact(ArtifactIndex artifactIndex)
	{
		if (artifactIndex >= (ArtifactIndex)0 && (int)artifactIndex < ArtifactCatalog.artifactCount)
		{
			a ^= (ushort)(1 << (int)artifactIndex);
		}
	}

	public void RemoveArtifact(ArtifactIndex artifactIndex)
	{
		if (artifactIndex >= (ArtifactIndex)0 && (int)artifactIndex < ArtifactCatalog.artifactCount)
		{
			a &= (ushort)(~(1 << (int)artifactIndex));
		}
	}

	public static ArtifactMask operator &(ArtifactMask mask1, ArtifactMask mask2)
	{
		ArtifactMask result = default(ArtifactMask);
		result.a = (ushort)(mask1.a & mask2.a);
		return result;
	}

	[SystemInitializer(new Type[] { typeof(ArtifactCatalog) })]
	private static void Init()
	{
		all = default(ArtifactMask);
		for (ArtifactIndex artifactIndex = (ArtifactIndex)0; (int)artifactIndex < ArtifactCatalog.artifactCount; artifactIndex++)
		{
			all.AddArtifact(artifactIndex);
		}
	}
}
