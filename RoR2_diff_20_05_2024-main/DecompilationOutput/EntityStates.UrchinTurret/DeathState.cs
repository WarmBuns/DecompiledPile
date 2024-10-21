using RoR2;
using UnityEngine;

namespace EntityStates.UrchinTurret;

public class DeathState : BaseState
{
	public static GameObject initialExplosion;

	public static float effectScale;

	public static string deathString;

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(deathString, base.gameObject);
		Transform transform = FindModelChild("Muzzle");
		if (base.isAuthority)
		{
			if ((bool)initialExplosion)
			{
				EffectManager.SpawnEffect(initialExplosion, new EffectData
				{
					origin = transform.position,
					scale = effectScale
				}, transmit: true);
			}
			EntityState.Destroy(base.gameObject);
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Death;
	}
}
