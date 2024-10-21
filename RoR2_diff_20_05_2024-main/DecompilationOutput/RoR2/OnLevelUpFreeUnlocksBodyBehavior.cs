using UnityEngine;

namespace RoR2;

public class OnLevelUpFreeUnlocksBodyBehavior : MonoBehaviour
{
	public ParticleSystem onLevelUpFreeUnlocksFire;

	private void OnEnable()
	{
		GlobalEventManager.onCharacterLevelUp += BurstOfFire;
	}

	private void OnDisable()
	{
		GlobalEventManager.onCharacterLevelUp -= BurstOfFire;
	}

	private void BurstOfFire(CharacterBody characterBody)
	{
		ParticleSystem.EmissionModule emission = onLevelUpFreeUnlocksFire.emission;
		emission.rateOverTime = 50f;
		emission.rateOverTime = 5f;
		EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/OnLevelUpFreeUnlockDroneFlash"), new EffectData
		{
			origin = base.gameObject.transform.position
		}, transmit: true);
		Util.PlaySound("Play_item_proc_onLevelUpFreeUnlock_activate", base.gameObject);
	}
}
