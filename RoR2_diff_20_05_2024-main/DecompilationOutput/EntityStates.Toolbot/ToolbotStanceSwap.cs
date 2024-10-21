using System;
using RoR2;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Toolbot;

public class ToolbotStanceSwap : ToolbotStanceBase
{
	[SerializeField]
	private float baseDuration = 0.5f;

	private Run.FixedTimeStamp endTime;

	public Type nextStanceState;

	public Type previousStanceState;

	public override void OnEnter()
	{
		base.OnEnter();
		float num = baseDuration / attackSpeedStat;
		endTime = Run.FixedTimeStamp.now + num;
		GenericSkill a = GetPrimarySkill1();
		GenericSkill b = GetPrimarySkill2();
		if (previousStanceState != typeof(ToolbotStanceA))
		{
			Util.Swap(ref a, ref b);
		}
		if ((bool)b && b.skillDef is ToolbotWeaponSkillDef toolbotWeaponSkillDef)
		{
			SendWeaponStanceToAnimator(toolbotWeaponSkillDef);
			Util.PlaySound(toolbotWeaponSkillDef.entrySound, base.gameObject);
		}
		if ((bool)a && a.skillDef is ToolbotWeaponSkillDef toolbotWeaponSkillDef2)
		{
			PlayAnimation("Stance, Additive", toolbotWeaponSkillDef2.exitAnimState, "StanceSwap.playbackRate", num * 0.5f);
			PlayAnimation("Gesture, Additive", toolbotWeaponSkillDef2.exitGestureAnimState);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && endTime.hasPassed)
		{
			outer.SetNextState(EntityStateCatalog.InstantiateState(nextStanceState));
		}
	}

	public override void OnSerialize(NetworkWriter writer)
	{
		base.OnSerialize(writer);
		EntityStateIndex stateIndex = EntityStateCatalog.GetStateIndex(previousStanceState);
		EntityStateIndex stateIndex2 = EntityStateCatalog.GetStateIndex(nextStanceState);
		writer.Write(stateIndex);
		writer.Write(stateIndex2);
	}

	public override void OnDeserialize(NetworkReader reader)
	{
		base.OnDeserialize(reader);
		EntityStateIndex entityStateIndex = reader.ReadEntityStateIndex();
		EntityStateIndex entityStateIndex2 = reader.ReadEntityStateIndex();
		previousStanceState = EntityStateCatalog.GetStateType(entityStateIndex);
		nextStanceState = EntityStateCatalog.GetStateType(entityStateIndex2);
	}
}
