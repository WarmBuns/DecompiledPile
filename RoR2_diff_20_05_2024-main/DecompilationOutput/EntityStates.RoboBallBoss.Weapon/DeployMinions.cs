using System.Collections.ObjectModel;
using RoR2;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace EntityStates.RoboBallBoss.Weapon;

public class DeployMinions : BaseState
{
	public static float baseDuration = 3.5f;

	public static string attackSoundString;

	public static string summonSoundString;

	public static int maxSummonCount = 5;

	public static float summonDuration = 3.26f;

	public static string summonMuzzleString;

	public static string spawnCard;

	private Animator animator;

	private Transform modelTransform;

	private ChildLocator childLocator;

	private float duration;

	private float summonInterval;

	private float summonTimer;

	private int summonCount;

	private bool isSummoning;

	public override void OnEnter()
	{
		base.OnEnter();
		animator = GetModelAnimator();
		modelTransform = GetModelTransform();
		childLocator = modelTransform.GetComponent<ChildLocator>();
		duration = baseDuration;
		PlayCrossfade("Gesture, Additive", "DeployMinions", "DeployMinions.playbackRate", duration, 0.1f);
		Util.PlaySound(attackSoundString, base.gameObject);
		summonInterval = summonDuration / (float)maxSummonCount;
	}

	private Transform FindTargetClosest(Vector3 point, TeamIndex enemyTeam)
	{
		ReadOnlyCollection<TeamComponent> teamMembers = TeamComponent.GetTeamMembers(enemyTeam);
		float num = 99999f;
		Transform result = null;
		for (int i = 0; i < teamMembers.Count; i++)
		{
			float num2 = Vector3.SqrMagnitude(teamMembers[i].transform.position - point);
			if (num2 < num)
			{
				num = num2;
				result = teamMembers[i].transform;
			}
		}
		return result;
	}

	private void SummonMinion()
	{
		if (!base.characterBody || !base.characterBody.master || base.characterBody.master.GetDeployableCount(DeployableSlot.RoboBallMini) >= base.characterBody.master.GetDeployableSameSlotLimit(DeployableSlot.RoboBallMini))
		{
			return;
		}
		Util.PlaySound(summonSoundString, base.gameObject);
		if (!NetworkServer.active)
		{
			return;
		}
		Vector3 position = FindModelChild(summonMuzzleString).position;
		DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(LegacyResourcesAPI.Load<SpawnCard>($"SpawnCards/CharacterSpawnCards/{spawnCard}"), new DirectorPlacementRule
		{
			placementMode = DirectorPlacementRule.PlacementMode.Direct,
			minDistance = 0f,
			maxDistance = 0f,
			position = position
		}, RoR2Application.rng);
		directorSpawnRequest.summonerBodyObject = base.gameObject;
		GameObject gameObject = DirectorCore.instance.TrySpawnObject(directorSpawnRequest);
		if ((bool)gameObject)
		{
			CharacterMaster component = gameObject.GetComponent<CharacterMaster>();
			Inventory component2 = gameObject.GetComponent<Inventory>();
			component2.SetEquipmentIndex(base.characterBody.inventory.currentEquipmentIndex);
			if (base.characterBody.inventory.GetItemCount(RoR2Content.Items.Ghost) > 0)
			{
				component2.GiveItem(RoR2Content.Items.Ghost);
				component2.GiveItem(RoR2Content.Items.HealthDecay, 30);
				component2.GiveItem(RoR2Content.Items.BoostDamage, 150);
			}
			Deployable deployable = gameObject.AddComponent<Deployable>();
			deployable.onUndeploy = new UnityEvent();
			deployable.onUndeploy.AddListener(component.TrueKill);
			base.characterBody.master.AddDeployable(deployable, DeployableSlot.RoboBallMini);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if ((bool)animator)
		{
			bool flag = animator.GetFloat("DeployMinions.active") > 0.9f;
			if (isSummoning)
			{
				summonTimer += GetDeltaTime();
				if (NetworkServer.active && summonTimer > 0f && summonCount < maxSummonCount)
				{
					summonCount++;
					summonTimer -= summonInterval;
					SummonMinion();
				}
			}
			isSummoning = flag;
		}
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
