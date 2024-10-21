using System;
using UnityEngine;

namespace RoR2;

public class DelayedDamageEffectUpdater : MonoBehaviour
{
	private CharacterBody body;

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
		if (itemBehaviorData.itemIndex == DLC2Content.Items.DelayedDamage.itemIndex)
		{
			SpawnDelayedDamageEffect();
		}
	}

	public void SpawnDelayedDamageEffect()
	{
		UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/DelayedDamageIndicator"), body.mainHurtBox ? body.mainHurtBox.transform.position : body.transform.position, Quaternion.identity, body.mainHurtBox ? body.mainHurtBox.transform : body.transform);
	}
}
