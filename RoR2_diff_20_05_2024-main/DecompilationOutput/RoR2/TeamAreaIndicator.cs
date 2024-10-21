using System;
using UnityEngine;

namespace RoR2;

public class TeamAreaIndicator : MonoBehaviour
{
	[Serializable]
	public struct TeamMaterialPair
	{
		public TeamIndex teamIndex;

		public Material sharedMaterial;
	}

	public TeamComponent teamComponent;

	public TeamFilter teamFilter;

	public TeamMaterialPair[] teamMaterialPairs;

	public Renderer[] areaIndicatorRenderers;

	private void Start()
	{
		if ((bool)teamFilter || (bool)teamComponent)
		{
			TeamIndex teamIndex = (teamFilter ? teamFilter.teamIndex : (teamComponent ? teamComponent.teamIndex : TeamIndex.None));
			for (int i = 0; i < teamMaterialPairs.Length; i++)
			{
				if (teamMaterialPairs[i].teamIndex == teamIndex)
				{
					Renderer[] array = areaIndicatorRenderers;
					for (int j = 0; j < array.Length; j++)
					{
						array[j].sharedMaterial = teamMaterialPairs[i].sharedMaterial;
					}
				}
			}
		}
		else
		{
			Debug.LogWarning("No TeamFilter or TeamComponent assigned to TeamAreaIndicator.");
			base.gameObject.SetActive(value: false);
		}
	}
}
