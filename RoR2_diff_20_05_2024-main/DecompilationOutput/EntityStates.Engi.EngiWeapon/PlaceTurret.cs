using RoR2;
using UnityEngine;

namespace EntityStates.Engi.EngiWeapon;

public class PlaceTurret : BaseState
{
	private struct PlacementInfo
	{
		public bool ok;

		public Vector3 position;

		public Quaternion rotation;
	}

	[SerializeField]
	public GameObject wristDisplayPrefab;

	[SerializeField]
	public string placeSoundString;

	[SerializeField]
	public GameObject blueprintPrefab;

	[SerializeField]
	public GameObject turretMasterPrefab;

	private const float placementMaxUp = 1f;

	private const float placementMaxDown = 3f;

	private const float placementForwardDistance = 2f;

	private const float entryDelay = 0.1f;

	private const float exitDelay = 0.25f;

	private const float turretRadius = 0.5f;

	private const float turretHeight = 1.82f;

	private const float turretCenter = 0f;

	private const float turretModelYOffset = -0.75f;

	private GameObject wristDisplayObject;

	private BlueprintController blueprints;

	private float exitCountdown;

	private bool exitPending;

	private float entryCountdown;

	private static int PrepTurretStateHash = Animator.StringToHash("PrepTurret");

	private static int PlaceTurretStateHash = Animator.StringToHash("PlaceTurret");

	private PlacementInfo currentPlacementInfo;

	public override void OnEnter()
	{
		base.OnEnter();
		if (base.isAuthority)
		{
			currentPlacementInfo = GetPlacementInfo();
			blueprints = Object.Instantiate(blueprintPrefab, currentPlacementInfo.position, currentPlacementInfo.rotation).GetComponent<BlueprintController>();
		}
		PlayAnimation("Gesture", PrepTurretStateHash);
		entryCountdown = 0.1f;
		exitCountdown = 0.25f;
		exitPending = false;
		if (!base.modelLocator)
		{
			return;
		}
		ChildLocator component = base.modelLocator.modelTransform.GetComponent<ChildLocator>();
		if ((bool)component)
		{
			Transform transform = component.FindChild("WristDisplay");
			if ((bool)transform)
			{
				wristDisplayObject = Object.Instantiate(wristDisplayPrefab, transform);
			}
		}
	}

	private PlacementInfo GetPlacementInfo()
	{
		Ray aimRay = GetAimRay();
		Vector3 direction = aimRay.direction;
		direction.y = 0f;
		direction.Normalize();
		aimRay.direction = direction;
		PlacementInfo result = default(PlacementInfo);
		result.ok = false;
		result.rotation = Util.QuaternionSafeLookRotation(-direction);
		Ray ray = new Ray(aimRay.GetPoint(2f) + Vector3.up * 1f, Vector3.down);
		float num = 4f;
		float num2 = num;
		if (Physics.SphereCast(ray, 0.5f, out var hitInfo, num, LayerIndex.world.mask) && hitInfo.normal.y > 0.5f)
		{
			num2 = hitInfo.distance;
			result.ok = true;
		}
		Vector3 point = ray.GetPoint(num2 + 0.5f);
		result.position = point;
		if (result.ok)
		{
			float num3 = Mathf.Max(1.82f, 0f);
			if (Physics.CheckCapsule(result.position + Vector3.up * (num3 - 0.5f), result.position + Vector3.up * 0.5f, 0.45f, (int)LayerIndex.world.mask | (int)LayerIndex.CommonMasks.characterBodiesOrDefault))
			{
				result.ok = false;
			}
		}
		return result;
	}

	private void DestroyBlueprints()
	{
		if ((bool)blueprints)
		{
			EntityState.Destroy(blueprints.gameObject);
			blueprints = null;
		}
	}

	public override void OnExit()
	{
		base.OnExit();
		PlayAnimation("Gesture", PlaceTurretStateHash);
		if ((bool)wristDisplayObject)
		{
			EntityState.Destroy(wristDisplayObject);
		}
		DestroyBlueprints();
	}

	public override void Update()
	{
		base.Update();
		currentPlacementInfo = GetPlacementInfo();
		if ((bool)blueprints)
		{
			blueprints.PushState(currentPlacementInfo.position, currentPlacementInfo.rotation, currentPlacementInfo.ok);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!base.isAuthority)
		{
			return;
		}
		entryCountdown -= GetDeltaTime();
		if (exitPending)
		{
			exitCountdown -= GetDeltaTime();
			if (exitCountdown <= 0f)
			{
				outer.SetNextStateToMain();
			}
		}
		else
		{
			if (!base.inputBank || !(entryCountdown <= 0f))
			{
				return;
			}
			if ((base.inputBank.skill1.down || base.inputBank.skill4.justPressed) && currentPlacementInfo.ok)
			{
				if ((bool)base.characterBody)
				{
					base.characterBody.SendConstructTurret(base.characterBody, currentPlacementInfo.position, currentPlacementInfo.rotation, MasterCatalog.FindMasterIndex(turretMasterPrefab));
					if ((bool)base.skillLocator)
					{
						GenericSkill skill = base.skillLocator.GetSkill(SkillSlot.Special);
						if ((bool)skill)
						{
							skill.DeductStock(1);
						}
					}
				}
				Util.PlaySound(placeSoundString, base.gameObject);
				DestroyBlueprints();
				exitPending = true;
			}
			if (base.inputBank.skill2.justPressed)
			{
				DestroyBlueprints();
				exitPending = true;
			}
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
