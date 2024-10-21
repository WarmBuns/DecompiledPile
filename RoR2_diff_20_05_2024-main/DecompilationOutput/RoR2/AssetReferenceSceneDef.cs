using System;
using UnityEngine.AddressableAssets;

namespace RoR2;

[Serializable]
public class AssetReferenceSceneDef : AssetReferenceT<SceneDef>
{
	public AssetReferenceSceneDef(string guid)
		: base(guid)
	{
	}
}
