using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(NetworkedBodyAttachment))]
public class JetpackController : NetworkBehaviour
{
	private static readonly List<JetpackController> instancesList;

	private NetworkedBodyAttachment networkedBodyAttachment;

	public float duration;

	public float acceleration;

	public float boostSpeedMultiplier = 3f;

	public float boostCooldown = 0.5f;

	private float stopwatch;

	private Transform wingTransform;

	private Animator wingAnimator;

	private GameObject[] wingMotions;

	private GameObject wingMeshObject;

	private float boostCooldownTimer;

	private bool hasBegunSoundLoop;

	private ICharacterGravityParameterProvider targetCharacterGravityParameterProvider;

	private ICharacterFlightParameterProvider targetCharacterFlightParameterProvider;

	private bool _providingAntiGravity;

	private bool _providingFlight;

	private static int wingsReadyParamHash;

	private static int flyParamHash;

	private static int kRpcRpcResetTimer;

	private CharacterBody targetBody => networkedBodyAttachment.attachedBody;

	private bool providingAntiGravity
	{
		get
		{
			return _providingAntiGravity;
		}
		set
		{
			if (_providingAntiGravity != value)
			{
				_providingAntiGravity = value;
				if (targetCharacterGravityParameterProvider != null)
				{
					CharacterGravityParameters gravityParameters = targetCharacterGravityParameterProvider.gravityParameters;
					gravityParameters.channeledAntiGravityGranterCount += (_providingAntiGravity ? 1 : (-1));
					targetCharacterGravityParameterProvider.gravityParameters = gravityParameters;
				}
			}
		}
	}

	private bool providingFlight
	{
		get
		{
			return _providingFlight;
		}
		set
		{
			if (_providingFlight != value)
			{
				_providingFlight = value;
				if (targetCharacterFlightParameterProvider != null)
				{
					CharacterFlightParameters flightParameters = targetCharacterFlightParameterProvider.flightParameters;
					flightParameters.channeledFlightGranterCount += (_providingFlight ? 1 : (-1));
					targetCharacterFlightParameterProvider.flightParameters = flightParameters;
				}
			}
		}
	}

	public static JetpackController FindJetpackController(GameObject targetObject)
	{
		if (!targetObject)
		{
			return null;
		}
		for (int i = 0; i < instancesList.Count; i++)
		{
			if (instancesList[i].networkedBodyAttachment.attachedBodyObject == targetObject)
			{
				return instancesList[i];
			}
		}
		return null;
	}

	private void Awake()
	{
		networkedBodyAttachment = GetComponent<NetworkedBodyAttachment>();
	}

	private void Start()
	{
		SetupWings();
		if ((bool)targetBody)
		{
			targetCharacterGravityParameterProvider = targetBody.GetComponent<ICharacterGravityParameterProvider>();
			targetCharacterFlightParameterProvider = targetBody.GetComponent<ICharacterFlightParameterProvider>();
			StartFlight();
		}
	}

	private void StartFlight()
	{
		providingAntiGravity = true;
		providingFlight = true;
		if (targetBody.hasEffectiveAuthority && (bool)targetBody.characterMotor && targetBody.characterMotor.isGrounded)
		{
			Vector3 velocity = targetBody.characterMotor.velocity;
			velocity.y = 15f;
			targetBody.characterMotor.velocity = velocity;
			targetBody.characterMotor.Motor.ForceUnground();
		}
	}

	private void OnDestroy()
	{
		ShowMotionLines(showWings: false);
		if ((bool)targetBody)
		{
			providingFlight = false;
			targetCharacterFlightParameterProvider = null;
			providingAntiGravity = false;
			targetCharacterGravityParameterProvider = null;
		}
	}

	private void OnEnable()
	{
		instancesList.Add(this);
	}

	private void OnDisable()
	{
		instancesList.Remove(this);
	}

