using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Captain.Weapon;

public class SetupSupplyDrop : BaseState
{
	public struct PlacementInfo
	{
		public bool ok;

		public Vector3 position;

		public Quaternion rotation;

		public void Serialize(NetworkWriter writer)
		{
			writer.Write(ok);
			writer.Write(position);
			writer.Write(rotation);
		}

		public void Deserialize(NetworkReader reader)
		{
			ok = reader.ReadBoolean();
			position = reader.ReadVector3();
			rotation = reader.ReadQuaternion();
		}
	}

	public static GameObject crosshairOverridePrefab;

	public static string enterSoundString;

	public static string exitSoundString;

	public static GameObject effectMuzzlePrefab;

	public static string effectMuzzleString;

	public static float baseExitDuration;

	public static float maxPlacementDistance;

	public static GameObject blueprintPrefab;

	public static float normalYThreshold;

	private PlacementInfo currentPlacementInfo;

	private CrosshairUtils.OverrideRequest crosshairOverrideRequest;

	private GenericSkill primarySkillSlot;

	private AimAnimator modelAimAnimator;

	private GameObject effectMuzzleInstance;

	private Animator modelAnimator;

	private float timerSinceComplete;

	private bool beginExit;

	private GenericSkill originalPrimarySkill;

	private GenericSkill originalSecondarySkill;

	private BlueprintController blueprints;

	private CameraTargetParams.AimRequest aimRequest;

	private static int PrepSupplyDropStateHash = Animator.StringToHash("PrepSupplyDrop");

	private float exitDuration => baseExitDuration / attackSpeedStat;

	public override void OnEnter()
	{
		base.OnEnter();
		modelAnimator = GetModelAnimator();
		PlayAnimation("Gesture, Override", PrepSupplyDropStateHash);
		if ((bool)modelAnimator)
		{
			modelAnimator.SetBool(PrepSupplyDropStateHash, value: true);
		}
		Transform transform = FindModelChild(effectMuzzleString);
		if ((bool)transform)
		{
			effectMuzzleInstance = Object.Instantiate(effectMuzzlePrefab, transform);
		}
		if ((bool)crosshairOverridePrefab)
		{
			crosshairOverrideRequest = CrosshairUtils.RequestOverrideForBody(base.characterBody, crosshairOverridePrefab, CrosshairUtils.OverridePriority.Skill);
		}
		Util.PlaySound(enterSoundString, base.gameObject);
		blueprints = Object.Instantiate(blueprintPrefab, currentPlacementInfo.position, currentPlacementInfo.rotation).GetComponent<BlueprintController>();
		if ((bool)base.cameraTargetParams)
		{
			aimRequest = base.cameraTargetParams.RequestAimType(CameraTargetParams.AimType.Aura);
		}
		originalPrimarySkill = base.skillLocator.primary;
		originalSecondarySkill = base.skillLocator.secondary;
		base.skillLocator.primary = base.skillLocator.FindSkill("SupplyDrop1");
		base.skillLocator.secondary = base.skillLocator.FindSkill("SupplyDrop2");
	}

	public override void Update()
	{
		base.Update();
		currentPlacementInfo = GetPlacementInfo(GetAimRay(), base.gameObject);
		if ((bool)blueprints)
		{
			blueprints.PushState(currentPlacementInfo.position, currentPlacementInfo.rotation, currentPlacementInfo.ok);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if ((bool)base.characterDirection)
		{
			base.characterDirection.moveVector = GetAimRay().direction;
		}
		if (base.isAuthority && beginExit)
		{
			timerSinceComplete += GetDeltaTime();
			if (timerSinceComplete > exitDuration)
			{
				outer.SetNextStateToMain();
			}
		}
	}

	public override void OnExit()
	{
		if (!outer.destroying)
		{
			Util.PlaySound(exitSoundString, base.gameObject);
		}
		if ((bool)effectMuzzleInstance)
		{
			EntityState.Destroy(effectMuzzleInstance);
		}
		crosshairOverrideRequest?.Dispose();
		base.skillLocator.primary = originalPrimarySkill;
		base.skillLocator.secondary = originalSecondarySkill;
		if ((bool)modelAnimator)
		{
			modelAnimator.SetBool(PrepSupplyDropStateHash, value: false);
		}
		if ((bool)blueprints)
		{
			EntityState.Destroy(blueprints.gameObject);
			blueprints = null;
		}
		aimRequest?.Dispose();
		base.OnExit();
	}

	public static PlacementInfo GetPlacementInfo(Ray aimRay, GameObject gameObject)
	{
		float extraRaycastDistance = 0f;
		CameraRigController.ModifyAimRayIfApplicable(aimRay, gameObject, out extraRaycastDistance);
		Vector3 vector = -aimRay.direction;
		Vector3 vector2 = Vector3.up;
		Vector3 lhs = Vector3.Cross(vector2, vector);
		PlacementInfo result = default(PlacementInfo);
		result.ok = false;
		if (Physics.Raycast(aimRay, out var hitInfo, maxPlacementDistance, LayerIndex.world.mask) && hitInfo.normal.y > normalYThreshold)
		{
			vector2 = hitInfo.normal;
			vector = Vector3.Cross(lhs, vector2);
			result.ok = true;
		}
		result.rotation = Util.QuaternionSafeLookRotation(vector, vector2);
		Vector3 point = hitInfo.point;
		result.position = point;
		return result;
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
