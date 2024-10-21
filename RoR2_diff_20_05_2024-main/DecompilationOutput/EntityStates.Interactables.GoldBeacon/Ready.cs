using RoR2;
using UnityEngine;

namespace EntityStates.Interactables.GoldBeacon;

public class Ready : GoldBeaconBaseState
{
	public static GameObject activationEffectPrefab;

	public static int count { get; private set; }

	public override void OnEnter()
	{
		base.OnEnter();
		SetReady(ready: true);
		SetPingable(value: false);
		count++;
	}

	public override void OnExit()
	{
		count--;
		if (!outer.destroying)
		{
			EffectManager.SpawnEffect(activationEffectPrefab, new EffectData
			{
				origin = base.transform.position,
				scale = 10f
			}, transmit: false);
		}
		base.OnExit();
	}
}
