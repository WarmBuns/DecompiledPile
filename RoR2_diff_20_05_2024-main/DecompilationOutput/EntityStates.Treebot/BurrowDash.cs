using System;
using RoR2;
using UnityEngine;

namespace EntityStates.Treebot;

public class BurrowDash : BaseCharacterMain
{
	[SerializeField]
	public float baseDuration;

	[SerializeField]
	public static AnimationCurve speedMultiplier;

	public static float chargeDamageCoefficient;

	public static GameObject impactEffectPrefab;

	public static GameObject burrowLoopEffectPrefab;

	public static float hitPauseDuration;

	public static float timeBeforeExitToPlayExitAnimation;

	public static string impactSoundString;

	public static string startSoundString;

	public static string endSoundString;

	public static float healPercent;

	public static bool resetDurationOnImpact;

	[SerializeField]
	public GameObject startEffectPrefab;

	[SerializeField]
	public GameObject endEffectPrefab;

	private float duration;

	private float hitPauseTimer;

	private Vector3 idealDirection;

	private OverlapAttack attack;

	private ChildLocator childLocator;

	private bool inHitPause;

	private bool beginPlayingExitAnimation;

	private Transform modelTransform;

	private GameObject burrowLoopEffectInstance;

	private int originalLayer;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration;
		if (base.isAuthority)
		{
			if ((bool)base.inputBank)
			{
				idealDirection = base.inputBank.aimDirection;
				idealDirection.y = 0f;
			}
			UpdateDirection();
		}
		if ((bool)base.modelLocator)
		{
			base.modelLocator.normalizeToFloor = true;
		}
		if ((bool)startEffectPrefab && (bool)base.characterBody)
		{
			EffectManager.SpawnEffect(startEffectPrefab, new EffectData
			{
				origin = base.characterBody.corePosition
			}, transmit: false);
		}
		if ((bool)base.characterDirection)
		{
			base.characterDirection.forward = idealDirection;
		}
		Util.PlaySound(startSoundString, base.gameObject);
		PlayCrossfade("Body", "BurrowEnter", 0.1f);
		HitBoxGroup hitBoxGroup = null;
		modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			hitBoxGroup = Array.Find(modelTransform.GetComponents<HitBoxGroup>(), (HitBoxGroup element) => element.groupName == "BurrowCharge");
			childLocator = modelTransform.GetComponent<ChildLocator>();
		}
		attack = new OverlapAttack();
		attack.attacker = base.gameObject;
		attack.inflictor = base.gameObject;
		attack.teamIndex = TeamComponent.GetObjectTeam(attack.attacker);
		attack.damage = chargeDamageCoefficient * damageStat;
		attack.hitEffectPrefab = impactEffectPrefab;
		attack.hitBoxGroup = hitBoxGroup;
		attack.damage = damageStat * chargeDamageCoefficient;
		attack.damageType = DamageType.Freeze2s;
		attack.procCoefficient = 1f;
		originalLayer = base.gameObject.layer;
		base.gameObject.layer = LayerIndex.debris.intVal;
		base.characterMotor.Motor.RebuildCollidableLayers();
		modelTransform.GetComponent<HurtBoxGroup>().hurtBoxesDeactivatorCounter++;
		base.characterBody.hideCrosshair = true;
		base.characterBody.isSprinting = true;
		if ((bool)childLocator)
		{
			Transform transform = childLocator.FindChild("BurrowCenter");
			if ((bool)transform && (bool)burrowLoopEffectPrefab)
			{
				burrowLoopEffectInstance = UnityEngine.Object.Instantiate(burrowLoopEffectPrefab, transform.position, transform.rotation);
				burrowLoopEffectInstance.transform.parent = transform;
			}
		}
	}

	public override void OnExit()
	{
		if ((bool)base.characterBody && !outer.destroying && (bool)endEffectPrefab)
		{
			EffectManager.SpawnEffect(endEffectPrefab, new EffectData
			{
				origin = base.characterBody.corePosition
			}, transmit: false);
		}
		Util.PlaySound(endSoundString, base.gameObject);
		base.gameObject.layer = originalLayer;
		base.characterMotor.Motor.RebuildCollidableLayers();
		modelTransform.GetComponent<HurtBoxGroup>().hurtBoxesDeactivatorCounter--;
		base.characterBody.hideCrosshair = false;
		if ((bool)burrowLoopEffectInstance)
		{
			EntityState.Destroy(burrowLoopEffectInstance);
		}
		Animator animator = GetModelAnimator();
		int layerIndex = animator.GetLayerIndex("Impact");
		if (layerIndex >= 0)
		{
			animator.SetLayerWeight(layerIndex, 2f);
			animator.PlayInFixedTime("LightImpact", layerIndex, 0f);
		}
		base.OnExit();
	}

	private void UpdateDirection()
	{
		if ((bool)base.inputBank)
		{
			Vector2 vector = Util.Vector3XZToVector2XY(base.inputBank.moveVector);
			if (vector != Vector2.zero)
			{
				vector.Normalize();
				idealDirection = new Vector3(vector.x, 0f, vector.y).normalized;
			}
		}
	}

	protected override void UpdateAnimationParameters()
	{
	}

	private Vector3 GetIdealVelocity()
	{
		return base.characterDirection.forward * base.characterBody.moveSpeed * speedMultiplier.Evaluate(base.fixedAge / duration);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration)
		{
			outer.SetNextStateToMain();
			return;
		}
		if (base.fixedAge >= duration - timeBeforeExitToPlayExitAnimation && !beginPlayingExitAnimation)
		{
			beginPlayingExitAnimation = true;
			PlayCrossfade("Body", "BurrowExit", 0.1f);
		}
		if (!base.isAuthority)
		{
			return;
		}
		UpdateDirection();
		if (!inHitPause)
		{
			if ((bool)base.characterDirection)
			{
				base.characterDirection.moveVector = idealDirection;
				if ((bool)base.characterMotor && !base.characterMotor.disableAirControlUntilCollision)
				{
					base.characterMotor.rootMotion += GetIdealVelocity() * GetDeltaTime();
				}
			}
			if (attack.Fire())
			{
				Util.PlaySound(impactSoundString, base.gameObject);
				inHitPause = true;
				hitPauseTimer = hitPauseDuration;
				if (healPercent > 0f)
				{
					base.healthComponent.HealFraction(healPercent, default(ProcChainMask));
					Util.PlaySound("Play_item_use_fruit", base.gameObject);
					EffectData effectData = new EffectData();
					effectData.origin = base.transform.position;
					effectData.SetNetworkedObjectReference(base.gameObject);
					EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/FruitHealEffect"), effectData, transmit: true);
				}
				if (resetDurationOnImpact)
				{
					base.fixedAge = 0f;
				}
				else
				{
					base.fixedAge -= hitPauseDuration;
				}
			}
		}
		else
		{
			base.characterMotor.velocity = Vector3.zero;
			hitPauseTimer -= GetDeltaTime();
			if (hitPauseTimer < 0f)
			{
				inHitPause = false;
			}
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Death;
	}
}
