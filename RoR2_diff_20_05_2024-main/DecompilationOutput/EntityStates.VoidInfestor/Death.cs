using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.VoidInfestor;

public class Death : GenericCharacterDeath
{
	public static float deathDelay;

	public static GameObject deathEffectPrefab;

	private bool hasDied;

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge > deathDelay && NetworkServer.active && !hasDied)
		{
			hasDied = true;
			EffectManager.SimpleImpactEffect(deathEffectPrefab, base.characterBody.corePosition, Vector3.up, transmit: true);
			DestroyBodyAsapServer();
			EntityState.Destroy(base.gameObject);
		}
	}

	public override void OnExit()
	{
		DestroyModel();
		base.OnExit();
	}
}
