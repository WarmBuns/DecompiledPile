using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Toolbot;

public class ToolbotDash : BaseCharacterMain
{
	[SerializeField]
	public float baseDuration;

	[SerializeField]
	public float speedMultiplier;

	public static float chargeDamageCoefficient;

	public static float awayForceMagnitude;

	public static float upwardForceMagnitude;

	public static GameObject impactEffectPrefab;

	public static float hitPauseDuration;

	public static string impactSoundString;

	public static float recoilAmplitude;

	public static string startSoundString;

	public static string endSoundString;

	public static GameObject knockbackEffectPrefab;

	public static float knockbackDamageCoefficient;

	public static float massThresholdForKnockback;

	public static float knockbackForce;

	[SerializeField]
	public GameObject startEffectPrefab;

	[SerializeField]
	public GameObject endEffectPrefab;

	private uint soundID;

	private float duration;

	private float hitPauseTimer;

	private Vector3 idealDirection;

	private OverlapAttack attack;

	private bool inHitPause;

	private List<HurtBox> victimsStruck = new List<HurtBox>();

	private static int BoxModeExitStateHash = Animator.StringToHash("BoxModeExit");

	private static int EmptyStateHash = Animator.StringToHash("Empty");

	private static int aimWeightStateHash = Animator.StringToHash("aimWeight");

	private static int BoxModeImpactStateHash = Animator.StringToHash("BoxModeImpact");

	private static int BoxModeImpactParamHash = Animator.StringToHash("BoxModeImpact.playbackRate");

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
		soundID = Util.PlaySound(startSoundString, base.gameObject);
		PlayCrossfade("Body", "BoxModeEnter", 0.1f);
		PlayCrossfade("Stance, Override", "PutAwayGun", 0.1f);
		base.modelAnimator.SetFloat("aimWeight", 0f);
		if (NetworkServer.active)
		{
			base.characterBody.AddBuff(RoR2Content.Buffs.ArmorBoost);
		}
		HitBoxGroup hitBoxGroup = null;
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			hitBoxGroup = Array.Find(modelTransform.GetComponents<HitBoxGroup>(), (HitBoxGroup element) => element.groupName == "Charge");
		}
		attack = new OverlapAttack();
		attack.attacker = base.gameObject;
		attack.inflictor = base.gameObject;
		attack.teamIndex = GetTeam();
		attack.damage = chargeDamageCoefficient * damageStat;
		attack.hitEffectPrefab = impactEffectPrefab;
		attack.forceVector = Vector3.up * upwardForceMagnitude;
		attack.pushAwayForce = awayForceMagnitude;
		attack.hitBoxGroup = hitBoxGroup;
		attack.isCrit = RollCrit();
	}

	public override void OnExit()
	{
		AkSoundEngine.StopPlayingID(soundID);
		Util.PlaySound(endSoundString, base.gameObject);
		if (!outer.destroying && (bool)base.characterBody)
		{
			if ((bool)endEffectPrefab)
			{
				EffectManager.SpawnEffect(endEffectPrefab, new EffectData
				{
					origin = base.characterBody.corePosition
				}, transmit: false);
			}
			PlayAnimation("Body", BoxModeExitStateHash);
			PlayCrossfade("Stance, Override", EmptyStateHash, 0.1f);
			base.characterBody.isSprinting = false;
			if (NetworkServer.active)
			{
				base.characterBody.RemoveBuff(RoR2Content.Buffs.ArmorBoost);
			}
		}
		if ((bool)base.characterMotor && !base.characterMotor.disableAirControlUntilCollision)
		{
			base.characterMotor.velocity += GetIdealVelocity();
		}
		if ((bool)base.modelLocator)
		{
			base.modelLocator.normalizeToFloor = false;
		}
		base.modelAnimator.SetFloat(aimWeightStateHash, 1f);
		base.OnExit();
	}

	private float GetDamageBoostFromSpeed()
	{
		return Mathf.Max(1f, base.characterBody.moveSpeed / base.characterBody.baseMoveSpeed);
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

	private Vector3 GetIdealVelocity()
	{
		return base.characterDirection.forward * base.characterBody.moveSpeed * speedMultiplier;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration)
		{
			outer.SetNextStateToMain();
		}
		else
		{
			if (!base.isAuthority)
			{
				return;
			}
			if ((bool)base.characterBody)
			{
				base.characterBody.isSprinting = true;
			}
			if ((bool)base.skillLocator.special && base.inputBank.skill4.down)
			{
				base.skillLocator.special.ExecuteIfReady();
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
				attack.damage = damageStat * (chargeDamageCoefficient * GetDamageBoostFromSpeed());
				if (!attack.Fire(victimsStruck))
				{
					return;
				}
				Util.PlaySound(impactSoundString, base.gameObject);
				inHitPause = true;
				hitPauseTimer = hitPauseDuration;
				AddRecoil(-0.5f * recoilAmplitude, -0.5f * recoilAmplitude, -0.5f * recoilAmplitude, 0.5f * recoilAmplitude);
				PlayAnimation("Gesture, Additive", BoxModeImpactStateHash, BoxModeImpactParamHash, hitPauseDuration);
				for (int i = 0; i < victimsStruck.Count; i++)
				{
					float num = 0f;
					HurtBox hurtBox = victimsStruck[i];
					if (!hurtBox.healthComponent)
					{
						continue;
					}
					CharacterMotor component = hurtBox.healthComponent.GetComponent<CharacterMotor>();
					if ((bool)component)
					{
						num = component.mass;
					}
					else
					{
						Rigidbody component2 = hurtBox.healthComponent.GetComponent<Rigidbody>();
						if ((bool)component2)
						{
							num = component2.mass;
						}
					}
					if (num >= massThresholdForKnockback)
					{
						outer.SetNextState(new ToolbotDashImpact
						{
							victimHealthComponent = hurtBox.healthComponent,
							idealDirection = idealDirection,
							damageBoostFromSpeed = GetDamageBoostFromSpeed(),
							isCrit = attack.isCrit
						});
						break;
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
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Frozen;
	}
}
