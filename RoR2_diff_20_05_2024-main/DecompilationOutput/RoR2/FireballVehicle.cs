using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(VehicleSeat))]
public class FireballVehicle : MonoBehaviour, ICameraStateProvider
{
	[Header("Vehicle Parameters")]
	public float duration = 3f;

	public float initialSpeed = 120f;

	public float targetSpeed = 40f;

	public float acceleration = 20f;

	public float cameraLerpTime = 1f;

	[Header("Blast Parameters")]
	public bool detonateOnCollision;

	public GameObject explosionEffectPrefab;

	public float blastDamageCoefficient;

	public float blastRadius;

	public float blastForce;

	public BlastAttack.FalloffModel blastFalloffModel;

	public DamageTypeCombo blastDamageType;

	public Vector3 blastBonusForce;

	public float blastProcCoefficient;

	public string explosionSoundString;

	[Header("Overlap Parameters")]
	public float overlapDamageCoefficient;

	public float overlapProcCoefficient;

	public float overlapForce;

	public float overlapFireFrequency;

	public float overlapResetFrequency;

	public float overlapVehicleDurationBonusPerHit;

	public GameObject overlapHitEffectPrefab;

	private float age;

	private bool hasDetonatedServer;

	private VehicleSeat vehicleSeat;

	private Rigidbody rigidbody;

	private OverlapAttack overlapAttack;

	private float overlapFireAge;

	private float overlapResetAge;

	private void Awake()
	{
		vehicleSeat = GetComponent<VehicleSeat>();
		vehicleSeat.onPassengerEnter += OnPassengerEnter;
		vehicleSeat.onPassengerExit += OnPassengerExit;
		rigidbody = GetComponent<Rigidbody>();
	}

	private void OnPassengerExit(GameObject passenger)
	{
		if (NetworkServer.active)
		{
			DetonateServer();
		}
		foreach (CameraRigController readOnlyInstances in CameraRigController.readOnlyInstancesList)
		{
			if (readOnlyInstances.target == passenger)
			{
				readOnlyInstances.SetOverrideCam(this, 0f);
				readOnlyInstances.SetOverrideCam(null, cameraLerpTime);
			}
		}
	}

	private void OnPassengerEnter(GameObject passenger)
	{
		if ((bool)vehicleSeat.currentPassengerInputBank)
		{
			Vector3 aimDirection = vehicleSeat.currentPassengerInputBank.aimDirection;
			rigidbody.rotation = Quaternion.LookRotation(aimDirection);
			rigidbody.velocity = aimDirection * initialSpeed;
			CharacterBody currentPassengerBody = vehicleSeat.currentPassengerBody;
			overlapAttack = new OverlapAttack
			{
				attacker = currentPassengerBody.gameObject,
				damage = overlapDamageCoefficient * currentPassengerBody.damage,
				pushAwayForce = overlapForce,
				isCrit = currentPassengerBody.RollCrit(),
				damageColorIndex = DamageColorIndex.Item,
				inflictor = base.gameObject,
				procChainMask = default(ProcChainMask),
				procCoefficient = overlapProcCoefficient,
				teamIndex = currentPassengerBody.teamComponent.teamIndex,
				hitBoxGroup = base.gameObject.GetComponent<HitBoxGroup>(),
				hitEffectPrefab = overlapHitEffectPrefab
			};
		}
	}

	private void DetonateServer()
	{
		if (!hasDetonatedServer)
		{
			hasDetonatedServer = true;
			CharacterBody currentPassengerBody = vehicleSeat.currentPassengerBody;
			if ((bool)currentPassengerBody)
			{
				EffectData effectData = new EffectData
				{
					origin = base.transform.position,
					scale = blastRadius
				};
				EffectManager.SpawnEffect(explosionEffectPrefab, effectData, transmit: true);
				BlastAttack blastAttack = new BlastAttack();
				blastAttack.attacker = currentPassengerBody.gameObject;
				blastAttack.baseDamage = blastDamageCoefficient * currentPassengerBody.damage;
				blastAttack.baseForce = blastForce;
				blastAttack.bonusForce = blastBonusForce;
				blastAttack.attackerFiltering = AttackerFiltering.NeverHitSelf;
				blastAttack.crit = currentPassengerBody.RollCrit();
				blastAttack.damageColorIndex = DamageColorIndex.Item;
				blastAttack.damageType = blastDamageType;
				blastAttack.falloffModel = blastFalloffModel;
				blastAttack.inflictor = base.gameObject;
				blastAttack.position = base.transform.position;
				blastAttack.procChainMask = default(ProcChainMask);
				blastAttack.procCoefficient = blastProcCoefficient;
				blastAttack.radius = blastRadius;
				blastAttack.teamIndex = currentPassengerBody.teamComponent.teamIndex;
				blastAttack.Fire();
			}
			Util.PlaySound(explosionSoundString, base.gameObject);
			Object.Destroy(base.gameObject);
		}
	}

	private void FixedUpdate()
	{
		if (!vehicleSeat || !vehicleSeat.currentPassengerInputBank)
		{
			return;
		}
		age += Time.fixedDeltaTime;
		overlapFireAge += Time.fixedDeltaTime;
		overlapResetAge += Time.fixedDeltaTime;
		if (NetworkServer.active)
		{
			if (overlapFireAge > 1f / overlapFireFrequency)
			{
				if (overlapAttack.Fire())
				{
					age = Mathf.Max(0f, age - overlapVehicleDurationBonusPerHit);
				}
				overlapFireAge = 0f;
			}
			if (overlapResetAge >= 1f / overlapResetFrequency)
			{
				overlapAttack.ResetIgnoredHealthComponents();
				overlapResetAge = 0f;
			}
		}
		Ray aimRay = vehicleSeat.currentPassengerInputBank.GetAimRay();
		aimRay = CameraRigController.ModifyAimRayIfApplicable(aimRay, base.gameObject, out var _);
		Vector3 velocity = rigidbody.velocity;
		Vector3 target = aimRay.direction * targetSpeed;
		Vector3 vector = Vector3.MoveTowards(velocity, target, acceleration * Time.fixedDeltaTime);
		rigidbody.MoveRotation(Quaternion.LookRotation(aimRay.direction));
		rigidbody.AddForce(vector - velocity, ForceMode.VelocityChange);
		if (NetworkServer.active && duration <= age)
		{
			DetonateServer();
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (detonateOnCollision && NetworkServer.active)
		{
			DetonateServer();
		}
	}

	public void GetCameraState(CameraRigController cameraRigController, ref CameraState cameraState)
	{
	}

	public bool IsUserLookAllowed(CameraRigController cameraRigController)
	{
		return true;
	}

	public bool IsUserControlAllowed(CameraRigController cameraRigController)
	{
		return true;
	}

	public bool IsHudAllowed(CameraRigController cameraRigController)
	{
		return true;
	}
}
