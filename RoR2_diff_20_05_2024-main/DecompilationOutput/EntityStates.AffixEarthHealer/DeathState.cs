using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.AffixEarthHealer;

public class DeathState : GenericCharacterDeath
{
	public static GameObject initialExplosion;

	public static float duration;

	public static string enterSoundString;

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)initialExplosion)
		{
			EffectManager.SimpleEffect(initialExplosion, base.transform.position, base.transform.rotation, transmit: false);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active && base.fixedAge > duration)
		{
			EntityState.Destroy(base.gameObject);
		}
	}
}
