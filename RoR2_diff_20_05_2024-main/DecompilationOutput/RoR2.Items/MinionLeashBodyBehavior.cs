using System.Collections;
using System.Collections.Generic;
using RoR2.Navigation;
using UnityEngine;

namespace RoR2.Items;

public class MinionLeashBodyBehavior : BaseItemBodyBehavior
{
	public const float leashDistSq = 160000f;

	public const float teleportDelayTime = 10f;

	public const float minTeleportDistance = 10f;

	public const float maxTeleportDistance = 40f;

	private GameObject helperPrefab;

	private RigidbodyMotor rigidbodyMotor;

	private float teleportAttemptTimer = 10f;

	private float currentTeleportDelayTimer;

	[ItemDefAssociation(useOnServer = true, useOnClient = true)]
	private static ItemDef GetItemDef()
	{
		return RoR2Content.Items.MinionLeash;
	}

	private void OnEnable()
	{
		if (SceneInfo.instance.sceneDef.cachedName == "meridian")
		{
			MeridianEventTriggerInteraction.MeridianEventStart.OnMeridianEventStart += StartTeleporting;
		}
	}

	private void OnDisable()
	{
		if (SceneInfo.instance.sceneDef.cachedName == "meridian")
		{
			MeridianEventTriggerInteraction.MeridianEventStart.OnMeridianEventStart -= StartTeleporting;
		}
	}

	public void Start()
	{
		if (base.body.hasEffectiveAuthority)
		{
			helperPrefab = LegacyResourcesAPI.Load<GameObject>("SpawnCards/HelperPrefab");
			rigidbodyMotor = GetComponent<RigidbodyMotor>();
		}
	}

	private void FixedUpdate()
	{
		if (!base.body.hasEffectiveAuthority)
		{
			return;
		}
		CharacterMaster master = base.body.master;
		CharacterMaster characterMaster = (master ? master.minionOwnership.ownerMaster : null);
		CharacterBody characterBody = (characterMaster ? characterMaster.GetBody() : null);
		if (!characterBody)
		{
			return;
		}
		Vector3 corePosition = characterBody.corePosition;
		Vector3 corePosition2 = base.body.corePosition;
		if (((!base.body.characterMotor || !(base.body.characterMotor.walkSpeed > 0f)) && (!rigidbodyMotor || !(base.body.moveSpeed > 0f))) || !((corePosition2 - corePosition).sqrMagnitude > 160000f))
		{
			return;
		}
		teleportAttemptTimer -= Time.fixedDeltaTime;
		if (!(teleportAttemptTimer <= 0f))
		{
			return;
		}
		teleportAttemptTimer = 10f;
		SpawnCard spawnCard = ScriptableObject.CreateInstance<SpawnCard>();
		spawnCard.hullSize = base.body.hullClassification;
		spawnCard.nodeGraphType = (base.body.isFlying ? MapNodeGroup.GraphType.Air : MapNodeGroup.GraphType.Ground);
		spawnCard.prefab = helperPrefab;
		GameObject gameObject = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(spawnCard, new DirectorPlacementRule
		{
			placementMode = DirectorPlacementRule.PlacementMode.Approximate,
			position = corePosition,
			minDistance = 10f,
			maxDistance = 40f
		}, RoR2Application.rng));
		if ((bool)gameObject)
		{
			Vector3 position = gameObject.transform.position;
			if ((position - corePosition).sqrMagnitude < 160000f)
			{
				TeleportHelper.TeleportBody(base.body, position);
				GameObject teleportEffectPrefab = Run.instance.GetTeleportEffectPrefab(base.body.gameObject);
				if ((bool)teleportEffectPrefab)
				{
					EffectManager.SimpleEffect(teleportEffectPrefab, position, Quaternion.identity, transmit: true);
				}
				Object.Destroy(gameObject);
			}
		}
		Object.Destroy(spawnCard);
	}

	private void StartTeleporting()
	{
		CharacterMaster master = base.body.master;
		CharacterMaster characterMaster = (master ? master.minionOwnership.ownerMaster : null);
		CharacterBody characterBody = (characterMaster ? characterMaster.GetBody() : null);
		if (base.body.hasEffectiveAuthority && (bool)characterBody && (((bool)base.body.characterMotor && base.body.characterMotor.walkSpeed > 0f) || ((bool)rigidbodyMotor && base.body.moveSpeed > 0f)))
		{
			StartCoroutine(Teleport());
		}
	}

	private IEnumerator Teleport()
	{
		currentTeleportDelayTimer = 0f;
		while (currentTeleportDelayTimer < 10f)
		{
			currentTeleportDelayTimer += Time.deltaTime;
			yield return null;
		}
		CharacterMaster master = base.body.master;
		CharacterMaster characterMaster = (master ? master.minionOwnership.ownerMaster : null);
		CharacterBody ownerBody = (characterMaster ? characterMaster.GetBody() : null);
		NodeGraph nodeGraph = SceneInfo.instance.GetNodeGraph(MapNodeGroup.GraphType.Ground);
		if (!ownerBody)
		{
			yield break;
		}
		List<NodeGraph.NodeIndex> list = nodeGraph.FindNodesInRangeWithFlagConditions(ownerBody.transform.position, 3f, 20f, HullMask.None, NodeFlags.None, NodeFlags.NoCharacterSpawn, preventOverhead: false);
		while (list.Count == 0)
		{
			yield return new WaitForSeconds(1f);
			list = nodeGraph.FindNodesInRangeWithFlagConditions(ownerBody.transform.position, 3f, 20f, HullMask.None, NodeFlags.None, NodeFlags.NoCharacterSpawn, preventOverhead: false);
		}
		while (list.Count > 0)
		{
			int index = Random.Range(0, list.Count);
			NodeGraph.NodeIndex nodeIndex = list[index];
			if (nodeGraph.GetNodePosition(nodeIndex, out var position))
			{
				TeleportHelper.TeleportBody(base.body, position);
				GameObject teleportEffectPrefab = Run.instance.GetTeleportEffectPrefab(base.body.gameObject);
				if ((bool)teleportEffectPrefab)
				{
					EffectManager.SimpleEffect(teleportEffectPrefab, position, Quaternion.identity, transmit: true);
				}
				break;
			}
		}
	}
}
