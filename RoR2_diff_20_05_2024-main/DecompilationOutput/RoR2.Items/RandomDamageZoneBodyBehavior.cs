using System.Collections.Generic;
using RoR2.Navigation;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Items;

public class RandomDamageZoneBodyBehavior : BaseItemBodyBehavior
{
	private static readonly float wardDuration = 25f;

	private static readonly float wardRespawnRetryTime = 1f;

	private static readonly float baseWardRadius = 16f;

	private static readonly float wardRadiusMultiplierPerStack = 1.5f;

	private float wardResummonCooldown;

	[ItemDefAssociation(useOnServer = true, useOnClient = false)]
	private static ItemDef GetItemDef()
	{
		return RoR2Content.Items.RandomDamageZone;
	}

	private void FixedUpdate()
	{
		CharacterMaster master = base.body.master;
		if (!master || !master.IsDeployableSlotAvailable(DeployableSlot.PowerWard))
		{
			return;
		}
		wardResummonCooldown -= Time.fixedDeltaTime;
		if (!(wardResummonCooldown <= 0f))
		{
			return;
		}
		wardResummonCooldown = wardRespawnRetryTime;
		if (master.IsDeployableSlotAvailable(DeployableSlot.PowerWard))
		{
			Vector3? vector = FindWardSpawnPosition(base.body.corePosition);
			if (vector.HasValue)
			{
				GameObject gameObject = Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/DamageZoneWard"), vector.Value, Quaternion.identity);
				Util.PlaySound("Play_randomDamageZone_appear", gameObject.gameObject);
				gameObject.GetComponent<TeamFilter>().teamIndex = TeamIndex.None;
				BuffWard component = gameObject.GetComponent<BuffWard>();
				component.Networkradius = baseWardRadius * Mathf.Pow(wardRadiusMultiplierPerStack, stack - 1);
				component.expireDuration = wardDuration;
				NetworkServer.Spawn(gameObject);
				Deployable component2 = gameObject.GetComponent<Deployable>();
				master.AddDeployable(component2, DeployableSlot.PowerWard);
			}
		}
	}

	private Vector3? FindWardSpawnPosition(Vector3 ownerPosition)
	{
		if (!SceneInfo.instance)
		{
			return null;
		}
		NodeGraph groundNodes = SceneInfo.instance.groundNodes;
		if (!groundNodes)
		{
			return null;
		}
		List<NodeGraph.NodeIndex> list = groundNodes.FindNodesInRange(ownerPosition, 0f, 50f, HullMask.None);
		if (list.Count > 0 && groundNodes.GetNodePosition(list[(int)Random.Range(0f, list.Count)], out var position))
		{
			return position;
		}
		return null;
	}
}
