using RoR2;
using UnityEngine;

namespace EntityStates.ParentEgg;

public class IncubateState : BaseEggState
{
	private float duration;

	private static int SpawnStateHash = Animator.StringToHash("Spawn");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = base.controller.incubationDuration;
		PlayAnimation("Body", SpawnStateHash);
		Util.PlaySound(base.controller.podSpawnSound, base.gameObject);
		EffectManager.SimpleEffect(base.controller.spawnEffect, base.gameObject.transform.position, base.transform.rotation, transmit: true);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextState(new PreHatch());
		}
	}
}
