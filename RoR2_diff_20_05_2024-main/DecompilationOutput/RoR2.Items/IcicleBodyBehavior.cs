using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RoR2.Items;

public class IcicleBodyBehavior : BaseItemBodyBehavior
{
	private static GameObject icicleAuraPrefab;

	private IcicleAuraController icicleAura;

	[ItemDefAssociation(useOnServer = true, useOnClient = false)]
	private static ItemDef GetItemDef()
	{
		return RoR2Content.Items.Icicle;
	}

	[InitDuringStartup]
	private static void Init()
	{
		AsyncOperationHandle<GameObject> asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/NetworkedObjects/IcicleAura");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
		{
			icicleAuraPrefab = x.Result;
		};
	}

	private void OnEnable()
	{
		GlobalEventManager.onCharacterDeathGlobal += OnCharacterDeathGlobal;
		GameObject gameObject = Object.Instantiate(icicleAuraPrefab, base.transform.position, Quaternion.identity);
		icicleAura = gameObject.GetComponent<IcicleAuraController>();
		icicleAura.Networkowner = base.gameObject;
		NetworkServer.Spawn(gameObject);
	}

	private void OnDisable()
	{
		GlobalEventManager.onCharacterDeathGlobal -= OnCharacterDeathGlobal;
		if ((bool)icicleAura)
		{
			Object.Destroy(icicleAura);
			icicleAura = null;
		}
	}

	private void OnCharacterDeathGlobal(DamageReport damageReport)
	{
		if ((object)damageReport.attackerBody == base.body && (bool)icicleAura)
		{
			icicleAura.OnOwnerKillOther();
		}
	}
}
