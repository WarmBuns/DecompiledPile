using UnityEngine;

namespace RoR2.Items;

public class NearbyDamageBonusBodyBehavior : BaseItemBodyBehavior
{
	private GameObject nearbyDamageBonusIndicator;

	private bool indicatorEnabled
	{
		get
		{
			return nearbyDamageBonusIndicator;
		}
		set
		{
			if (indicatorEnabled != value)
			{
				if (value)
				{
					GameObject original = LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/NearbyDamageBonusIndicator");
					nearbyDamageBonusIndicator = Object.Instantiate(original, base.body.corePosition, Quaternion.identity);
					nearbyDamageBonusIndicator.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(base.gameObject);
				}
				else
				{
					Object.Destroy(nearbyDamageBonusIndicator);
					nearbyDamageBonusIndicator = null;
				}
			}
		}
	}

	[ItemDefAssociation(useOnServer = true, useOnClient = false)]
	private static ItemDef GetItemDef()
	{
		return RoR2Content.Items.NearbyDamageBonus;
	}

	private void OnEnable()
	{
		indicatorEnabled = true;
	}

	private void OnDisable()
	{
		indicatorEnabled = false;
	}
}
