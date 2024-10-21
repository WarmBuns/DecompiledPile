using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(GenericOwnership))]
public class HealBeamController : NetworkBehaviour
{
	public Transform startPointTransform;

	public Transform endPointTransform;

	public float tickInterval = 1f;

	public bool breakOnTargetFullyHealed;

	public LineRenderer lineRenderer;

	public float lingerAfterBrokenDuration;

	[SyncVar(hook = "OnSyncTarget")]
	private HurtBoxReference netTarget;

	private float stopwatchServer;

	private bool broken;

	private HurtBoxReference previousHurtBoxReference;

	private HurtBox cachedHurtBox;

	private float scaleFactorVelocity;

	private float maxLineWidth = 0.3f;

	private float smoothTime = 0.1f;

	private float scaleFactor;

	public GenericOwnership ownership { get; private set; }

	public HurtBox target
	{
		get
		{
			return cachedHurtBox;
		}
		[Server]
		set
		{
			if (!NetworkServer.active)
			{
				Debug.LogWarning("[Server] function 'System.Void RoR2.HealBeamController::set_target(RoR2.HurtBox)' called on client");
				return;
			}
			NetworknetTarget = HurtBoxReference.FromHurtBox(value);
			UpdateCachedHurtBox();
		}
	}

	public float healRate { get; set; }

	public HurtBoxReference NetworknetTarget
	{
		get
		{
			return netTarget;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				OnSyncTarget(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVar(value, ref netTarget, 1u);
		}
	}

	private void Awake()
	{
		ownership = GetComponent<GenericOwnership>();
		startPointTransform.SetParent(null, worldPositionStays: true);
		endPointTransform.SetParent(null, worldPositionStays: true);
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		UpdateCachedHurtBox();
	}

	private void OnDestroy()
	{
		if ((bool)startPointTransform)
		{
			Object.Destroy(startPointTransform.gameObject);
		}
		if ((bool)endPointTransform)
		{
			Object.Destroy(endPointTransform.gameObject);
		}
	}

	private void OnEnable()
	{
		InstanceTracker.Add(this);
	}

	private void OnDisable()
	{
		InstanceTracker.Remove(this);
	}

	private void LateUpdate()
	{
		UpdateHealBeamVisuals();
	}

	private void OnSyncTarget(HurtBoxReference newValue)
	{
		NetworknetTarget = newValue;
		UpdateCachedHurtBox();
	}

	private void FixedUpdate()
	{
		if (NetworkServer.active)
		{
			FixedUpdateServer();
		}
	}

	private void FixedUpdateServer()
	{
		if (!cachedHurtBox)
		{
			BreakServer();
		}
		else if (tickInterval > 0f)
		{
			stopwatchServer += Time.fixedDeltaTime;
			while (stopwatchServer >= tickInterval)
			{
				stopwatchServer -= tickInterval;
				OnTickServer();
			}
		}
	}

	private void OnTickServer()
	{
		if (!cachedHurtBox || !cachedHurtBox.healthComponent)
		{
			BreakServer();
			return;
		}
		cachedHurtBox.healthComponent.Heal(healRate * tickInterval, default(ProcChainMask));
		if (breakOnTargetFullyHealed && cachedHurtBox.healthComponent.health >= cachedHurtBox.healthComponent.fullHealth)
		{
			BreakServer();
		}
	}

	private void UpdateCachedHurtBox()
	{
		if (!previousHurtBoxReference.Equals(netTarget))
		{
			cachedHurtBox = netTarget.ResolveHurtBox();
			previousHurtBoxReference = netTarget;
		}
	}

	public static bool HealBeamAlreadyExists(GameObject owner, HurtBox target)
	{
		return HealBeamAlreadyExists(owner, target.healthComponent);
	}

	public static bool HealBeamAlreadyExists(GameObject owner, HealthComponent targetHealthComponent)
	{
		List<HealBeamController> instancesList = InstanceTracker.GetInstancesList<HealBeamController>();
		int i = 0;
		for (int count = instancesList.Count; i < count; i++)
		{
			HealBeamController healBeamController = instancesList[i];
			if ((object)healBeamController.target?.healthComponent == targetHealthComponent && (object)healBeamController.ownership.ownerObject == owner)
			{
				return true;
			}
		}
		return false;
	}

	public static int GetHealBeamCountForOwner(GameObject owner)
	{
		int num = 0;
		List<HealBeamController> instancesList = InstanceTracker.GetInstancesList<HealBeamController>();
		int i = 0;
		for (int count = instancesList.Count; i < count; i++)
		{
			if ((object)instancesList[i].ownership.ownerObject == owner)
			{
				num++;
			}
		}
		return num;
	}

	private void UpdateHealBeamVisuals()
	{
		float num = (target ? 1f : 0f);
		scaleFactor = Mathf.SmoothDamp(scaleFactor, num, ref scaleFactorVelocity, smoothTime);
		Vector3 localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
		startPointTransform.SetPositionAndRotation(base.transform.position, base.transform.rotation);
		startPointTransform.localScale = localScale;
		if ((bool)cachedHurtBox)
		{
			endPointTransform.position = cachedHurtBox.transform.position;
		}
		endPointTransform.localScale = localScale;
		lineRenderer.widthMultiplier = scaleFactor * maxLineWidth;
	}

	[Server]
	public void BreakServer()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.HealBeamController::BreakServer()' called on client");
		}
		else if (!broken)
		{
			broken = true;
			target = null;
			base.transform.SetParent(null);
			ownership.ownerObject = null;
			Object.Destroy(base.gameObject, lingerAfterBrokenDuration);
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			GeneratedNetworkCode._WriteHurtBoxReference_None(writer, netTarget);
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
			GeneratedNetworkCode._WriteHurtBoxReference_None(writer, netTarget);
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
			netTarget = GeneratedNetworkCode._ReadHurtBoxReference_None(reader);
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			OnSyncTarget(GeneratedNetworkCode._ReadHurtBoxReference_None(reader));
		}
	}

	public override void PreStartClient()
	{
	}
}
