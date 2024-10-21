using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace RoR2;

[RequireComponent(typeof(NetworkedBodyAttachment))]
public class HelfireController : NetworkBehaviour
{
	[SyncVar]
	public int stack = 1;

	[FormerlySerializedAs("radius")]
	public float baseRadius;

	public float dotDuration;

	public float interval;

	[Range(0f, 1f)]
	public float healthFractionPerSecond = 0.05f;

	public float allyDamageScalar = 0.5f;

	public float enemyDamageScalar = 24f;

	public Transform auraEffectTransform;

	private float timer;

	private float radius;

	private CameraTargetParams.AimRequest aimRequest;

	private CameraTargetParams cameraTargetParams;

	public NetworkedBodyAttachment networkedBodyAttachment { get; private set; }

	public int Networkstack
	{
		get
		{
			return stack;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref stack, 1u);
		}
	}

	private void Awake()
	{
		networkedBodyAttachment = GetComponent<NetworkedBodyAttachment>();
		auraEffectTransform.SetParent(null);
	}

	private void OnDestroy()
	{
		if ((bool)auraEffectTransform)
		{
			UnityEngine.Object.Destroy(auraEffectTransform.gameObject);
			auraEffectTransform = null;
		}
		aimRequest?.Dispose();
	}

	private void FixedUpdate()
	{
		radius = baseRadius * (1f + (float)(stack - 1) * 0.5f);
		if (NetworkServer.active)
		{
			ServerFixedUpdate();
		}
	}

	private void ServerFixedUpdate()
	{
		timer -= Time.fixedDeltaTime;
		if (!(timer <= 0f))
		{
			return;
		}
		timer = interval;
		float num = healthFractionPerSecond * dotDuration * networkedBodyAttachment.attachedBody.healthComponent.fullCombinedHealth;
		float num2 = 1f;
		TeamDef teamDef = TeamCatalog.GetTeamDef(networkedBodyAttachment.attachedBody.teamComponent.teamIndex);
		if (teamDef != null && teamDef.friendlyFireScaling > 0f)
		{
			num2 = 1f / teamDef.friendlyFireScaling;
		}
		Collider[] colliders;
		int num3 = HGPhysics.OverlapSphere(out colliders, base.transform.position, radius, LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Collide);
		GameObject[] array = new GameObject[colliders.Length];
		int count = 0;
		for (int i = 0; i < num3; i++)
		{
			CharacterBody characterBody = Util.HurtBoxColliderToBody(colliders[i]);
			GameObject gameObject = (characterBody ? characterBody.gameObject : null);
			if (!gameObject || Array.IndexOf(array, gameObject, 0, count) != -1)
			{
				continue;
			}
			float num4 = num;
			float num5 = 1f;
			if (networkedBodyAttachment.attachedBody.teamComponent.teamIndex == characterBody.teamComponent.teamIndex)
			{
				num4 *= num2;
				num5 *= num2;
				if ((object)networkedBodyAttachment.attachedBody != characterBody)
				{
					num4 *= allyDamageScalar;
					num5 *= allyDamageScalar;
				}
			}
			else
			{
				num4 *= enemyDamageScalar;
				num5 *= enemyDamageScalar;
			}
			InflictDotInfo inflictDotInfo = default(InflictDotInfo);
			inflictDotInfo.attackerObject = networkedBodyAttachment.attachedBodyObject;
			inflictDotInfo.victimObject = gameObject;
			inflictDotInfo.totalDamage = num4;
			inflictDotInfo.damageMultiplier = num5;
			inflictDotInfo.dotIndex = DotController.DotIndex.Helfire;
			inflictDotInfo.maxStacksFromAttacker = 1u;
			InflictDotInfo dotInfo = inflictDotInfo;
			StrengthenBurnUtils.CheckDotForUpgrade(networkedBodyAttachment.attachedBody.inventory, ref dotInfo);
			DotController.InflictDot(ref dotInfo);
			array[count++] = gameObject;
		}
		HGPhysics.ReturnResults(colliders);
	}

	private void LateUpdate()
	{
		CharacterBody attachedBody = networkedBodyAttachment.attachedBody;
		if ((bool)attachedBody)
		{
			auraEffectTransform.position = networkedBodyAttachment.attachedBody.corePosition;
			auraEffectTransform.localScale = new Vector3(radius, radius, radius);
			if (!cameraTargetParams)
			{
				cameraTargetParams = attachedBody.GetComponent<CameraTargetParams>();
			}
			else if (aimRequest == null)
			{
				aimRequest = cameraTargetParams.RequestAimType(CameraTargetParams.AimType.Aura);
			}
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.WritePackedUInt32((uint)stack);
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
			writer.WritePackedUInt32((uint)stack);
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
			stack = (int)reader.ReadPackedUInt32();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			stack = (int)reader.ReadPackedUInt32();
		}
	}

	public override void PreStartClient()
	{
	}
}
