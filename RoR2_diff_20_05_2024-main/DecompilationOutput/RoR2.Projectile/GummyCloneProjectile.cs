using System;
using EntityStates.GummyClone;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace RoR2.Projectile;

[RequireComponent(typeof(ProjectileController))]
public class GummyCloneProjectile : MonoBehaviour, IProjectileImpactBehavior
{
	[SerializeField]
	private int damageBoostCount = 10;

	[SerializeField]
	private int hpBoostCount = 10;

	[SerializeField]
	private float maxLifetime = 10f;

	private float stopwatch;

	private bool isAlive = true;

	public void OnProjectileImpact(ProjectileImpactInfo impactInfo)
	{
		if (isAlive)
		{
			SpawnGummyClone();
			isAlive = false;
		}
	}

	protected void FixedUpdate()
	{
		stopwatch += Time.fixedDeltaTime;
		if (NetworkServer.active && (!isAlive || stopwatch > maxLifetime))
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	public void SpawnGummyClone()
	{
		ProjectileController component = GetComponent<ProjectileController>();
		if (!component || !component.owner)
		{
			return;
		}
		CharacterBody component2 = component.owner.GetComponent<CharacterBody>();
		if (!component2)
		{
			return;
		}
		CharacterMaster characterMaster = component2.master;
		if (!characterMaster || characterMaster.IsDeployableLimited(DeployableSlot.GummyClone))
		{
			return;
		}
		MasterCopySpawnCard masterCopySpawnCard = MasterCopySpawnCard.FromMaster(characterMaster, copyItems: false, copyEquipment: false);
		if (!masterCopySpawnCard)
		{
			return;
		}
		masterCopySpawnCard.GiveItem(DLC1Content.Items.GummyCloneIdentifier);
		masterCopySpawnCard.GiveItem(RoR2Content.Items.BoostDamage, damageBoostCount);
		masterCopySpawnCard.GiveItem(RoR2Content.Items.BoostHp, hpBoostCount);
		Transform spawnOnTarget = base.transform;
		DirectorPlacementRule directorPlacementRule = new DirectorPlacementRule
		{
			spawnOnTarget = spawnOnTarget,
			placementMode = DirectorPlacementRule.PlacementMode.Direct
		};
		DirectorCore.GetMonsterSpawnDistance(DirectorCore.MonsterSpawnDistance.Close, out directorPlacementRule.minDistance, out directorPlacementRule.maxDistance);
		DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(masterCopySpawnCard, directorPlacementRule, new Xoroshiro128Plus(Run.instance.seed + (ulong)Run.instance.fixedTime));
		directorSpawnRequest.teamIndexOverride = characterMaster.teamIndex;
		directorSpawnRequest.ignoreTeamMemberLimit = true;
		directorSpawnRequest.summonerBodyObject = component2.gameObject;
		directorSpawnRequest.onSpawnedServer = (Action<SpawnCard.SpawnResult>)Delegate.Combine(directorSpawnRequest.onSpawnedServer, (Action<SpawnCard.SpawnResult>)delegate(SpawnCard.SpawnResult result)
		{
			CharacterMaster component3 = result.spawnedInstance.GetComponent<CharacterMaster>();
			Deployable deployable = result.spawnedInstance.AddComponent<Deployable>();
			characterMaster.AddDeployable(deployable, DeployableSlot.GummyClone);
			deployable.onUndeploy = deployable.onUndeploy ?? new UnityEvent();
			deployable.onUndeploy.AddListener(component3.TrueKill);
			GameObject bodyObject = component3.GetBodyObject();
			if ((bool)bodyObject)
			{
				EntityStateMachine[] components = bodyObject.GetComponents<EntityStateMachine>();
				foreach (EntityStateMachine entityStateMachine in components)
				{
					if (entityStateMachine.customName == "Body")
					{
						entityStateMachine.SetState(new GummyCloneSpawnState());
						break;
					}
				}
			}
		});
		DirectorCore.instance.TrySpawnObject(directorSpawnRequest);
		UnityEngine.Object.Destroy(masterCopySpawnCard);
	}
}
