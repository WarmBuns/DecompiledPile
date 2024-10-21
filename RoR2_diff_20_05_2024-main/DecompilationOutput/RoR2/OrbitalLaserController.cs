using System;
using EntityStates;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class OrbitalLaserController : MonoBehaviour
{
	private abstract class OrbitalLaserBaseState : BaseState
	{
		protected OrbitalLaserController controller;

		public override void OnEnter()
		{
			base.OnEnter();
			controller = GetComponent<OrbitalLaserController>();
		}
	}

	private class OrbitalLaserChargeState : OrbitalLaserBaseState
	{
		public override void OnEnter()
		{
			base.OnEnter();
			controller.chargeEffect.SetActive(value: true);
			controller.maxSpeed = controller.chargeMaxVelocity;
		}

		public override void OnExit()
		{
			controller.chargeEffect.SetActive(value: false);
			base.OnExit();
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();
			if (NetworkServer.active && base.fixedAge >= controller.chargeDuration)
			{
				outer.SetNextState(new OrbitalLaserFireState());
			}
		}
	}

	private class OrbitalLaserFireState : OrbitalLaserBaseState
	{
		private float bulletAttackTimer;

		public override void OnEnter()
		{
			base.OnEnter();
			controller.fireEffect.SetActive(value: true);
			controller.maxSpeed = controller.fireMaxVelocity;
		}

		public override void OnExit()
		{
			controller.fireEffect.SetActive(value: false);
			base.OnExit();
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();
			if (!NetworkServer.active)
			{
				return;
			}
			if (base.fixedAge >= controller.fireDuration || !controller.ownerBody)
			{
				outer.SetNextState(new OrbitalLaserDecayState());
				return;
			}
			bulletAttackTimer -= GetDeltaTime();
			if ((bool)controller.ownerBody && bulletAttackTimer < 0f)
			{
				bulletAttackTimer += 1f / controller.fireFrequency;
				BulletAttack bulletAttack = new BulletAttack();
				bulletAttack.owner = controller.ownerBody.gameObject;
				bulletAttack.weapon = base.gameObject;
				bulletAttack.origin = base.transform.position + Vector3.up * 600f;
				bulletAttack.maxDistance = 1200f;
				bulletAttack.aimVector = Vector3.down;
				bulletAttack.minSpread = 0f;
				bulletAttack.maxSpread = 0f;
				bulletAttack.damage = Mathf.Lerp(controller.damageCoefficientInitial, controller.damageCoefficientFinal, base.fixedAge / controller.fireDuration) * controller.ownerBody.damage / controller.fireFrequency;
				bulletAttack.force = controller.force;
				bulletAttack.tracerEffectPrefab = controller.tracerEffectPrefab;
				bulletAttack.muzzleName = "";
				bulletAttack.hitEffectPrefab = controller.hitEffectPrefab;
				bulletAttack.isCrit = Util.CheckRoll(controller.ownerBody.crit, controller.ownerBody.master);
				bulletAttack.stopperMask = LayerIndex.world.mask;
				bulletAttack.damageColorIndex = DamageColorIndex.Item;
				bulletAttack.procCoefficient = controller.procCoefficient / controller.fireFrequency;
				bulletAttack.radius = 2f;
				bulletAttack.Fire();
			}
		}
	}

	private class OrbitalLaserDecayState : OrbitalLaserBaseState
	{
		public override void OnEnter()
		{
			base.OnEnter();
			controller.maxSpeed = 0f;
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();
			if (NetworkServer.active && base.fixedAge >= controller.decayDuration)
			{
				EntityState.Destroy(base.gameObject);
			}
		}
	}

	[NonSerialized]
	public CharacterBody ownerBody;

	private InputBankTest ownerInputBank;

	[Header("Movement Parameters")]
	public float smoothDampTime = 0.3f;

	private Vector3 velocity;

	private float maxSpeed;

	[Header("Attack Parameters")]
	public float fireFrequency = 5f;

	public float damageCoefficientInitial = 6f;

	public float damageCoefficientFinal = 6f;

	public float procCoefficient = 0.5f;

	public float force;

	[Header("Charge")]
	public GameObject chargeEffect;

	public float chargeDuration = 3f;

	public float chargeMaxVelocity = 20f;

	private Transform chargeEffectTransform;

	[Header("Fire")]
	public GameObject fireEffect;

	public float fireDuration = 6f;

	public float fireMaxVelocity = 1f;

	public GameObject tracerEffectPrefab;

	public GameObject hitEffectPrefab;

	[Header("Decay")]
	public float decayDuration = 1.5f;

	[Header("Laser Pointer")]
	[Tooltip("The transform of the child laser pointer effect.")]
	public Transform laserPointerEffectTransform;

	[Tooltip("The transform of the muzzle effect.")]
	public Transform muzzleEffectTransform;

	private Vector3 mostRecentPointerPosition;

	private Vector3 mostRecentPointerNormal;

	private Vector3 mostRecentMuzzlePosition;

	private void Start()
	{
		chargeEffect.SetActive(value: true);
		chargeEffect.GetComponent<ObjectScaleCurve>().timeMax = chargeDuration;
		mostRecentPointerPosition = base.transform.position;
		mostRecentPointerNormal = Vector3.up;
	}

	private void UpdateLaserPointer()
	{
		if ((bool)ownerBody)
		{
			ownerInputBank = ownerBody.GetComponent<InputBankTest>();
			Ray ray = default(Ray);
			ray.origin = ownerInputBank.aimOrigin;
			ray.direction = ownerInputBank.aimDirection;
			Ray ray2 = ray;
			mostRecentMuzzlePosition = ray2.origin;
			float num = 900f;
			if (Physics.Raycast(ray2, out var hitInfo, num, (int)LayerIndex.world.mask | (int)LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore))
			{
				mostRecentPointerPosition = hitInfo.point;
			}
			else
			{
				mostRecentPointerPosition = ray2.GetPoint(num);
			}
			mostRecentPointerNormal = -ray2.direction;
		}
	}

	private void Update()
	{
		UpdateLaserPointer();
		laserPointerEffectTransform.SetPositionAndRotation(mostRecentPointerPosition, Quaternion.LookRotation(mostRecentPointerNormal));
		muzzleEffectTransform.SetPositionAndRotation(mostRecentMuzzlePosition, Quaternion.identity);
	}

	private void FixedUpdate()
	{
		UpdateLaserPointer();
		if (NetworkServer.active)
		{
			base.transform.position = Vector3.SmoothDamp(base.transform.position, mostRecentPointerPosition, ref velocity, smoothDampTime, maxSpeed, Time.fixedDeltaTime);
		}
	}
}
