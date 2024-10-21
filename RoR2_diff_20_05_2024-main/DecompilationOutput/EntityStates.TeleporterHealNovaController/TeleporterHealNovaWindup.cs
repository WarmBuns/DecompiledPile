using RoR2;
using UnityEngine;

namespace EntityStates.TeleporterHealNovaController;

public class TeleporterHealNovaWindup : BaseState
{
	public static GameObject chargeEffectPrefab;

	public static float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		EffectManager.SimpleEffect(chargeEffectPrefab, base.transform.position, Quaternion.identity, transmit: false);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && duration <= base.fixedAge)
		{
			outer.SetNextState(new TeleporterHealNovaPulse());
		}
	}
}
