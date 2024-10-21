using System;
using System.Linq;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.BeetleQueenMonster;

public class SummonEggs : BaseState
{
	public static float baseDuration = 3.5f;

	public static string attackSoundString;

	public static float randomRadius = 8f;

	public static GameObject spitPrefab;

	public static int maxSummonCount = 5;

	public static float summonInterval = 1f;

	private static float summonDuration = 3.26f;

	public static SpawnCard spawnCard;

	private Animator animator;

	private Transform modelTransform;

	private ChildLocator childLocator;

	private float duration;

	private float summonTimer;

	private int summonCount;

	private bool isSummoning;

	private BullseyeSearch enemySearch;

	public override void Reset()
	{
		base.Reset();
		animator = null;
		modelTransform = null;
		childLocator = null;
		duration = 0f;
		summonInterval = 0f;
		summonTimer = 0f;
		summonCount = 0;
		isSummoning = false;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		animator = GetModelAnimator();
		modelTransform = GetModelTransform();
		childLocator = modelTransform.GetComponent<ChildLocator>();
		duration = baseDuration;
		PlayCrossfade("Gesture", "SummonEggs", 0.5f);
		Util.PlaySound(attackSoundString, base.gameObject);
		if (NetworkServer.active)
		{
			enemySearch = new BullseyeSearch();
			enemySearch.filterByDistinctEntity = false;
			enemySearch.filterByLoS = false;
			enemySearch.maxDistanceFilter = float.PositiveInfinity;
			enemySearch.minDistanceFilter = 0f;
			enemySearch.minAngleFilter = 0f;
			enemySearch.maxAngleFilter = 180f;
			enemySearch.teamMaskFilter = TeamMask.GetEnemyTeams(GetTeam());
			enemySearch.sortMode = BullseyeSearch.SortMode.Distance;
			enemySearch.viewer = base.characterBody;
		}
	}

	private void SummonEgg()
	{
		Vector3 searchOrigin = GetAimRay().origin;
		if ((bool)base.inputBank && base.inputBank.GetAimRaycast(float.PositiveInfinity, out var hitInfo))
		{
			searchOrigin = hitInfo.point;
		}
		if (enemySearch == null)
		{
			return;
		}
		enemySearch.searchOrigin = searchOrigin;
		enemySearch.RefreshCandidates();
		HurtBox hurtBox = enemySearch.GetResults().FirstOrDefault();
		Transform transform = (((bool)hurtBox && (bool)hurtBox.healthComponent) ? hurtBox.healthComponent.body.coreTransform : base.characterBody.coreTransform);
		if (!transform)
		{
			return;
		}
		DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(spawnCard, new DirectorPlacementRule
		{
			placementMode = DirectorPlacementRule.PlacementMode.Approximate,
			minDistance = 3f,
			maxDistance = 20f,
			spawnOnTarget = transform
		}, RoR2Application.rng);
		directorSpawnRequest.summonerBodyObject = base.gameObject;
		directorSpawnRequest.onSpawnedServer = (Action<SpawnCard.SpawnResult>)Delegate.Combine(directorSpawnRequest.onSpawnedServer, (Action<SpawnCard.SpawnResult>)delegate(SpawnCard.SpawnResult spawnResult)
		{
			if (spawnResult.success && (bool)spawnResult.spawnedInstance && (bool)base.characterBody)
			{
				Inventory component = spawnResult.spawnedInstance.GetComponent<Inventory>();
				if ((bool)component)
				{
					component.CopyEquipmentFrom(base.characterBody.inventory);
					if (base.characterBody.inventory.GetItemCount(RoR2Content.Items.Ghost) > 0)
					{
						component.GiveItem(RoR2Content.Items.Ghost);
						component.GiveItem(RoR2Content.Items.HealthDecay, 30);
						component.GiveItem(RoR2Content.Items.BoostDamage, 150);
					}
				}
			}
		});
		DirectorCore.instance?.TrySpawnObject(directorSpawnRequest);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		bool flag = animator.GetFloat("SummonEggs.active") > 0.9f;
		if (flag && !isSummoning)
		{
			string childName = "Mouth";
			Transform transform = childLocator.FindChild(childName);
			if ((bool)transform)
			{
				if (!EffectManager.ShouldUsePooledEffect(spitPrefab))
				{
					UnityEngine.Object.Instantiate(spitPrefab, transform);
				}
				else
				{
					EffectManager.GetAndActivatePooledEffect(spitPrefab, transform, inResetLocal: true);
				}
			}
		}
		if (isSummoning)
		{
			summonTimer += GetDeltaTime();
			if (NetworkServer.active && summonTimer > 0f && summonCount < maxSummonCount)
			{
				summonCount++;
				summonTimer -= summonInterval;
				SummonEgg();
			}
		}
		isSummoning = flag;
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
