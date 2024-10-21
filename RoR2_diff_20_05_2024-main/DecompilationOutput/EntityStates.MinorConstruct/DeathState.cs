using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.MinorConstruct;

public class DeathState : GenericCharacterDeath
{
	public static GameObject explosionPrefab;

	public override void OnEnter()
	{
		base.OnEnter();
		if (NetworkServer.active)
		{
			EffectManager.SimpleEffect(explosionPrefab, base.transform.position, base.transform.rotation, transmit: true);
			EntityState.Destroy(base.gameObject);
		}
	}

	public override void OnExit()
	{
		DestroyModel();
		base.OnExit();
	}
}
