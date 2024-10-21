using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.TitanMonster;

public class RechargeRocks : BaseState
{
	public static float baseDuration = 3f;

	public static float baseRechargeDuration = 2f;

	public static GameObject effectPrefab;

	public static string attackSoundString;

	public static GameObject rockControllerPrefab;

	private int rocksFired;

	private float duration;

	private float stopwatch;

	private float rechargeStopwatch;

	private GameObject chargeEffect;

	public override void OnEnter()
	{
		base.OnEnter();
		stopwatch = 0f;
		duration = baseDuration / attackSpeedStat;
		Transform modelTransform = GetModelTransform();
		Util.PlaySound(attackSoundString, base.gameObject);
		PlayCrossfade("Body", "RechargeRocks", "RechargeRocks.playbackRate", duration, 0.2f);
		if ((bool)modelTransform)
		{
			ChildLocator component = modelTransform.GetComponent<ChildLocator>();
			if ((bool)component)
			{
				Transform transform = component.FindChild("LeftFist");
				if ((bool)transform && (bool)effectPrefab)
				{
					chargeEffect = Object.Instantiate(effectPrefab, transform.position, transform.rotation);
					chargeEffect.transform.parent = transform;
					ScaleParticleSystemDuration component2 = chargeEffect.GetComponent<ScaleParticleSystemDuration>();
					if ((bool)component2)
					{
						component2.newDuration = duration;
					}
				}
			}
		}
		if (NetworkServer.active)
		{
			GameObject obj = Object.Instantiate(rockControllerPrefab);
			obj.GetComponent<TitanRockController>().SetOwner(base.gameObject);
			NetworkServer.Spawn(obj);
		}
	}

	public override void OnExit()
	{
		base.OnExit();
		if ((bool)chargeEffect)
		{
			EntityState.Destroy(chargeEffect);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		stopwatch += GetDeltaTime();
		if (stopwatch >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
