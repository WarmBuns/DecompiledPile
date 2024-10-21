using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(EntityStateMachine))]
[RequireComponent(typeof(VehicleSeat))]
public sealed class SurvivorPodController : NetworkBehaviour, ICameraStateProvider
{
	private EntityStateMachine stateMachine;

	[Tooltip("The bone which controls the camera during the entry animation.")]
	public Transform cameraBone;

	public bool exitAllowed;

	public EntityStateMachine characterStateMachine { get; private set; }

	public VehicleSeat vehicleSeat { get; private set; }

	private void Awake()
	{
		stateMachine = GetComponent<EntityStateMachine>();
		vehicleSeat = GetComponent<VehicleSeat>();
		vehicleSeat.onPassengerEnter += OnPassengerEnter;
		vehicleSeat.onPassengerExit += OnPassengerExit;
		vehicleSeat.enterVehicleAllowedCheck.AddCallback(CheckEnterAllowed);
		vehicleSeat.exitVehicleAllowedCheck.AddCallback(CheckExitAllowed);
	}

	private void OnPassengerEnter(GameObject passenger)
	{
		UpdateCameras(passenger);
	}

	private void OnPassengerExit(GameObject passenger)
	{
		UpdateCameras(null);
		vehicleSeat.enabled = false;
	}

	private void CheckEnterAllowed(CharacterBody characterBody, ref Interactability? resultOverride)
	{
		resultOverride = Interactability.Disabled;
	}

	private void CheckExitAllowed(CharacterBody characterBody, ref Interactability? resultOverride)
	{
		resultOverride = (exitAllowed ? Interactability.Available : Interactability.Disabled);
	}

	private void Update()
	{
		UpdateCameras(vehicleSeat.currentPassengerBody ? vehicleSeat.currentPassengerBody.gameObject : null);
	}

	private void UpdateCameras(GameObject characterBodyObject)
	{
		foreach (CameraRigController readOnlyInstances in CameraRigController.readOnlyInstancesList)
		{
			if ((bool)characterBodyObject && readOnlyInstances.target == characterBodyObject)
			{
				readOnlyInstances.SetOverrideCam(this, 0f);
			}
			else if (readOnlyInstances.IsOverrideCam(this))
			{
				readOnlyInstances.SetOverrideCam(null, 0.05f);
			}
		}
	}

	void ICameraStateProvider.GetCameraState(CameraRigController cameraRigController, ref CameraState cameraState)
	{
		Vector3 position = base.transform.position;
		Vector3 position2 = cameraBone.position;
		Vector3 direction = position2 - position;
		Ray ray = new Ray(position, direction);
		Vector3 position3 = position2;
		if (Physics.Raycast(ray, out var hitInfo, direction.magnitude, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
		{
			position3 = ray.GetPoint(Mathf.Max(hitInfo.distance - 0.25f, 0.25f));
		}
		cameraState = new CameraState
		{
			position = position3,
			rotation = cameraBone.rotation,
			fov = 60f
		};
	}

	bool ICameraStateProvider.IsUserLookAllowed(CameraRigController cameraRigController)
	{
		return false;
	}

	bool ICameraStateProvider.IsUserControlAllowed(CameraRigController cameraRigController)
	{
		return true;
	}

	bool ICameraStateProvider.IsHudAllowed(CameraRigController cameraRigController)
	{
		return true;
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
