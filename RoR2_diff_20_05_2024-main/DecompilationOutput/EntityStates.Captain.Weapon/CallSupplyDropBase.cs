using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Captain.Weapon;

public class CallSupplyDropBase : BaseSkillState
{
	[SerializeField]
	public GameObject muzzleflashEffect;

	[SerializeField]
	public GameObject supplyDropPrefab;

	public static string muzzleString;

	public static float baseDuration;

	public static float impactDamageCoefficient;

	public static float impactDamageForce;

	public SetupSupplyDrop.PlacementInfo placementInfo;

	private static int CallSupplyDropStateHash = Animator.StringToHash("CallSupplyDrop");

	private static int CallSupplyDropParamHash = Animator.StringToHash("CallSupplyDrop.playbackRate");

	private static int BufferEmptyStateHash = Animator.StringToHash("BufferEmpty");

	private float duration => baseDuration / attackSpeedStat;

	public override void OnEnter()
	{
		base.OnEnter();
		if (base.isAuthority)
		{
			placementInfo = SetupSupplyDrop.GetPlacementInfo(GetAimRay(), base.gameObject);
			if (placementInfo.ok)
			{
				base.activatorSkillSlot.DeductStock(1);
			}
		}
		if (placementInfo.ok)
		{
			EffectManager.SimpleMuzzleFlash(muzzleflashEffect, base.gameObject, muzzleString, transmit: false);
			base.characterBody.SetAimTimer(3f);
			PlayAnimation("Gesture, Additive", CallSupplyDropStateHash, CallSupplyDropParamHash, duration);
			PlayAnimation("Gesture, Override", CallSupplyDropStateHash, CallSupplyDropParamHash, duration);
			if (NetworkServer.active)
			{
				GameObject obj = Object.Instantiate(supplyDropPrefab, placementInfo.position, placementInfo.rotation);
				obj.GetComponent<TeamFilter>().teamIndex = base.teamComponent.teamIndex;
				obj.GetComponent<GenericOwnership>().ownerObject = base.gameObject;
				Deployable component = obj.GetComponent<Deployable>();
				if ((bool)component && (bool)base.characterBody.master)
				{
					base.characterBody.master.AddDeployable(component, DeployableSlot.CaptainSupplyDrop);
				}
				ProjectileDamage component2 = obj.GetComponent<ProjectileDamage>();
				component2.crit = RollCrit();
				component2.damage = damageStat * impactDamageCoefficient;
				component2.damageColorIndex = DamageColorIndex.Default;
				component2.force = impactDamageForce;
				component2.damageType = DamageType.Generic;
				NetworkServer.Spawn(obj);
			}
		}
		else
		{
			PlayCrossfade("Gesture, Additive", BufferEmptyStateHash, 0.1f);
			PlayCrossfade("Gesture, Additive", BufferEmptyStateHash, 0.1f);
		}
		EntityStateMachine entityStateMachine = EntityStateMachine.FindByCustomName(base.gameObject, "Skillswap");
		if ((bool)entityStateMachine)
		{
			entityStateMachine.SetNextStateToMain();
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge > duration)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Pain;
	}

	public override void OnSerialize(NetworkWriter writer)
	{
		base.OnSerialize(writer);
		placementInfo.Serialize(writer);
	}

	public override void OnDeserialize(NetworkReader reader)
	{
		base.OnDeserialize(reader);
		placementInfo.Deserialize(reader);
	}
}
