using System;
using UnityEngine;

namespace RoR2;

[CreateAssetMenu(menuName = "RoR2/AssetCollection")]
public class AssetCollection : ScriptableObject
{
	public UnityEngine.Object[] assets = Array.Empty<UnityEngine.Object>();

	[ContextMenu("Add selected assets.")]
	private void AddSelectedAssets()
	{
		UnityEngine.Object[] additionalAssets = Array.Empty<UnityEngine.Object>();
		AddAssets(additionalAssets);
	}

	public void AddAssets(UnityEngine.Object[] additionalAssets)
	{
		int num = assets.Length;
		Array.Resize(ref assets, assets.Length + additionalAssets.Length);
		for (int i = 0; i < additionalAssets.Length; i++)
		{
			assets[num + i] = additionalAssets[i];
		}
	}
}
