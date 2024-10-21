using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class HealingFollowerController : NetworkBehaviour
{
	public float fractionHealthHealing = 0.01f;

	public float fractionHealthBurst = 0.05f;

	public float healingInterval = 1f;

	public float rotationAngularVelocity;

	public float acceleration = 20f;

	public float damping = 0.1f;

	public bool enableSpringMotion;

	[SyncVar]
	public GameObject ownerBodyObject;

	[SyncVar]
	public GameObject targetBodyObject;

	public GameObject indicator;

	private GameObject cachedTargetBodyObject;

	private HealthComponent cachedTargetHealthComponent;

	private float healingTimer;

	private Vector3 velocity;

	private NetworkInstanceId ___ownerBodyObjectNetId;

	private NetworkInstanceId ___targetBodyObjectNetId;

	public GameObject NetworkownerBodyObject
	{
		get
		{
			return ownerBodyObject;
		}
		[param: In]
		set
		{
			SetSyncVarGameObject(value, ref ownerBodyObject, 1u, ref ___ownerBodyObjectNetId);
		}
	}

	public GameObject NetworktargetBodyObject
	{
		get
		{
			return targetBodyObject;
		}
		[param: In]
		set
		{
			SetSyncVarGameObject(value, ref targetBodyObject, 2u, ref ___targetBodyObjectNetId);
		}
	}

	private void FixedUpdate()
	{
		if (cachedTargetBodyObject != targetBodyObject)
		{
			cachedTargetBodyObject = targetBodyObject;
			OnTargetChanged();
		}
		if (NetworkServer.active)
		{
			FixedUpdateServer();
		}
	}

	[Server]
	public void AssignNewTarget(GameObject target)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.HealingFollowerController::AssignNewTarget(UnityEngine.GameObject)' called on client");
			return;
		}
		NetworktargetBodyObject = (target ? target : ownerBodyObject);
		cachedTargetBodyObject = targetBodyObject;
		cachedTargetHealthComponent = (cachedTargetBodyObject ? cachedTargetBodyObject.GetComponent<HealthComponent>() : null);
		OnTargetChanged();
	}

	private void OnTargetChanged()
	{
		cachedTargetHealthComponent = (cachedTargetBodyObject ? cachedTargetBodyObject.GetComponent<HealthComponent>() : null);
	}

	private void FixedUpdateServer()
	{
		healingTimer -= Time.fixedDeltaTime;
		if (healingTimer <= 0f)
		{
			healingTimer = healingInterval;
			DoHeal(fractionHealthHealing * healingInterval);
		}
		if (!targetBodyObject)
		{
			NetworktargetBodyObject = ownerBodyObject;
		}
		if (!ownerBodyObject)
		{
			Object.Destroy(base.gameObject);
		}
	}

	private void Update()
	{
		UpdateMotion();
		base.transform.position += velocity * Time.deltaTime;
		base.transform.rotation = Quaternion.AngleAxis(rotationAngularVelocity * Time.deltaTime, Vector3.up) * base.transform.rotation;
		if ((bool)targetBodyObject)
		{
			indicator.transform.position = GetTargetPosition();
		}
	}

	[Server]
	private void DoHeal(float healFraction)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.HealingFollowerController::DoHeal(System.Single)' called on client");
		}
		else if ((bool)cachedTargetHealthComponent)
		{
			cachedTargetHealthComponent.HealFraction(healFraction, default(ProcChainMask));
		}
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		base.transform.position = GetDesiredPosition();
	}

	private Vector3 GetTargetPosition()
	{
		GameObject gameObject = targetBodyObject ?? ownerBodyObject;
		if ((bool)gameObject)
		{
			CharacterBody component = gameObject.GetComponent<CharacterBody>();
			if (!component)
			{
				return gameObject.transform.position;
			}
			return component.corePosition;
		}
		return base.transform.position;
	}

	private Vector3 GetDesiredPosition()
	{
		return GetTargetPosition();
	}

	private void UpdateMotion()
	{
		Vector3 desiredPosition = GetDesiredPosition();
		if (enableSpringMotion)
		{
			Vector3 vector = desiredPosition - base.transform.position;
			if (vector != Vector3.zero)
			{
				Vector3 vector2 = vector.normalized * acceleration;
				Vector3 vector3 = velocity * (0f - damping);
				velocity += (vector2 + vector3) * Time.deltaTime;
			}
		}
		else
		{
			base.transform.position = Vector3.SmoothDamp(base.transform.position, desiredPosition, ref velocity, damping);
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(ownerBodyObject);
			writer.Write(targetBodyObject);
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
			writer.Write(ownerBodyObject);
		}
		if ((base.syncVarDirtyBits & 2u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(targetBodyObject);
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
			___ownerBodyObjectNetId = reader.ReadNetworkId();
			___targetBodyObjectNetId = reader.ReadNetworkId();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			ownerBodyObject = reader.ReadGameObject();
		}
		if (((uint)num & 2u) != 0)
		{
			targetBodyObject = reader.ReadGameObject();
		}
	}

	public override void PreStartClient()
	{
		if (!___ownerBodyObjectNetId.IsEmpty())
		{
			NetworkownerBodyObject = ClientScene.FindLocalObject(___ownerBodyObjectNetId);
		}
		if (!___targetBodyObjectNetId.IsEmpty())
		{
			NetworktargetBodyObject = ClientScene.FindLocalObject(___targetBodyObjectNetId);
		}
	}
}
