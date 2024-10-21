using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(BezierCurveLine))]
public class TarTetherController : NetworkBehaviour
{
	[SyncVar]
	public GameObject targetRoot;

	[SyncVar]
	public GameObject ownerRoot;

	public float reelSpeed = 12f;

	[NonSerialized]
	public float mulchDistanceSqr;

	[NonSerialized]
	public float breakDistanceSqr;

	[NonSerialized]
	public float mulchDamageScale;

	[NonSerialized]
	public float mulchTickIntervalScale;

	[NonSerialized]
	public float damageCoefficientPerTick;

	[NonSerialized]
	public float tickInterval;

	[NonSerialized]
	public float tickTimer;

	public float attachTime;

	private float fixedAge;

	private float age;

	private bool beginSiphon;

	private BezierCurveLine bezierCurveLine;

	private HealthComponent targetHealthComponent;

	private HealthComponent ownerHealthComponent;

	private CharacterBody ownerBody;

	private NetworkInstanceId ___targetRootNetId;

	private NetworkInstanceId ___ownerRootNetId;

	public GameObject NetworktargetRoot
	{
		get
		{
			return targetRoot;
		}
		[param: In]
		set
		{
			SetSyncVarGameObject(value, ref targetRoot, 1u, ref ___targetRootNetId);
		}
	}

	public GameObject NetworkownerRoot
	{
		get
		{
			return ownerRoot;
		}
		[param: In]
		set
		{
			SetSyncVarGameObject(value, ref ownerRoot, 2u, ref ___ownerRootNetId);
		}
	}

	private void Awake()
	{
		bezierCurveLine = GetComponent<BezierCurveLine>();
	}

	private void DoDamageTick(bool mulch)
	{
		if (!targetHealthComponent)
		{
			targetHealthComponent = targetRoot.GetComponent<HealthComponent>();
		}
		if (!ownerHealthComponent)
		{
			ownerHealthComponent = ownerRoot.GetComponent<HealthComponent>();
		}
		if (!ownerBody)
		{
			ownerBody = ownerRoot.GetComponent<CharacterBody>();
		}
		if ((bool)ownerRoot)
		{
			DamageInfo damageInfo = new DamageInfo
			{
				position = targetRoot.transform.position,
				attacker = null,
				inflictor = null,
				damage = (mulch ? (damageCoefficientPerTick * mulchDamageScale) : damageCoefficientPerTick) * ownerBody.damage,
				damageColorIndex = DamageColorIndex.Default,
				damageType = DamageType.Generic,
				crit = false,
				force = Vector3.zero,
				procChainMask = default(ProcChainMask),
				procCoefficient = 0f
			};
			targetHealthComponent.TakeDamage(damageInfo);
			if (!damageInfo.rejected)
			{
				ownerHealthComponent.Heal(damageInfo.damage, default(ProcChainMask));
			}
			if (!targetHealthComponent.alive)
			{
				NetworktargetRoot = null;
			}
		}
	}

	private Vector3 GetTargetRootPosition()
	{
		if ((bool)targetRoot)
		{
			Vector3 result = targetRoot.transform.position;
			if ((bool)targetHealthComponent)
			{
				result = targetHealthComponent.body.corePosition;
			}
			return result;
		}
		return base.transform.position;
	}

	private void Update()
	{
		age += Time.deltaTime;
		Vector3 position = ownerRoot.transform.position;
		if (!beginSiphon)
		{
			Vector3 position2 = Vector3.Lerp(position, GetTargetRootPosition(), age / attachTime);
			bezierCurveLine.endTransform.position = position2;
		}
		else if ((bool)targetRoot)
		{
			bezierCurveLine.endTransform.position = targetRoot.transform.position;
		}
	}

	private void FixedUpdate()
	{
		fixedAge += Time.fixedDeltaTime;
		if ((bool)targetRoot && (bool)ownerRoot)
		{
			Vector3 targetRootPosition = GetTargetRootPosition();
			if (!beginSiphon && fixedAge >= attachTime)
			{
				beginSiphon = true;
				return;
			}
			Vector3 vector = ownerRoot.transform.position - targetRootPosition;
			if (NetworkServer.active)
			{
				float sqrMagnitude = vector.sqrMagnitude;
				bool flag = sqrMagnitude < mulchDistanceSqr;
				tickTimer -= Time.fixedDeltaTime;
				if (tickTimer <= 0f)
				{
					tickTimer += (flag ? (tickInterval * mulchTickIntervalScale) : tickInterval);
					DoDamageTick(flag);
				}
				if (sqrMagnitude > breakDistanceSqr)
				{
					UnityEngine.Object.Destroy(base.gameObject);
					return;
				}
			}
			if (!Util.HasEffectiveAuthority(targetRoot))
			{
				return;
			}
			Vector3 vector2 = vector.normalized * (reelSpeed * Time.fixedDeltaTime);
			CharacterMotor component = targetRoot.GetComponent<CharacterMotor>();
			if ((bool)component)
			{
				component.rootMotion += vector2;
				return;
			}
			Rigidbody component2 = targetRoot.GetComponent<Rigidbody>();
			if ((bool)component2)
			{
				component2.velocity += vector2;
			}
		}
		else if (NetworkServer.active)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(targetRoot);
			writer.Write(ownerRoot);
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
			writer.Write(targetRoot);
		}
		if ((base.syncVarDirtyBits & 2u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(ownerRoot);
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
			___targetRootNetId = reader.ReadNetworkId();
			___ownerRootNetId = reader.ReadNetworkId();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			targetRoot = reader.ReadGameObject();
		}
		if (((uint)num & 2u) != 0)
		{
			ownerRoot = reader.ReadGameObject();
		}
	}

	public override void PreStartClient()
	{
		if (!___targetRootNetId.IsEmpty())
		{
			NetworktargetRoot = ClientScene.FindLocalObject(___targetRootNetId);
		}
		if (!___ownerRootNetId.IsEmpty())
		{
			NetworkownerRoot = ClientScene.FindLocalObject(___ownerRootNetId);
		}
	}
}
