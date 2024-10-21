using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.AcidLarva;

public class Death : GenericCharacterDeath
{
	public static float deathDelay;

	public static GameObject deathEffectPrefab;

	private bool hasDied;

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge > deathDelay && !hasDied)
		{
			hasDied = true;
			DestroyModel();
			EffectManager.SimpleImpactEffect(deathEffectPrefab, base.characterBody.corePosition, Vector3.up, transmit: false);
			if (NetworkServer.active)
			{
				DestroyBodyAsapServer();
			}
		}
	}

	public override void OnExit()
	{
		DestroyModel();
		base.OnExit();
	}
}
