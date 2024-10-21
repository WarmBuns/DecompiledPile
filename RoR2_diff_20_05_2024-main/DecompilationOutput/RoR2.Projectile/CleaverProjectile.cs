using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace RoR2.Projectile;

[RequireComponent(typeof(ProjectileController))]
public class CleaverProjectile : NetworkBehaviour, IProjectileImpactBehavior
{
	private enum BoomerangState
	{
		FlyOut,
		Transition,
		FlyBack,
		Stopped
	}

	public float travelSpeed = 40f;

	public float charge;

	public float transitionDuration;

	private float maxFlyStopwatch;

	public GameObject impactSpark;

	public GameObject returnImpactSpark;

	public GameObject crosshairPrefab;

	public bool canHitCharacters;

	public bool canHitWorld;

	public bool returnOnHitWorldOrHover;

	public ProjectileController projectileController;

	[SyncVar]
	private BoomerangState boomerangState;

	private Transform ownerTransform;

	private ProjectileDamage projectileDamage;

	private Rigidbody rigidbody;

	private float stopwatch;

	private float fireAge;

	private float fireFrequency;

	[FormerlySerializedAs("distanceMultiplier")]
	public float travelTime = 2f;

	public UnityEvent onFlyBack;

	private bool setScale;

	private bool recallDamageFired;

	private float stoppedStopwatch = 3f;

	private ChefController chefController;

	private ushort predictionId;

	private bool recalled;

	private bool hasEffectiveAuthority;

	private bool disableOnRcall_ReelingCache;

	public Collider[] disableOnRecall;

	private static int kCmdCmdDestroyAndCleanup;

	public BoomerangState NetworkboomerangState
	{
		get
		{
			return boomerangState;
		}
		[param: In]
		set
		{
			ulong newValueAsUlong = (ulong)value;
			ulong fieldValueAsUlong = (ulong)boomerangState;
			SetSyncVarEnum(value, newValueAsUlong, ref boomerangState, fieldValueAsUlong, 1u);
		}
	}

	private void Awake()
	{
		rigidbody = GetComponent<Rigidbody>();
		projectileController = GetComponent<ProjectileController>();
		projectileDamage = GetComponent<ProjectileDamage>();
		maxFlyStopwatch = travelTime;
	}

	private void Start()
	{
		if ((bool)projectileController)
		{
			predictionId = projectileController.predictionId;
			if ((bool)projectileController.owner)
			{
				ownerTransform = projectileController.owner.transform;
				chefController = projectileController.owner.GetComponent<ChefController>();
				if ((bool)chefController)
				{
					chefController.AddLocalProjectileReference(predictionId, this);
					if (base.hasAuthority && !NetworkServer.active && chefController.TryRetrieveCachedProjectileFireInfo(out var fireProjectileInfo))
					{
						ApplyCachedFireProjectileInfo(fireProjectileInfo);
					}
				}
			}
		}
		hasEffectiveAuthority = Util.HasEffectiveAuthority(chefController.gameObject);
	}

	private void ApplyCachedFireProjectileInfo(FireProjectileInfo fireProjectileInfo)
	{
		ProjectileDamage component = GetComponent<ProjectileDamage>();
		TeamFilter component2 = GetComponent<TeamFilter>();
		ProjectileSimple component3 = GetComponent<ProjectileSimple>();
		if ((bool)component2)
		{
			component2.teamIndex = TeamComponent.GetObjectTeam(fireProjectileInfo.owner);
		}
		if (fireProjectileInfo.useSpeedOverride && (bool)component3)
		{
			component3.desiredForwardSpeed = fireProjectileInfo.speedOverride;
		}
		if ((bool)component)
		{
			component.damage = fireProjectileInfo.damage;
			component.force = fireProjectileInfo.force;
			component.crit = fireProjectileInfo.crit;
			component.damageColorIndex = fireProjectileInfo.damageColorIndex;
			if (fireProjectileInfo.damageTypeOverride.HasValue)
			{
				component.damageType = fireProjectileInfo.damageTypeOverride.Value;
			}
		}
		projectileController.DispatchOnInitialized();
	}

	private void OnEnable()
	{
		recalled = false;
		recallDamageFired = false;
	}

	public void OnProjectileImpact(ProjectileImpactInfo impactInfo)
	{
		if (!canHitWorld)
		{
			return;
		}
		if (impactInfo.collider.gameObject.layer == LayerIndex.world.intVal)
		{
			if (returnOnHitWorldOrHover)
			{
				NetworkboomerangState = BoomerangState.FlyBack;
			}
			else
			{
				NetworkboomerangState = BoomerangState.Stopped;
			}
		}
		if (boomerangState != BoomerangState.Stopped)
		{
			EffectManager.SimpleImpactEffect(recalled ? returnImpactSpark : impactSpark, impactInfo.estimatedPointOfImpact, -base.transform.forward, transmit: true);
		}
	}

