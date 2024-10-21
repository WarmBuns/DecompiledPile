using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace EntityStates.Engi.EngiMissilePainter;

public class Fire : BaseEngiMissilePainterState
{
	public static float baseDurationPerMissile;

	public static float damageCoefficient;

	public static GameObject projectilePrefab;

	public static GameObject muzzleflashEffectPrefab;

	public List<HurtBox> targetsList;

	private int fireIndex;

	private float durationPerMissile;

	private float stopwatch;

	private static int IdleHarpoonsStateHash = Animator.StringToHash("IdleHarpoons");

	private static int FireHarpoonStateHash = Animator.StringToHash("FireHarpoon");

	public override void OnEnter()
	{
		base.OnEnter();
		durationPerMissile = baseDurationPerMissile / attackSpeedStat;
		PlayAnimation("Gesture, Additive", IdleHarpoonsStateHash);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		bool flag = false;
		if (base.isAuthority)
		{
			stopwatch += GetDeltaTime();
			if (stopwatch >= durationPerMissile)
			{
				stopwatch -= durationPerMissile;
				while (fireIndex < targetsList.Count)
				{
					HurtBox hurtBox = targetsList[fireIndex++];
					if (!hurtBox.healthComponent || !hurtBox.healthComponent.alive)
					{
						base.activatorSkillSlot.AddOneStock();
						continue;
					}
					string text = ((fireIndex % 2 == 0) ? "MuzzleLeft" : "MuzzleRight");
					Vector3 position = base.inputBank.aimOrigin;
					Transform transform = FindModelChild(text);
					if (transform != null)
					{
						position = transform.position;
					}
					EffectManager.SimpleMuzzleFlash(muzzleflashEffectPrefab, base.gameObject, text, transmit: true);
					FireMissile(hurtBox, position);
					flag = true;
					break;
				}
				if (fireIndex >= targetsList.Count)
				{
					outer.SetNextState(new Finish());
				}
			}
		}
		if (flag)
		{
			PlayAnimation((fireIndex % 2 == 0) ? "Gesture Left Cannon, Additive" : "Gesture Right Cannon, Additive", FireHarpoonStateHash);
		}
	}

	private void FireMissile(HurtBox target, Vector3 position)
	{
		MissileUtils.FireMissile(base.inputBank.aimOrigin, base.characterBody, default(ProcChainMask), target.gameObject, damageStat * damageCoefficient, RollCrit(), projectilePrefab, DamageColorIndex.Default, Vector3.up, 0f, addMissileProc: false);
	}

	public override void OnExit()
	{
		base.OnExit();
		PlayCrossfade("Gesture, Additive", "ExitHarpoons", 0.1f);
	}
}
