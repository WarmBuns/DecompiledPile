using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.VoidRaidCrab;

public class BaseWardWipeState : BaseState
{
	[SerializeField]
	public GameObject safeWardDisappearEffectPrefab;

	protected List<GameObject> safeWards = new List<GameObject>();

	protected FogDamageController fogDamageController;

	public override void ModifyNextState(EntityState nextState)
	{
		base.ModifyNextState(nextState);
		if (nextState is BaseWardWipeState baseWardWipeState)
		{
			baseWardWipeState.fogDamageController = fogDamageController;
			baseWardWipeState.safeWards = safeWards;
			return;
		}
		if ((bool)fogDamageController)
		{
			fogDamageController.enabled = false;
		}
		if (safeWards == null || !NetworkServer.active)
		{
			return;
		}
		foreach (GameObject safeWard in safeWards)
		{
			if ((bool)fogDamageController)
			{
				IZone component = safeWard.GetComponent<IZone>();
				fogDamageController.RemoveSafeZone(component);
			}
			if ((bool)safeWardDisappearEffectPrefab)
			{
				EffectData effectData = new EffectData();
				effectData.origin = safeWard.transform.position;
				EffectManager.SpawnEffect(safeWardDisappearEffectPrefab, effectData, transmit: true);
			}
			EntityState.Destroy(safeWard);
		}
	}
}
