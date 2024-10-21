using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class SproutOfLifeHealOrbSpawn : NetworkBehaviour
{
	private float seedOfLifeOrbsLeft = 10f;

	private float seedOfLifeOrbsTimer;

	private void Start()
	{
		Util.PlaySound("Play_item_use_healAndRevive_healPlantGrow", base.gameObject);
	}

	private void FixedUpdate()
	{
		if (seedOfLifeOrbsLeft > 0f)
		{
			seedOfLifeOrbsTimer -= Time.fixedDeltaTime;
			if (seedOfLifeOrbsTimer <= 0f && NetworkServer.active)
			{
				SproutSpawnHealOrb();
			}
		}
		else
		{
			EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/SproutOfLifeBurst"), new EffectData
			{
				origin = base.gameObject.transform.position
			}, transmit: true);
			Object.Destroy(base.gameObject);
		}
	}

	[Server]
	private void SproutSpawnHealOrb()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.SproutOfLifeHealOrbSpawn::SproutSpawnHealOrb()' called on client");
			return;
		}
		seedOfLifeOrbsTimer += 0.5f;
		float num = Mathf.Pow(4f, 0.25f);
		GameObject original = LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/HealPack");
		Vector3 vector = default(Vector3);
		vector = base.transform.position + new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f));
		GameObject obj = Object.Instantiate(original, vector, Random.rotation);
		TeamIndex teamIndex = TeamIndex.Player;
		TeamFilter component = obj.GetComponent<TeamFilter>();
		if ((bool)component)
		{
			component.teamIndex = teamIndex;
		}
		HealthPickup componentInChildren = obj.GetComponentInChildren<HealthPickup>();
		if ((bool)componentInChildren)
		{
			componentInChildren.flatHealing = 0f;
			componentInChildren.fractionalHealing = 0.02f;
		}
		obj.transform.localScale = new Vector3(num, num, num);
		NetworkServer.Spawn(obj);
		Util.PlaySound("Play_item_use_healAndRevive_healOrb_gather", base.gameObject);
		seedOfLifeOrbsLeft -= 1f;
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		bool result = default(bool);
		return result;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
	}

	public override void PreStartClient()
	{
	}
}
