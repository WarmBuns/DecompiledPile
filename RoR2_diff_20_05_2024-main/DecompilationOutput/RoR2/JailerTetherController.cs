using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(BezierCurveLine))]
public class JailerTetherController : NetworkBehaviour
{
	[SyncVar]
	public GameObject targetRoot;

	[SyncVar]
	public GameObject ownerRoot;

	[SyncVar]
	public GameObject origin;

	[SyncVar]
	public float reelSpeed = 12f;

	[NonSerialized]
	public float breakDistanceSqr;

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

	private CharacterBody ownerBody;

	private CharacterBody targetBody;

	private BuffDef tetheredBuff;

	private NetworkInstanceId ___targetRootNetId;

	private NetworkInstanceId ___ownerRootNetId;

	private NetworkInstanceId ___originNetId;

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

	public GameObject Networkorigin
	{
		get
		{
			return origin;
		}
		[param: In]
		set
		{
			SetSyncVarGameObject(value, ref origin, 4u, ref ___originNetId);
		}
	}

	public float NetworkreelSpeed
	{
		get
		{
			return reelSpeed;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref reelSpeed, 8u);
		}
	}

	private void Awake()
	{
		bezierCurveLine = GetComponent<BezierCurveLine>();
	}

	private void Start()
	{
		if (!targetHealthComponent)
		{
			targetHealthComponent = targetRoot.GetComponent<HealthComponent>();
		}
		if (!ownerBody)
		{
			ownerBody = ownerRoot.GetComponent<CharacterBody>();
		}
		Networkorigin = ((origin != null) ? origin : ownerRoot);
	}

	private void LateUpdate()
	{
		age += Time.deltaTime;
		Vector3 position = origin.transform.position;
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
			Vector3 vector = origin.transform.position - targetRootPosition;
			if (NetworkServer.active)
			{
				float sqrMagnitude = vector.sqrMagnitude;
				tickTimer -= Time.fixedDeltaTime;
				if (tickTimer <= 0f)
				{
					tickTimer += tickInterval;
					DoDamageTick();
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
			Vector3 vector2 = reelSpeed * Time.fixedDeltaTime * vector.normalized;
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

	private void DoDamageTick()
	{
		DamageInfo damageInfo = new DamageInfo
		{
			position = targetRoot.transform.position,
			attacker = null,
			inflictor = null,
			damage = damageCoefficientPerTick * ownerBody.damage,
			damageColorIndex = DamageColorIndex.Default,
			damageType = DamageType.Generic,
			crit = false,
			force = Vector3.zero,
			procChainMask = default(ProcChainMask),
			procCoefficient = 0f
		};
		targetHealthComponent.TakeDamage(damageInfo);
		if (!targetHealthComponent.alive)
		{
			NetworktargetRoot = null;
		}
	}

	public override void OnNetworkDestroy()
	{
		if (NetworkServer.active)
		{
			RemoveBuff();
		}
		base.OnNetworkDestroy();
	}

	private void RemoveBuff()
	{
		if ((bool)tetheredBuff && (bool)targetBody)
		{
			targetBody.RemoveBuff(tetheredBuff);
		}
	}

	public CharacterBody GetTargetBody()
	{
		if ((bool)targetBody)
		{
			return targetBody;
		}
		if ((bool)targetRoot)
		{
			targetBody = targetRoot.GetComponent<CharacterBody>();
			return targetBody;
		}
		return null;
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

	public void SetTetheredBuff(BuffDef buffDef)
	{
		if (buffDef != null)
		{
			CharacterBody characterBody = GetTargetBody();
			if ((bool)characterBody)
			{
				tetheredBuff = buffDef;
				characterBody.AddBuff(tetheredBuff);
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
			writer.Write(targetRoot);
			writer.Write(ownerRoot);
			writer.Write(origin);
			writer.Write(reelSpeed);
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
		if ((base.syncVarDirtyBits & 4u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(origin);
		}
		if ((base.syncVarDirtyBits & 8u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(reelSpeed);
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
			___originNetId = reader.ReadNetworkId();
			reelSpeed = reader.ReadSingle();
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
		if (((uint)num & 4u) != 0)
		{
			origin = reader.ReadGameObject();
		}
		if (((uint)num & 8u) != 0)
		{
			reelSpeed = reader.ReadSingle();
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
		if (!___originNetId.IsEmpty())
		{
			Networkorigin = ClientScene.FindLocalObject(___originNetId);
		}
	}
}
