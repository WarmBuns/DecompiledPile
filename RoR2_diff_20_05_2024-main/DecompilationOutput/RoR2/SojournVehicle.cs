using System.Collections.Generic;
using EntityStates.Seeker;
using RoR2.HudOverlay;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace RoR2;

[RequireComponent(typeof(VehicleSeat))]
public class SojournVehicle : NetworkBehaviour, ICameraStateProvider
{
	[Header("Vehicle Parameters")]
	public float nonLinearSpeedRate = 0.15f;

	public float nonLinearSpeedInterval = 1f;

	private float currentSpeed;

	public float startingSpeedBoost = 25f;

	private float startingSpeed;

	public float endSpeedTarget;

	public float endSpeedBodyInfluance = 1f;

	private float trueEndSpeed;

	public float cameraLerpTime = 1f;

	public float cameraForward = 2f;

	[Header("Blast Parameters")]
	public bool detonateOnCollision;

	public GameObject explosionEffectPrefab;

	public float blastDamageCoefficient;

	public float blastForce;

	public BlastAttack.FalloffModel blastFalloffModel;

	public DamageTypeCombo blastDamageType;

	public Vector3 blastBonusForce;

	public float blastProcCoefficient;

	public string explosionSoundString;

	public GameObject healingExplosionPrefab;

	public float dmgAddedPerSecond = 0.5f;

	[Header("Blast Growth Rate")]
	private float blastRadius;

	public float minBlastSize = 4f;

	public float baseMaxBlastSize = 10f;

	public float perChakraAddMax = 2f;

	public float perChakraAddMin = 0.5f;

	public float blastNonlinearGrowthRate = 1f;

	[Tooltip("These are the explosion size indicators. They will be scaled up by blast size.")]
	public GameObject[] blastRadiusIndicators = new GameObject[0];

	[Tooltip("This is the scale of the visible size indicators.")]
	public float blastRadiusIndicatorScale = 0.125f;

	[Tooltip("This will be multiplied by the bast size before calculating collision. Larger will expand the collider.")]
	public float blastRadiusCollisionMult = 1f;

	public GameObject effectsToFlatten;

	[Header("Overlap Parameters")]
	public float overlapDamageCoefficient;

	public float overlapProcCoefficient;

	public float overlapForce;

	public float overlapFirePeriod;

	public float overlapVehicleDurationBonusPerHit;

	public GameObject overlapHitEffectPrefab;

	private float age;

	private VehicleSeat vehicleSeat;

	private Rigidbody vehicleRigidbody;

	[Header("Camera Parameters")]
	public PlayerCharacterMasterController playerCharacterMasterController;

	public GameObject sojournOverlayPrefab;

	private CameraTargetParams cameraTargetParams;

	private CharacterBody characterBody;

	[Header("Flight Parameters")]
	private SeekerController seekerController;

	private float increaseAgeCheck;

	public float agePerTenthSecondsCheck = 0.1f;

	public float incrementalFlightDamage = 0.075f;

	[FormerlySerializedAs("initialFlightDamageDelay")]
	public float initialFlightGracePeriod = 0.3f;

	public float afterGraceLerpDownTime = 1f;

	[Header("Healing Parameters")]
	private float healingOverShield;

	private float healingExplosionAmount;

	private bool skillKeyWasDown = true;

	private bool skillHeldDown = true;

	private OverlayController overlayController;

	private List<ImageFillController> fillUiList = new List<ImageFillController>();

	private ChildLocator overlayInstanceChildLocator;

	private OverlapAttack overlapAttack;

	private float overlapFireAge;

	private float overlapResetAge;

	private float agePerSecond;

	private float totalHealth;

	public Sojourn sojournState;

	public float barrierPercentage;

	public GameObject effectGameObject;

	public GameObject hitBox;

	private LocalUser localUser;

	private static int kRpcRpcEndSojournState;

	private void CacheLocalUser()
	{
		localUser = null;
		foreach (NetworkUser readOnlyLocalPlayers in NetworkUser.readOnlyLocalPlayersList)
		{
			if (readOnlyLocalPlayers.master == characterBody.master)
			{
				localUser = readOnlyLocalPlayers.localUser;
			}
		}
	}

	private void Awake()
	{
		playerCharacterMasterController = GetComponent<PlayerCharacterMasterController>();
		vehicleSeat = GetComponent<VehicleSeat>();
		vehicleSeat.onPassengerEnter += OnPassengerEnter;
		vehicleSeat.onPassengerExit += OnPassengerExit;
		vehicleRigidbody = GetComponent<Rigidbody>();
		effectGameObject = base.transform.Find("Effect").gameObject;
		hitBox = base.transform.Find("HitBox").gameObject;
		increaseAgeCheck = agePerTenthSecondsCheck;
	}

