using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RoR2.Orbs;

public class MissileVoidOrb : GenericDamageOrb
{
	public override void Begin()
	{
		speed = 75f;
		base.Begin();
	}

	protected override GameObject GetOrbEffect()
	{
		return Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/MissileVoid/MissileVoidOrbEffect.prefab").WaitForCompletion();
	}
}
