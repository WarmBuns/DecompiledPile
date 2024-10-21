using System;
using System.Collections.Generic;
using System.Linq;
using RoR2;
using RoR2.CharacterAI;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.VoidInfestor;

public class Infest : BaseState
{
	public static GameObject enterEffectPrefab;

	public static GameObject successfulInfestEffectPrefab;

	public static GameObject infestVfxPrefab;

	public static string enterSoundString;

	public static float searchMaxDistance;

	public static float searchMaxAngle;

	public static float velocityInitialSpeed;

	public static float velocityTurnRate;

	public static float infestDamageCoefficient;

	private Transform targetTransform;

	private GameObject infestVfxInstance;

	private OverlapAttack attack;

	private List<HurtBox> victimsStruck = new List<HurtBox>();

	private static int InfestStateHash = Animator.StringToHash("Infest");

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation("Base", InfestStateHash);
		Util.PlaySound(enterSoundString, base.gameObject);
		if ((bool)enterEffectPrefab)
		{
			EffectManager.SimpleImpactEffect(enterEffectPrefab, base.characterBody.corePosition, Vector3.up, transmit: false);
		}
		if ((bool)infestVfxPrefab)
		{
			infestVfxInstance = UnityEngine.Object.Instantiate(infestVfxPrefab, base.transform.position, Quaternion.identity);
			infestVfxInstance.transform.parent = base.transform;
		}
		HitBoxGroup hitBoxGroup = null;
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			hitBoxGroup = Array.Find(modelTransform.GetComponents<HitBoxGroup>(), (HitBoxGroup element) => element.groupName == "Infest");
		}
		attack = new OverlapAttack();
		attack.attacker = base.gameObject;
		attack.inflictor = base.gameObject;
		attack.teamIndex = GetTeam();
		attack.damage = infestDamageCoefficient * damageStat;
		attack.hitEffectPrefab = null;
		attack.hitBoxGroup = hitBoxGroup;
		attack.isCrit = RollCrit();
		attack.damageType = DamageType.Stun1s;
		attack.damageColorIndex = DamageColorIndex.Void;
		BullseyeSearch bullseyeSearch = new BullseyeSearch();
		bullseyeSearch.viewer = base.characterBody;
		bullseyeSearch.teamMaskFilter = TeamMask.allButNeutral;
		bullseyeSearch.teamMaskFilter.RemoveTeam(base.characterBody.teamComponent.teamIndex);
		bullseyeSearch.sortMode = BullseyeSearch.SortMode.Distance;
		bullseyeSearch.minDistanceFilter = 0f;
		bullseyeSearch.maxDistanceFilter = searchMaxDistance;
		bullseyeSearch.searchOrigin = base.inputBank.aimOrigin;
		bullseyeSearch.searchDirection = base.inputBank.aimDirection;
		bullseyeSearch.maxAngleFilter = searchMaxAngle;
		bullseyeSearch.filterByLoS = true;
		bullseyeSearch.RefreshCandidates();
		HurtBox hurtBox = bullseyeSearch.GetResults().FirstOrDefault();
		if ((bool)hurtBox)
		{
			targetTransform = hurtBox.transform;
			if ((bool)base.characterMotor)
			{
				Vector3 position = targetTransform.position;
				float num = velocityInitialSpeed;
				Vector3 vector = position - base.transform.position;
				Vector2 vector2 = new Vector2(vector.x, vector.z);
				float magnitude = vector2.magnitude;
				float y = Trajectory.CalculateInitialYSpeed(magnitude / num, vector.y);
				Vector3 vector3 = new Vector3(vector2.x / magnitude * num, y, vector2.y / magnitude * num);
				base.characterMotor.velocity = vector3;
				base.characterMotor.disableAirControlUntilCollision = true;
				base.characterMotor.Motor.ForceUnground();
				base.characterDirection.forward = vector3;
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if ((bool)targetTransform && (bool)base.characterMotor)
		{
			Vector3 target = targetTransform.position - base.transform.position;
			Vector3 velocity = base.characterMotor.velocity;
			velocity = Vector3.RotateTowards(velocity, target, velocityTurnRate * GetDeltaTime() * (MathF.PI / 180f), 0f);
			base.characterMotor.velocity = velocity;
			if (NetworkServer.active && attack.Fire(victimsStruck))
			{
				for (int i = 0; i < victimsStruck.Count; i++)
				{
					HealthComponent obj = victimsStruck[i].healthComponent;
					CharacterBody body = obj.body;
					CharacterMaster master = body.master;
					if (obj.alive && master != null && !body.isPlayerControlled && !body.bodyFlags.HasFlag(CharacterBody.BodyFlags.Mechanical))
					{
						master.teamIndex = TeamIndex.Void;
						body.teamComponent.teamIndex = TeamIndex.Void;
						body.inventory.SetEquipmentIndex(DLC1Content.Elites.Void.eliteEquipmentDef.equipmentIndex);
						BaseAI component = master.GetComponent<BaseAI>();
						if ((bool)component)
						{
							component.enemyAttention = 0f;
							component.ForceAcquireNearestEnemyIfNoCurrentEnemy();
						}
						base.healthComponent.Suicide();
						if ((bool)successfulInfestEffectPrefab)
						{
							EffectManager.SimpleImpactEffect(successfulInfestEffectPrefab, base.transform.position, Vector3.up, transmit: false);
						}
						break;
					}
				}
			}
		}
		if ((bool)base.characterDirection)
		{
			base.characterDirection.moveVector = base.characterMotor.velocity;
		}
		if (base.isAuthority && (bool)base.characterMotor && base.characterMotor.isGrounded && base.fixedAge > 0.1f)
		{
			outer.SetNextStateToMain();
		}
	}

	public override void OnExit()
	{
		if ((bool)infestVfxInstance)
		{
			EntityState.Destroy(infestVfxInstance);
		}
		base.OnExit();
	}
}
