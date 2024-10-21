using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Mage;

public class FlyUpState : MageCharacterMain
{
	public static GameObject blinkPrefab;

	public static float duration = 0.3f;

	public static string beginSoundString;

	public static string endSoundString;

	public static AnimationCurve speedCoefficientCurve;

	public static GameObject muzzleflashEffect;

	public static float blastAttackRadius;

	public static float blastAttackDamageCoefficient;

	public static float blastAttackProcCoefficient;

	public static float blastAttackForce;

	private Vector3 flyVector = Vector3.zero;

	private Transform modelTransform;

	private CharacterModel characterModel;

	private HurtBoxGroup hurtboxGroup;

	private Vector3 blastPosition;

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(beginSoundString, base.gameObject);
		modelTransform = GetModelTransform();
		flyVector = Vector3.up;
		CreateBlinkEffect(Util.GetCorePosition(base.gameObject));
		PlayCrossfade("Body", "FlyUp", "FlyUp.playbackRate", duration, 0.1f);
		base.characterMotor.Motor.ForceUnground();
		base.characterMotor.velocity = Vector3.zero;
		EffectManager.SimpleMuzzleFlash(muzzleflashEffect, base.gameObject, "MuzzleLeft", transmit: false);
		EffectManager.SimpleMuzzleFlash(muzzleflashEffect, base.gameObject, "MuzzleRight", transmit: false);
		if (base.isAuthority)
		{
			blastPosition = base.characterBody.corePosition;
		}
		if (NetworkServer.active)
		{
			BlastAttack obj = new BlastAttack
			{
				radius = blastAttackRadius,
				procCoefficient = blastAttackProcCoefficient,
				position = blastPosition,
				attacker = base.gameObject,
				crit = Util.CheckRoll(base.characterBody.crit, base.characterBody.master),
				baseDamage = base.characterBody.damage * blastAttackDamageCoefficient,
				falloffModel = BlastAttack.FalloffModel.None,
				baseForce = blastAttackForce
			};
			obj.teamIndex = TeamComponent.GetObjectTeam(obj.attacker);
			obj.damageType = DamageType.Stun1s;
			obj.attackerFiltering = AttackerFiltering.NeverHitSelf;
			obj.Fire();
		}
	}

	public override void OnSerialize(NetworkWriter writer)
	{
		base.OnSerialize(writer);
		writer.Write(blastPosition);
	}

	public override void OnDeserialize(NetworkReader reader)
	{
		base.OnDeserialize(reader);
		blastPosition = reader.ReadVector3();
	}

	public override void HandleMovements()
	{
		base.HandleMovements();
		base.characterMotor.rootMotion += flyVector * (moveSpeedStat * speedCoefficientCurve.Evaluate(base.fixedAge / duration) * GetDeltaTime());
		base.characterMotor.velocity.y = 0f;
	}

	protected override void UpdateAnimationParameters()
	{
		base.UpdateAnimationParameters();
	}

	private void CreateBlinkEffect(Vector3 origin)
	{
		EffectData effectData = new EffectData();
		effectData.rotation = Util.QuaternionSafeLookRotation(flyVector);
		effectData.origin = origin;
		EffectManager.SpawnEffect(blinkPrefab, effectData, transmit: false);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override void OnExit()
	{
		if (!outer.destroying)
		{
			Util.PlaySound(endSoundString, base.gameObject);
		}
		base.OnExit();
	}
}
