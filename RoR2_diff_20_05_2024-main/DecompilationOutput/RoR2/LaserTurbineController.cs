using EntityStates.LaserTurbine;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(EntityStateMachine))]
[RequireComponent(typeof(GenericOwnership))]
[RequireComponent(typeof(NetworkIdentity))]
[RequireComponent(typeof(NetworkStateMachine))]
public class LaserTurbineController : NetworkBehaviour
{
	public float visualSpinRate = 7200f;

	public Transform chargeIndicator;

	public Transform spinIndicator;

	public Transform turbineDisplayRoot;

	public bool showTurbineDisplay;

	public string spinRtpc;

	public float spinRtpcScale;

	public float visualSpin;

	public float visualSpinDecayRate = 0.2f;

	private GenericOwnership genericOwnership;

	private CharacterBody cachedOwnerBody;

	public float charge { get; private set; }

	private void Awake()
	{
		genericOwnership = GetComponent<GenericOwnership>();
		genericOwnership.onOwnerChanged += OnOwnerChanged;
	}

	private void Update()
	{
		if (NetworkClient.active)
		{
			UpdateClient();
		}
	}

	private void FixedUpdate()
	{
		int killChargeCount = 0;
		if ((bool)cachedOwnerBody)
		{
			killChargeCount = cachedOwnerBody.GetBuffCount(RoR2Content.Buffs.LaserTurbineKillCharge);
		}
		charge = CalcCurrentChargeValue(killChargeCount);
		if ((bool)turbineDisplayRoot)
		{
			turbineDisplayRoot.gameObject.SetActive(showTurbineDisplay);
		}
	}

	private void OnEnable()
	{
		if (NetworkServer.active)
		{
			GlobalEventManager.onCharacterDeathGlobal += OnCharacterDeathGlobalServer;
		}
	}

	private void OnDisable()
	{
		GlobalEventManager.onCharacterDeathGlobal -= OnCharacterDeathGlobalServer;
	}

	[Client]
	private void UpdateClient()
	{
		if (!NetworkClient.active)
		{
			Debug.LogWarning("[Client] function 'System.Void RoR2.LaserTurbineController::UpdateClient()' called on server");
			return;
		}
		if (visualSpin <= charge)
		{
			visualSpin = charge;
		}
		else
		{
			visualSpin -= visualSpinDecayRate * Time.deltaTime;
		}
		visualSpin = Mathf.Max(visualSpin, 0f);
		float num = HGMath.CircleAreaToRadius(visualSpin * HGMath.CircleRadiusToArea(1f));
		chargeIndicator.localScale = new Vector3(num, num, num);
		Vector3 localEulerAngles = spinIndicator.localEulerAngles;
		localEulerAngles.y += visualSpin * Time.deltaTime * visualSpinRate;
		spinIndicator.localEulerAngles = localEulerAngles;
		AkSoundEngine.SetRTPCValue(spinRtpc, visualSpin * spinRtpcScale, base.gameObject);
	}

	[Server]
	public void ExpendCharge()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.LaserTurbineController::ExpendCharge()' called on client");
		}
		else if ((bool)cachedOwnerBody)
		{
			cachedOwnerBody.ClearTimedBuffs(RoR2Content.Buffs.LaserTurbineKillCharge);
		}
	}

	private void OnCharacterDeathGlobalServer(DamageReport damageReport)
	{
		if ((object)damageReport.attacker == genericOwnership.ownerObject && (object)damageReport.attacker != null)
		{
			OnOwnerKilledOtherServer();
		}
	}

	private void OnOwnerKilledOtherServer()
	{
		if ((bool)cachedOwnerBody)
		{
			cachedOwnerBody.AddTimedBuff(RoR2Content.Buffs.LaserTurbineKillCharge, RechargeState.killChargeDuration, RechargeState.killChargesRequired);
		}
	}

	private void OnOwnerChanged(GameObject newOwner)
	{
		cachedOwnerBody = (newOwner ? newOwner.GetComponent<CharacterBody>() : null);
	}

	public float CalcCurrentChargeValue(int killChargeCount)
	{
		return Mathf.Clamp01((float)killChargeCount / (float)RechargeState.killChargesRequired);
	}

	private void UNetVersion()
	{
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