	private void Start()
	{
		overlayController = HudOverlayManager.AddOverlay(base.gameObject, new OverlayCreationParams
		{
			prefab = sojournOverlayPrefab,
			childLocatorEntry = "CrosshairExtras"
		});
		totalHealth = characterBody.baseMaxHealth + characterBody.levelMaxHealth * (float)((int)characterBody.level - 1);
		UpdateBlastRadius();
	}

	private void OnPassengerExit(GameObject passenger)
	{
		foreach (CameraRigController readOnlyInstances in CameraRigController.readOnlyInstancesList)
		{
			if (readOnlyInstances.target == passenger)
			{
				readOnlyInstances.SetOverrideCam(this, 0f);
				readOnlyInstances.SetOverrideCam(null, cameraLerpTime);
			}
		}
		DetonateServer();
	}

	[ClientRpc]
	private void RpcEndSojournState()
	{
		Util.PlaySound(explosionSoundString, base.gameObject);
		seekerController.doesExitVehicle = true;
	}

	private void OnPassengerEnter(GameObject passenger)
	{
		skillHeldDown = vehicleSeat.currentPassengerInputBank.skill3.down;
		vehicleSeat.currentPassengerBody.GetComponent<InteractionDriver>().interactableOverride = null;
		Vector3 aimDirection = vehicleSeat.currentPassengerInputBank.aimDirection;
		vehicleRigidbody.rotation = Quaternion.LookRotation(aimDirection);
		characterBody = vehicleSeat.currentPassengerBody;
		seekerController = characterBody.transform.GetComponent<SeekerController>();
		CacheLocalUser();
		overlapAttack = new OverlapAttack
		{
			attacker = characterBody.gameObject,
			damage = 0f,
			pushAwayForce = overlapForce,
			isCrit = characterBody.RollCrit(),
			damageColorIndex = DamageColorIndex.Item,
			inflictor = base.gameObject,
			procChainMask = default(ProcChainMask),
			procCoefficient = overlapProcCoefficient,
			teamIndex = characterBody.teamComponent.teamIndex,
			hitBoxGroup = base.gameObject.GetComponent<HitBoxGroup>(),
			hitEffectPrefab = overlapHitEffectPrefab,
			damageType = DamageType.Stun1s
		};
		startingSpeed = characterBody.moveSpeed + startingSpeedBoost;
		trueEndSpeed = endSpeedBodyInfluance * characterBody.moveSpeed + endSpeedTarget;
		currentSpeed = startingSpeed;
		vehicleRigidbody.velocity = aimDirection * currentSpeed;
	}

