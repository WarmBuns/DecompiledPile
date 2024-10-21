using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace EntityStates.LunarGolem;

public class ChargeTwinShot : BaseState
{
	public static float baseDuration = 3f;

	public static float laserMaxWidth = 0.2f;

	public static GameObject effectPrefab;

	public static string chargeSoundString;

	private float duration;

	private uint chargePlayID;

	private List<GameObject> chargeEffects = new List<GameObject>();

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		Transform modelTransform = GetModelTransform();
		chargePlayID = Util.PlayAttackSpeedSound(chargeSoundString, base.gameObject, attackSpeedStat);
		PlayCrossfade("Gesture, Additive", "ChargeTwinShot", "TwinShot.playbackRate", duration, 0.1f);
		if ((bool)modelTransform)
		{
			ChildLocator component = modelTransform.GetComponent<ChildLocator>();
			if ((bool)component)
			{
				List<Transform> list = new List<Transform>();
				list.Add(component.FindChild("MuzzleLT"));
				list.Add(component.FindChild("MuzzleLB"));
				list.Add(component.FindChild("MuzzleRT"));
				list.Add(component.FindChild("MuzzleRB"));
				if ((bool)effectPrefab)
				{
					for (int i = 0; i < list.Count; i++)
					{
						if ((bool)list[i])
						{
							GameObject gameObject = Object.Instantiate(effectPrefab, list[i].position, list[i].rotation);
							gameObject.transform.parent = list[i];
							ScaleParticleSystemDuration component2 = gameObject.GetComponent<ScaleParticleSystemDuration>();
							if ((bool)component2)
							{
								component2.newDuration = duration;
							}
							chargeEffects.Add(gameObject);
						}
					}
				}
			}
		}
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(duration);
		}
	}

	public override void OnExit()
	{
		AkSoundEngine.StopPlayingID(chargePlayID);
		base.OnExit();
		for (int i = 0; i < chargeEffects.Count; i++)
		{
			if ((bool)chargeEffects[i])
			{
				EntityState.Destroy(chargeEffects[i]);
			}
		}
	}

	public override void Update()
	{
		base.Update();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			FireTwinShots nextState = new FireTwinShots();
			outer.SetNextState(nextState);
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
