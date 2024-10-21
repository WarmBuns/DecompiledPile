using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Engi.EngiWeapon;

public class EngiOtherShield : BaseState
{
	public CharacterBody target;

	public float minimumDuration;

	private Indicator indicator;

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)target)
		{
			indicator = new Indicator(base.gameObject, LegacyResourcesAPI.Load<GameObject>("Prefabs/EngiShieldRetractIndicator"));
			indicator.active = true;
			indicator.targetTransform = Util.GetCoreTransform(target.gameObject);
			target.AddBuff(RoR2Content.Buffs.EngiShield);
			target.RecalculateStats();
			HealthComponent component = target.GetComponent<HealthComponent>();
			if ((bool)component)
			{
				component.RechargeShieldFull();
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!target || !base.characterBody.healthComponent.alive)
		{
			outer.SetNextStateToMain();
		}
	}

	public override void OnExit()
	{
		base.skillLocator.utility = base.skillLocator.FindSkill("GiveShield");
		if (NetworkServer.active && (bool)target)
		{
			target.RemoveBuff(RoR2Content.Buffs.EngiShield);
		}
		if (base.isAuthority)
		{
			base.skillLocator.utility.RemoveAllStocks();
		}
		if (indicator != null)
		{
			indicator.active = false;
		}
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		if (!(base.fixedAge >= minimumDuration))
		{
			return InterruptPriority.PrioritySkill;
		}
		return InterruptPriority.Skill;
	}

	public override void OnSerialize(NetworkWriter writer)
	{
		base.OnSerialize(writer);
		writer.Write(target.gameObject);
	}

	public override void OnDeserialize(NetworkReader reader)
	{
		base.OnDeserialize(reader);
		GameObject gameObject = reader.ReadGameObject();
		if ((bool)gameObject)
		{
			target = gameObject.GetComponent<CharacterBody>();
		}
	}
}
