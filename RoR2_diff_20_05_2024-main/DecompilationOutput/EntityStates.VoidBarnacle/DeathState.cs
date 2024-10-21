using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.VoidBarnacle;

public class DeathState : GenericCharacterDeath
{
	public static string animationLayerName;

	public static string animationStateName;

	public static string animationPlaybackRateName;

	public static float duration;

	public static GameObject deathFXPrefab;

	public override void OnEnter()
	{
		base.OnEnter();
		if (deathFXPrefab != null)
		{
			EffectManager.SimpleEffect(deathFXPrefab, base.transform.position, base.transform.rotation, transmit: true);
		}
		PlayAnimation(animationLayerName, animationStateName, animationPlaybackRateName, duration);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && NetworkServer.active)
		{
			EntityState.Destroy(base.gameObject);
		}
	}

	public override void OnExit()
	{
		DestroyModel();
		base.OnExit();
	}
}