	private void FixedUpdate()
	{
		stopwatch += Time.fixedDeltaTime;
		boostCooldownTimer -= Time.fixedDeltaTime;
		if ((bool)targetBody)
		{
			base.transform.position = targetBody.transform.position;
			if (targetBody.hasEffectiveAuthority && (bool)targetBody.characterMotor)
			{
				if (stopwatch < duration)
				{
					if (boostCooldownTimer <= 0f && targetBody.inputBank.jump.justPressed && targetBody.inputBank.moveVector != Vector3.zero)
					{
						boostCooldownTimer = boostCooldown;
						targetBody.characterMotor.velocity = targetBody.inputBank.moveVector * (targetBody.moveSpeed * boostSpeedMultiplier);
						targetBody.characterMotor.disableAirControlUntilCollision = false;
					}
				}
				else
				{
					Vector3 velocity = targetBody.characterMotor.velocity;
					velocity.y = Mathf.Max(velocity.y, -5f);
					targetBody.characterMotor.velocity = velocity;
					providingAntiGravity = false;
					providingFlight = false;
				}
			}
		}
		if (stopwatch >= duration)
		{
			bool flag = !targetBody.characterMotor || !targetBody.characterMotor.isGrounded;
			if ((bool)wingAnimator && !flag)
			{
				wingAnimator.SetBool(wingsReadyParamHash, value: false);
			}
			if (NetworkServer.active && !flag)
			{
				Object.Destroy(base.gameObject);
			}
			return;
		}
		float num = 4f;
		if ((bool)targetBody.characterMotor)
		{
			float magnitude = targetBody.characterMotor.velocity.magnitude;
			float moveSpeed = targetBody.moveSpeed;
			if (magnitude != 0f && moveSpeed != 0f)
			{
				num += magnitude / moveSpeed * 6f;
			}
		}
		if ((bool)wingAnimator)
		{
			wingAnimator.SetBool(wingsReadyParamHash, value: true);
			wingAnimator.SetFloat(flyParamHash, num, 0.1f, Time.fixedDeltaTime);
			ShowMotionLines(showWings: true);
		}
	}

	public void ResetTimer()
	{
		stopwatch = 0f;
		StartFlight();
		if (NetworkServer.active)
		{
			CallRpcResetTimer();
		}
	}

	[ClientRpc]
	private void RpcResetTimer()
	{
		if (!NetworkServer.active)
		{
			ResetTimer();
		}
	}

	private Transform FindWings()
	{
		ModelLocator modelLocator = targetBody.modelLocator;
		if ((bool)modelLocator)
		{
			Transform modelTransform = modelLocator.modelTransform;
			if ((bool)modelTransform)
			{
				CharacterModel component = modelTransform.GetComponent<CharacterModel>();
				if ((bool)component)
				{
					List<GameObject> equipmentDisplayObjects = component.GetEquipmentDisplayObjects(RoR2Content.Equipment.Jetpack.equipmentIndex);
					if (equipmentDisplayObjects.Count > 0)
					{
						return equipmentDisplayObjects[0].transform;
					}
				}
			}
		}
		return null;
	}

	private void ShowMotionLines(bool showWings)
	{
		if (wingMotions == null)
		{
			return;
		}
		for (int i = 0; i < wingMotions.Length; i++)
		{
			if ((bool)wingMotions[i])
			{
				wingMotions[i].SetActive(showWings);
			}
		}
		if ((bool)wingMeshObject)
		{
			wingMeshObject.SetActive(!showWings);
		}
		if (hasBegunSoundLoop != showWings)
		{
			if (showWings)
			{
				Util.PlaySound("Play_item_use_bugWingFlapLoop", base.gameObject);
			}
			else
			{
				Util.PlaySound("Stop_item_use_bugWingFlapLoop", base.gameObject);
			}
			hasBegunSoundLoop = showWings;
		}
	}

	public void SetupWings()
	{
		wingTransform = FindWings();
		if ((bool)wingTransform)
		{
			wingAnimator = wingTransform.GetComponentInChildren<Animator>();
			ChildLocator component = wingTransform.GetComponent<ChildLocator>();
			if ((bool)wingAnimator)
			{
				wingAnimator.SetBool(wingsReadyParamHash, value: true);
			}
			if ((bool)component)
			{
				wingMotions = new GameObject[4];
				wingMotions[0] = component.FindChild("WingMotionLargeL").gameObject;
				wingMotions[1] = component.FindChild("WingMotionLargeR").gameObject;
				wingMotions[2] = component.FindChild("WingMotionSmallL").gameObject;
				wingMotions[3] = component.FindChild("WingMotionSmallR").gameObject;
				wingMeshObject = component.FindChild("WingMesh").gameObject;
			}
		}
	}

	static JetpackController()
	{
		instancesList = new List<JetpackController>();
		wingsReadyParamHash = Animator.StringToHash("wingsReady");
		flyParamHash = Animator.StringToHash("fly.playbackRate");
		kRpcRpcResetTimer = 1278379706;
		NetworkBehaviour.RegisterRpcDelegate(typeof(JetpackController), kRpcRpcResetTimer, InvokeRpcRpcResetTimer);
		NetworkCRC.RegisterBehaviour("JetpackController", 0);
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeRpcRpcResetTimer(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcResetTimer called on server.");
		}
		else
		{
			((JetpackController)obj).RpcResetTimer();
		}
	}

	public void CallRpcResetTimer()
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcResetTimer called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcResetTimer);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		SendRPCInternal(networkWriter, 0, "RpcResetTimer");
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
