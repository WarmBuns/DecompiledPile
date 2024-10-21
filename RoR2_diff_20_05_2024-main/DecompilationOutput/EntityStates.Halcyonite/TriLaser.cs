using RoR2;
using UnityEngine;

namespace EntityStates.Halcyonite;

public class TriLaser : BaseState
{
	public static GameObject effectPrefab;

	public static GameObject hitEffectPrefab;

	public static GameObject tracerEffectPrefab;

	public static float damageCoefficient;

	public static float blastRadius;

	public static float force;

	public static float minSpread;

	public static float maxSpread;

	public static int bulletCount;

	public static float baseDuration = 2f;

	public static string attackSoundString;

	public Vector3 laserDirection;

	private float duration;

	private Ray modifiedAimRay;

	public float fireBeamTimer = 0.5f;

	private int timesFired;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		fireBeamTimer += Time.deltaTime;
		if (timesFired < 3 && fireBeamTimer > 0.5f)
		{
			FireTriLaser();
			fireBeamTimer = 0f;
			timesFired++;
		}
		if (base.fixedAge >= duration && timesFired > 2 && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	private void FireTriLaser()
	{
		duration = baseDuration / attackSpeedStat;
		modifiedAimRay = GetAimRay();
		modifiedAimRay.direction = laserDirection;
		GetModelAnimator();
		Transform modelTransform = GetModelTransform();
		Util.PlaySound(attackSoundString, base.gameObject);
		string text = "MuzzleLaser";
		Util.PlaySound("Play_halcyonite_skill2_shoot", base.gameObject);
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(2f);
		}
		PlayCrossfade("FullBody Override", "TriLaserFire", "TriLaser.playbackRate", duration * 0.5f, 0.1f);
		if ((bool)effectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(effectPrefab, base.gameObject, text, transmit: false);
		}
		if (!base.isAuthority)
		{
			return;
		}
		float num = 1000f;
		Vector3 vector = modifiedAimRay.origin + modifiedAimRay.direction * num;
		if (Physics.Raycast(modifiedAimRay, out var hitInfo, num, LayerIndex.CommonMasks.laser))
		{
			vector = hitInfo.point;
		}
		BlastAttack blastAttack = new BlastAttack();
		blastAttack.attacker = base.gameObject;
		blastAttack.inflictor = base.gameObject;
		blastAttack.teamIndex = TeamComponent.GetObjectTeam(base.gameObject);
		blastAttack.baseDamage = damageStat * damageCoefficient;
		blastAttack.baseForce = force * 0.2f;
		blastAttack.position = vector;
		blastAttack.radius = blastRadius;
		blastAttack.falloffModel = BlastAttack.FalloffModel.SweetSpot;
		blastAttack.bonusForce = force * modifiedAimRay.direction;
		blastAttack.Fire();
		_ = modifiedAimRay.origin;
		if (!modelTransform)
		{
			return;
		}
		ChildLocator component = modelTransform.GetComponent<ChildLocator>();
		if ((bool)component)
		{
			int childIndex = component.FindChildIndex(text);
			if ((bool)tracerEffectPrefab)
			{
				EffectData effectData = new EffectData
				{
					origin = vector,
					start = modifiedAimRay.origin
				};
				effectData.SetChildLocatorTransformReference(base.gameObject, childIndex);
				EffectManager.SpawnEffect(tracerEffectPrefab, effectData, transmit: true);
				EffectManager.SpawnEffect(hitEffectPrefab, effectData, transmit: true);
			}
		}
	}

	public override void OnExit()
	{
		PlayCrossfade("FullBody Override", "TriLaserExit", "TriLaser.playbackRate", duration, 0.1f);
		base.OnExit();
	}
}