	[Server]
	private void DetonateServer()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.SojournVehicle::DetonateServer()' called on client");
			return;
		}
		float num = blastRadius * blastRadiusCollisionMult;
		EffectData effectData = new EffectData
		{
			origin = base.transform.position,
			scale = num
		};
		EffectManager.SpawnEffect(explosionEffectPrefab, effectData, transmit: true);
		BlastAttack blastAttack = new BlastAttack();
		blastAttack.attacker = characterBody.gameObject;
		blastAttack.baseDamage = blastDamageCoefficient * characterBody.damage;
		blastAttack.baseForce = blastForce;
		blastAttack.bonusForce = blastBonusForce;
		blastAttack.attackerFiltering = AttackerFiltering.NeverHitSelf;
		blastAttack.crit = characterBody.RollCrit();
		blastAttack.damageColorIndex = DamageColorIndex.Item;
		blastAttack.damageType = blastDamageType;
		blastAttack.falloffModel = blastFalloffModel;
		blastAttack.inflictor = base.gameObject;
		blastAttack.position = base.transform.position;
		blastAttack.procChainMask = default(ProcChainMask);
		blastAttack.procCoefficient = blastProcCoefficient;
		blastAttack.radius = num;
		blastAttack.teamIndex = characterBody.teamComponent.teamIndex;
		blastAttack.Fire();
		Util.PlaySound(explosionSoundString, base.gameObject);
		seekerController.doesExitVehicle = true;
		CallRpcEndSojournState();
		Object.Destroy(base.gameObject);
	}

	private void Update()
	{
		effectsToFlatten.transform.rotation = Quaternion.identity;
	}

	private void FixedUpdate()
	{
		age += Time.fixedDeltaTime;
		if (NetworkServer.active)
		{
			FixedUpdateOverlayAttack();
			if (age > agePerTenthSecondsCheck)
			{
				FixedUpdatePerTenthSecond();
				agePerTenthSecondsCheck += increaseAgeCheck;
			}
		}
		FixedUpdateMovement();
		if (age > agePerSecond)
		{
			if (age > initialFlightGracePeriod)
			{
				blastForce += 25f;
				characterBody.AddBuff(DLC2Content.Buffs.SojournHealing);
				blastDamageCoefficient += dmgAddedPerSecond;
			}
			agePerSecond += 1f;
		}
		if (age > initialFlightGracePeriod)
		{
			float normalizedHealth = characterBody.healthComponent.GetNormalizedHealth();
			float b = Mathf.Lerp(trueEndSpeed, startingSpeed, normalizedHealth);
			float t = Mathf.Clamp01((age - initialFlightGracePeriod) / afterGraceLerpDownTime);
			currentSpeed = Mathf.LerpUnclamped(startingSpeed, b, t);
		}
		UpdateBlastRadius();
		if (localUser != null && vehicleSeat != null)
		{
			bool flag = localUser?.userProfile.toggleSeekerSojourn ?? true;
			bool? flag2 = vehicleSeat.currentPassengerInputBank?.skill3.down;
			if ((!flag && !flag2.Value) || (flag && flag2.Value && !skillKeyWasDown))
			{
				vehicleSeat.currentPassengerInputBank.skill3.hasPressBeenClaimed = true;
				vehicleSeat.CallCmdEjectPassenger();
			}
			skillKeyWasDown = vehicleSeat.currentPassengerInputBank?.skill3.down ?? false;
		}
		_ = effectGameObject.transform.position != hitBox.transform.position;
	}

	private void UpdateBlastRadius()
	{
		int buffCount = characterBody.GetBuffCount(DLC2Content.Buffs.ChakraBuff);
		float t = Util.ConvertAmplificationPercentageIntoReductionNormalized(blastNonlinearGrowthRate * age);
		blastRadius = Mathf.Lerp(minBlastSize + perChakraAddMin * (float)buffCount, baseMaxBlastSize + perChakraAddMax * (float)buffCount, t);
		Vector3 localScale = Vector3.one * (blastRadius * blastRadiusIndicatorScale);
		GameObject[] array = blastRadiusIndicators;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].transform.localScale = localScale;
		}
	}

	[Server]
	private void FixedUpdateOverlayAttack()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.SojournVehicle::FixedUpdateOverlayAttack()' called on client");
			return;
		}
		overlapFireAge += Time.fixedDeltaTime;
		overlapResetAge += Time.fixedDeltaTime;
		if (overlapFireAge > overlapFirePeriod)
		{
			overlapAttack.Fire();
			overlapFireAge = 0f;
		}
	}

	private void FixedUpdateMovement()
	{
		if (vehicleSeat.currentPassengerInputBank != null)
		{
			Ray aimRay = vehicleSeat.currentPassengerInputBank.GetAimRay();
			aimRay = CameraRigController.ModifyAimRayIfApplicable(aimRay, base.gameObject, out var _);
			vehicleRigidbody.MoveRotation(Quaternion.LookRotation(aimRay.direction));
			vehicleRigidbody.velocity = aimRay.direction * currentSpeed;
		}
	}

	[Server]
	private void FixedUpdatePerTenthSecond()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.SojournVehicle::FixedUpdatePerTenthSecond()' called on client");
			return;
		}
		float damage = totalHealth * incrementalFlightDamage;
		DamageInfo damageInfo = new DamageInfo
		{
			damageType = DamageTypeExtended.SojournVehicleDamage,
			damage = damage,
			position = base.gameObject.transform.position
		};
		if (age > initialFlightGracePeriod)
		{
			if (characterBody.healthComponent.health < totalHealth * 0.15f)
			{
				vehicleSeat.CallCmdEjectPassenger();
			}
			else
			{
				characterBody.healthComponent.TakeDamage(damageInfo);
			}
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (detonateOnCollision && NetworkServer.active)
		{
			vehicleSeat.CallCmdEjectPassenger();
		}
	}

	public void GetCameraState(CameraRigController cameraRigController, ref CameraState cameraState)
	{
		cameraState.position = base.transform.position + base.transform.forward * cameraForward;
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

	private void UNetVersion()
	{
	}

	protected static void InvokeRpcRpcEndSojournState(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcEndSojournState called on server.");
		}
		else
		{
			((SojournVehicle)obj).RpcEndSojournState();
		}
	}

	public void CallRpcEndSojournState()
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcEndSojournState called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcEndSojournState);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		SendRPCInternal(networkWriter, 0, "RpcEndSojournState");
	}

	static SojournVehicle()
	{
		kRpcRpcEndSojournState = -1858892214;
		NetworkBehaviour.RegisterRpcDelegate(typeof(SojournVehicle), kRpcRpcEndSojournState, InvokeRpcRpcEndSojournState);
		NetworkCRC.RegisterBehaviour("SojournVehicle", 0);
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		bool result = default(bool);
		return result;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
	}

	public override void PreStartClient()
	{
	}
}
