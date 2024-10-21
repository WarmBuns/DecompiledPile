using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace RoR2.Projectile;

[RequireComponent(typeof(ProjectileController))]
public class BoomerangProjectile : NetworkBehaviour, IProjectileImpactBehavior
{
	private enum BoomerangState
	{
		FlyOut,
		Transition,
		FlyBack,
		NetworkClientCatchup
	}

	public float travelSpeed = 40f;

	public float charge;

	public float transitionDuration;

	private float maxFlyStopwatch;

	public GameObject impactSpark;

	public GameObject crosshairPrefab;

	public bool canHitCharacters;

	public bool canHitWorld;

	private ProjectileController projectileController;

	[SyncVar]
	private BoomerangState boomerangState;

	private Transform ownerTransform;

	private ProjectileDamage projectileDamage;

	private Rigidbody rigidbody;

	private float stopwatch;

	private float fireAge;

	private float fireFrequency;

	public float distanceMultiplier = 2f;

	public UnityEvent onFlyBack;

	[Tooltip("Allows the server and client side boomerangs to idle harmlessly/invisibly for up to 1 second while the rest of the boomerangs from other clients have time to finish their flight.")]
	public bool EnableNetworkClientCatchup;

	private bool setScale;

	private float networkCatchupTimer = -1f;

	private float networkCatchupDuration = 1f;

	public bool isStunAndPierce;

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
		if ((bool)projectileController && (bool)projectileController.owner)
		{
			ownerTransform = projectileController.owner.transform;
		}
		maxFlyStopwatch = charge * distanceMultiplier;
	}

	private void Start()
	{
		float num = charge * 7f;
		if (num < 1f)
		{
			num = 1f;
		}
		Vector3 localScale = new Vector3(num * base.transform.localScale.x, num * base.transform.localScale.y, num * base.transform.localScale.z);
		base.transform.localScale = localScale;
		base.gameObject.GetComponent<ProjectileController>().ghost.transform.localScale = localScale;
		GetComponent<ProjectileDotZone>().damageCoefficient *= num;
	}

	public void OnProjectileImpact(ProjectileImpactInfo impactInfo)
	{
		if (canHitWorld)
		{
			NetworkboomerangState = BoomerangState.FlyBack;
			onFlyBack?.Invoke();
			EffectManager.SimpleImpactEffect(impactSpark, impactInfo.estimatedPointOfImpact, -base.transform.forward, transmit: true);
		}
	}

	private bool Reel()
	{
		return (projectileController.owner.transform.position - base.transform.position).magnitude <= 2f;
	}

	public void FixedUpdate()
	{
		bool active = NetworkServer.active;
		if (active)
		{
			if (!setScale)
			{
				setScale = true;
			}
			if (!projectileController.owner)
			{
				Object.Destroy(base.gameObject);
				return;
			}
		}
		switch (boomerangState)
		{
		case BoomerangState.FlyOut:
			if (active)
			{
				rigidbody.velocity = travelSpeed * base.transform.forward;
				stopwatch += Time.fixedDeltaTime;
				if (stopwatch >= maxFlyStopwatch)
				{
					stopwatch = 0f;
					NetworkboomerangState = BoomerangState.Transition;
				}
			}
			break;
		case BoomerangState.Transition:
			if (active)
			{
				stopwatch += Time.fixedDeltaTime;
				float num = stopwatch / transitionDuration;
				Vector3 vector = CalculatePullDirection();
				rigidbody.velocity = Vector3.Lerp(travelSpeed * base.transform.forward, travelSpeed * vector, num);
				if (num >= 1f)
				{
					NetworkboomerangState = BoomerangState.FlyBack;
					onFlyBack?.Invoke();
				}
			}
			break;
		case BoomerangState.FlyBack:
		{
			bool num2 = Reel();
			if (active)
			{
				canHitWorld = false;
				Vector3 vector2 = CalculatePullDirection();
				rigidbody.velocity = travelSpeed * vector2;
			}
			if (!num2)
			{
				break;
			}
			if (EnableNetworkClientCatchup)
			{
				projectileController.DisconnectFromGhost();
				if (active)
				{
					networkCatchupTimer = networkCatchupDuration;
					NetworkboomerangState = BoomerangState.NetworkClientCatchup;
				}
			}
			else if (active)
			{
				Object.Destroy(base.gameObject);
			}
			break;
		}
		case BoomerangState.NetworkClientCatchup:
			if (!active)
			{
				if (Reel())
				{
					projectileController.DisconnectFromGhost();
				}
				break;
			}
			networkCatchupTimer -= Time.fixedDeltaTime;
			if (networkCatchupTimer <= 0f)
			{
				Object.Destroy(base.gameObject);
			}
			break;
		}
		Vector3 CalculatePullDirection()
		{
			if ((bool)projectileController.owner)
			{
				return (projectileController.owner.transform.position - base.transform.position).normalized;
			}
			return base.transform.forward;
		}
	}

	private void UNetVersion()
	{
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
