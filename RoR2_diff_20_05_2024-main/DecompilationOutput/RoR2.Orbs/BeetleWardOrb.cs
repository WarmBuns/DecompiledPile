using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Orbs;

public class BeetleWardOrb : Orb
{
	public float speed;

	public override void Begin()
	{
		base.duration = base.distanceToTarget / speed;
		EffectData effectData = new EffectData
		{
			origin = origin,
			genericFloat = base.duration
		};
		effectData.SetHurtBoxReference(target);
		EffectManager.SpawnEffect(OrbStorageUtility.Get("Prefabs/Effects/OrbEffects/BeetleWardOrbEffect"), effectData, transmit: true);
	}

	public override void OnArrival()
	{
		if ((bool)target)
		{
			GameObject gameObject = Object.Instantiate(OrbStorageUtility.Get("Prefabs/NetworkedObjects/BeetleWard"), target.transform.position, Quaternion.identity);
			gameObject.GetComponent<TeamFilter>().teamIndex = target.teamIndex;
			NetworkServer.Spawn(gameObject);
		}
	}
}
