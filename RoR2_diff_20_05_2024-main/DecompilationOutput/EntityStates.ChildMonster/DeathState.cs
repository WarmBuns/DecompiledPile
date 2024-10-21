using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.ChildMonster;

public class DeathState : GenericCharacterDeath
{
	public static GameObject deathEffectPrefab;

	public float smallHopVelocity = 0.35f;

	private bool blewUp;

	public float duration = 0.2f;

	public static string deathSoundString;

	public override void OnEnter()
	{
		base.OnEnter();
		SmallHop(base.characterMotor, smallHopVelocity);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active && !blewUp && base.fixedAge > duration)
		{
			EffectManager.SpawnEffect(deathEffectPrefab, new EffectData
			{
				origin = base.characterBody.corePosition
			}, transmit: true);
			Util.PlaySound(deathSoundString, base.gameObject);
			blewUp = true;
		}
		if (base.fixedAge > duration * 2f)
		{
			DestroyBodyAsapServer();
		}
	}

	public override void OnExit()
	{
		DestroyModel();
		base.OnExit();
	}
}
