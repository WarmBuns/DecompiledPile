using System;
using UnityEngine;

namespace RoR2;

public class GoldOnStageStartBehaviour : MonoBehaviour
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
		if (itemBehaviorData.itemIndex == DLC2Content.Items.GoldOnStageStart.itemIndex)
		{
			GiveWarBondsGold(itemBehaviorData.floatValue);
		}
	}

	private void GiveWarBondsGold(float na)
	{
		if (((body.inventory != null && body.inventory.GetItemCount(DLC2Content.Items.GoldOnStageStart) != 0) ? 1 : 0) > (false ? 1 : 0))
		{
			Util.PlaySound("Play_item_proc_goldOnStageStart", body.gameObject);
			EffectManager.SpawnEffect(CharacterBody.CommonAssets.goldOnStageStartEffect, new EffectData
			{
				origin = base.transform.position
			}, transmit: false);
		}
	}
}
