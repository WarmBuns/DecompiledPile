using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Projectile;

[RequireComponent(typeof(ProjectileController))]
[RequireComponent(typeof(ProjectileTargetComponent))]
public class GatewayProjectileController : NetworkBehaviour, IInteractable
{
	private ProjectileController projectileController;

	private ProjectileTargetComponent projectileTargetComponent;

	[SyncVar(hook = "SetLinkedObject")]
	private GameObject linkedObject;

	private GatewayProjectileController linkedGatewayProjectileController;

	private NetworkInstanceId ___linkedObjectNetId;

	public GameObject NetworklinkedObject
	{
		get
		{
			return linkedObject;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				SetLinkedObject(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVarGameObject(value, ref linkedObject, 1u, ref ___linkedObjectNetId);
		}
	}

	public string GetContextString(Interactor activator)
	{
		throw new NotImplementedException();
	}

	public Interactability GetInteractability(Interactor activator)
	{
		if (!linkedObject)
		{
			return Interactability.ConditionsNotMet;
		}
		return Interactability.Available;
	}

	public void OnInteractionBegin(Interactor activator)
	{
		throw new NotImplementedException();
	}

	public bool ShouldIgnoreSpherecastForInteractibility(Interactor activator)
	{
		return false;
	}

	public bool ShouldShowOnScanner()
	{
		return false;
	}

	public bool ShouldProximityHighlight()
	{
		return false;
	}

	private void SetLinkedObject(GameObject newLinkedObject)
	{
		if (linkedObject != newLinkedObject)
		{
			NetworklinkedObject = newLinkedObject;
			linkedGatewayProjectileController = (linkedObject ? linkedObject.GetComponent<GatewayProjectileController>() : null);
			if ((bool)linkedGatewayProjectileController)
			{
				linkedGatewayProjectileController.SetLinkedObject(base.gameObject);
			}
		}
	}

	private void Awake()
	{
		projectileController = GetComponent<ProjectileController>();
		projectileTargetComponent = GetComponent<ProjectileTargetComponent>();
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		if ((bool)projectileTargetComponent.target)
		{
			SetLinkedObject(projectileTargetComponent.target.gameObject);
			return;
		}
		FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
		fireProjectileInfo.position = base.transform.position;
		fireProjectileInfo.rotation = base.transform.rotation;
		fireProjectileInfo.target = base.gameObject;
		fireProjectileInfo.owner = projectileController.owner;
		fireProjectileInfo.speedOverride = 0f;
		fireProjectileInfo.projectilePrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/GatewayProjectile");
		ProjectileManager.instance.FireProjectile(fireProjectileInfo);
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(linkedObject);
			return true;
		}
		bool flag = false;
		if ((base.syncVarDirtyBits & (true ? 1u : 0u)) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(linkedObject);
		}
		if (!flag)
		{
			writer.WritePackedUInt32(base.syncVarDirtyBits);
		}
		return flag;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		if (initialState)
		{
			___linkedObjectNetId = reader.ReadNetworkId();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			SetLinkedObject(reader.ReadGameObject());
		}
	}

	public override void PreStartClient()
	{
		if (!___linkedObjectNetId.IsEmpty())
		{
			NetworklinkedObject = ClientScene.FindLocalObject(___linkedObjectNetId);
		}
	}
}