	private bool Reel()
	{
		Vector3 vector = projectileController.owner.transform.position - base.transform.position;
		_ = vector.normalized;
		float magnitude = vector.magnitude;
		if (!chefController)
		{
			chefController = projectileController.owner.GetComponent<ChefController>();
		}
		return magnitude <= 2f;
	}

	public void FixedUpdate()
	{
		if (!hasEffectiveAuthority)
		{
			return;
		}
		if (!setScale)
		{
			setScale = true;
		}
		if (!projectileController.owner)
		{
			Object.Destroy(base.gameObject);
			return;
		}
		if (!chefController)
		{
			chefController = projectileController.owner.GetComponent<ChefController>();
		}
		recalled = recalled || chefController.recallCleaver || boomerangState == BoomerangState.FlyBack;
		if ((bool)chefController && recalled && boomerangState != BoomerangState.Transition)
		{
			NetworkboomerangState = BoomerangState.FlyBack;
		}
		if (returnOnHitWorldOrHover && boomerangState == BoomerangState.Stopped)
		{
			NetworkboomerangState = BoomerangState.FlyBack;
			recalled = true;
		}
		if (!recalled)
		{
			ToggleThrowOnlyObjects(isReeling: false);
			switch (boomerangState)
			{
			case BoomerangState.FlyOut:
				rigidbody.velocity = travelSpeed * base.transform.forward;
				stopwatch += Time.fixedDeltaTime;
				if (stopwatch >= maxFlyStopwatch)
				{
					stopwatch = 0f;
					NetworkboomerangState = BoomerangState.Stopped;
				}
				break;
			case BoomerangState.Transition:
			{
				stopwatch += Time.fixedDeltaTime;
				float num = stopwatch / transitionDuration;
				Vector3 vector = CalculatePullDirection();
				rigidbody.velocity = Vector3.Lerp(travelSpeed * base.transform.forward, travelSpeed * vector, num);
				if (num >= 0.1f)
				{
					NetworkboomerangState = BoomerangState.FlyBack;
					onFlyBack?.Invoke();
				}
				break;
			}
			case BoomerangState.Stopped:
				chefController = projectileController.owner.GetComponent<ChefController>();
				rigidbody.velocity = new Vector3(0f, 0f, 0f);
				stopwatch += Time.deltaTime;
				if (stopwatch > stoppedStopwatch)
				{
					CallCmdDestroyAndCleanup();
				}
				break;
			case BoomerangState.FlyBack:
				break;
			}
			return;
		}
		ToggleThrowOnlyObjects(isReeling: true);
		if (!recallDamageFired)
		{
			rigidbody.velocity = new Vector3(0f, 0f, 0f);
			GetComponent<ProjectileOverlapAttack>().ResetOverlapAttack();
			projectileDamage.damage *= 1.5f;
			recallDamageFired = true;
		}
		bool num2 = Reel();
		Vector3 vector2 = CalculatePullDirection();
		rigidbody.velocity = travelSpeed * vector2;
		if (num2)
		{
			if (chefController != null)
			{
				chefController.NetworkcatchDirtied = true;
			}
			CallCmdDestroyAndCleanup();
		}
	}

	private void ToggleThrowOnlyObjects(bool isReeling)
	{
		if (disableOnRcall_ReelingCache != isReeling)
		{
			int num = disableOnRecall.Length;
			for (int i = 0; i < num; i++)
			{
				disableOnRecall[i].enabled = !isReeling;
			}
			disableOnRcall_ReelingCache = isReeling;
		}
	}

	[Command]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CmdDestroyAndCleanup()
	{
		Object.Destroy(base.gameObject);
	}

	private Vector3 CalculatePullDirection()
	{
		if ((bool)projectileController.owner)
		{
			return (projectileController.owner.transform.position - base.transform.position).normalized;
		}
		return base.transform.forward;
	}

	private void OnDestroy()
	{
		if ((bool)chefController)
		{
			chefController.RemoveLocalProjectileReference(predictionId);
		}
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeCmdCmdDestroyAndCleanup(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdDestroyAndCleanup called on client.");
		}
		else
		{
			((CleaverProjectile)obj).CmdDestroyAndCleanup();
		}
	}

	public void CallCmdDestroyAndCleanup()
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdDestroyAndCleanup called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdDestroyAndCleanup();
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdDestroyAndCleanup);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		SendCommandInternal(networkWriter, 0, "CmdDestroyAndCleanup");
	}

	static CleaverProjectile()
	{
		kCmdCmdDestroyAndCleanup = 978502080;
		NetworkBehaviour.RegisterCommandDelegate(typeof(CleaverProjectile), kCmdCmdDestroyAndCleanup, InvokeCmdCmdDestroyAndCleanup);
		NetworkCRC.RegisterBehaviour("CleaverProjectile", 0);
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write((int)boomerangState);
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
			writer.Write((int)boomerangState);
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
			boomerangState = (BoomerangState)reader.ReadInt32();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			boomerangState = (BoomerangState)reader.ReadInt32();
		}
	}

	public override void PreStartClient()
	{
	}
}
