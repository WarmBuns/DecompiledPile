using System;
using ThreeEyedGames;
using UnityEngine;

namespace RoR2;

public class ArtifactFormulaDisplay : MonoBehaviour
{
	[Serializable]
	public struct ArtifactCompoundDisplayInfo
	{
		public ArtifactCompoundDef artifactCompoundDef;

		public Decal decal;
	}

	public ArtifactCompoundDisplayInfo[] artifactCompoundDisplayInfos;

	private void Start()
	{
		ArtifactCompoundDisplayInfo[] array = artifactCompoundDisplayInfos;
		for (int i = 0; i < array.Length; i++)
		{
			ArtifactCompoundDisplayInfo artifactCompoundDisplayInfo = array[i];
			artifactCompoundDisplayInfo.decal.Material = artifactCompoundDisplayInfo.artifactCompoundDef.decalMaterial;
		}
	}
}
