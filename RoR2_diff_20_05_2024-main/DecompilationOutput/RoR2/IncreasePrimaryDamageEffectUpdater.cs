using System;
using UnityEngine;

namespace RoR2;

public class IncreasePrimaryDamageEffectUpdater : MonoBehaviour
{
	private CharacterBody body;

	[SerializeField]
	private MeshRenderer shotRingTop;

	[SerializeField]
	private MeshRenderer shotRingMiddle;

	[SerializeField]
	private MeshRenderer shotRingBottom;

	[SerializeField]
	private Material luminousEnergyMaterial;

	[SerializeField]
	private Material unlitEnergyMaterial;

	private void OnEnable()
	{
		body = GetComponentInParent<CharacterModel>().body;
		CharacterBody characterBody = body;
		characterBody.OnNetworkItemBehaviorUpdate = (Action<CharacterBody.NetworkItemBehaviorData>)Delegate.Combine(characterBody.OnNetworkItemBehaviorUpdate, new Action<CharacterBody.NetworkItemBehaviorData>(HandleNetworkItemUpdate));
	}

	private void OnDisable()
	{
		if ((bool)body)
		{
			CharacterBody characterBody = body;
			characterBody.OnNetworkItemBehaviorUpdate = (Action<CharacterBody.NetworkItemBehaviorData>)Delegate.Remove(characterBody.OnNetworkItemBehaviorUpdate, new Action<CharacterBody.NetworkItemBehaviorData>(HandleNetworkItemUpdate));
		}
	}

	private void HandleNetworkItemUpdate(CharacterBody.NetworkItemBehaviorData itemBehaviorData)
	{
		if (itemBehaviorData.itemIndex == DLC2Content.Items.IncreasePrimaryDamage.itemIndex)
		{
			LightUpRings((int)itemBehaviorData.floatValue);
		}
	}

	public void LightUpRings(int ringsToLight)
	{
		if (ringsToLight == 0)
		{
			shotRingBottom.material = unlitEnergyMaterial;
			shotRingMiddle.material = unlitEnergyMaterial;
			shotRingTop.material = unlitEnergyMaterial;
			return;
		}
		if (ringsToLight >= 1)
		{
			shotRingBottom.material = luminousEnergyMaterial;
		}
		if (ringsToLight >= 2)
		{
			shotRingMiddle.material = luminousEnergyMaterial;
		}
		if (ringsToLight >= 3)
		{
			shotRingTop.material = luminousEnergyMaterial;
		}
		EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/IncreasePrimaryDamageDisplayCrackle"), new EffectData
		{
			origin = base.gameObject.transform.position
		}, transmit: false);
	}
}
