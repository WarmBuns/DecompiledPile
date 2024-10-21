using System;
using UnityEngine;

namespace RoR2;

public class LowerHealthHigherDamageEffectUpdater : MonoBehaviour
{
	private CharacterBody body;

	public GameObject rageCrystalEffect;

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
		if (itemBehaviorData.itemIndex == DLC2Content.Items.LowerHealthHigherDamage.itemIndex)
		{
			UpdateLanternFlameEffect((int)itemBehaviorData.floatValue);
		}
	}

	public void UpdateLanternFlameEffect(int on)
	{
		if (on != 0)
		{
			rageCrystalEffect.SetActive(value: true);
			Util.PlaySound("Play_item_proc_lowerHealthHigherDamage_active_loop", base.gameObject);
		}
		else
		{
			rageCrystalEffect.SetActive(value: false);
			Util.PlaySound("Stop_item_proc_lowerHealthHigherDamage_active_loop", base.gameObject);
		}
	}
}
