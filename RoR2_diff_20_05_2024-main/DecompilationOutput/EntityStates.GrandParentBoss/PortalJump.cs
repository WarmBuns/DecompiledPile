using System.Linq;
using RoR2;
using RoR2.Navigation;
using UnityEngine;

namespace EntityStates.GrandParentBoss;

public class PortalJump : BaseState
{
	private BullseyeSearch enemyFinder;

	public static float duration = 3f;

	public static float retreatDuration = 2.433f;

	public static float emergeDuration = 2.933f;

	public static float portalScaleDuration = 2f;

	public static float effectsDuration = 2f;

	private bool retreatDone;

	private bool teleported;

	private bool canMoveDuringTeleport;

	private bool hasEmerged;

	private HurtBox foundBullseye;

	public static float telezoneRadius;

	public static float skillDistance = 2000f;

	private float stopwatch;

	private Vector3 destinationPressence = Vector3.zero;

	private Vector3 startPressence = Vector3.zero;

	private Transform modelTransform;

	private Animator animator;

	private CharacterModel characterModel;

	private HurtBoxGroup hurtboxGroup;

	public static GameObject jumpInEffectPrefab;

	public static GameObject jumpOutEffectPrefab;

	public static Vector3 teleportOffset;

	private GrandparentEnergyFXController FXController;

	private static int RetreatStateHash = Animator.StringToHash("Retreat");

	private static int RetreatParamHash = Animator.StringToHash("retreat.playbackRate");

	private static int EmergeStateHash = Animator.StringToHash("Emerge");

	private static int EmergeParamHash = Animator.StringToHash("emerge.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			animator = modelTransform.GetComponent<Animator>();
			characterModel = modelTransform.GetComponent<CharacterModel>();
			hurtboxGroup = modelTransform.GetComponent<HurtBoxGroup>();
			PlayAnimation("Body", RetreatStateHash, RetreatParamHash, retreatDuration);
			EffectData effectData = new EffectData
			{
				origin = base.characterBody.modelLocator.modelTransform.GetComponent<ChildLocator>().FindChild("Portal").position
			};
			EffectManager.SpawnEffect(jumpInEffectPrefab, effectData, transmit: true);
		}
		FXController = base.characterBody.GetComponent<GrandparentEnergyFXController>();
		if ((bool)FXController)
		{
			FXController.portalObject = base.characterBody.modelLocator.modelTransform.GetComponent<ChildLocator>().FindChild("Portal").GetComponentInChildren<EffectComponent>()
				.gameObject;
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		stopwatch += GetDeltaTime();
		if (stopwatch >= retreatDuration && !retreatDone)
		{
			retreatDone = true;
			if ((bool)FXController)
			{
				ScaleObject(FXController.portalObject, scaleUp: false);
			}
		}
		if (stopwatch >= retreatDuration + portalScaleDuration && !teleported)
		{
			teleported = true;
			canMoveDuringTeleport = true;
			if ((bool)FXController)
			{
				FXController.portalObject.GetComponent<ObjectScaleCurve>().enabled = false;
			}
			DoTeleport();
		}
		if ((bool)base.characterMotor && (bool)base.characterDirection)
		{
			base.characterMotor.velocity = Vector3.zero;
		}
		if (canMoveDuringTeleport)
		{
			SetPosition(Vector3.Lerp(startPressence, destinationPressence, stopwatch / duration));
		}
		if (stopwatch >= retreatDuration + portalScaleDuration + duration && canMoveDuringTeleport)
		{
			canMoveDuringTeleport = false;
			if ((bool)FXController)
			{
				FXController.portalObject.transform.position = base.characterBody.modelLocator.modelTransform.GetComponent<ChildLocator>().FindChild("Portal").position;
				ScaleObject(FXController.portalObject, scaleUp: true);
			}
		}
		if (stopwatch >= retreatDuration + portalScaleDuration * 2f + duration && !hasEmerged)
		{
			hasEmerged = true;
			if ((bool)FXController)
			{
				FXController.portalObject.GetComponent<ObjectScaleCurve>().enabled = false;
			}
			modelTransform = GetModelTransform();
			if ((bool)modelTransform)
			{
				PlayAnimation("Body", EmergeStateHash, EmergeParamHash, duration);
				EffectData effectData = new EffectData
				{
					origin = base.characterBody.modelLocator.modelTransform.GetComponent<ChildLocator>().FindChild("Portal").position
				};
				EffectManager.SpawnEffect(jumpOutEffectPrefab, effectData, transmit: true);
				if ((bool)characterModel)
				{
					characterModel.invisibilityCount--;
				}
				if ((bool)hurtboxGroup)
				{
					HurtBoxGroup hurtBoxGroup = hurtboxGroup;
					int hurtBoxesDeactivatorCounter = hurtBoxGroup.hurtBoxesDeactivatorCounter - 1;
					hurtBoxGroup.hurtBoxesDeactivatorCounter = hurtBoxesDeactivatorCounter;
				}
				if ((bool)base.characterMotor)
				{
					base.characterMotor.enabled = true;
				}
			}
		}
		if (stopwatch >= retreatDuration + portalScaleDuration * 2f + duration + emergeDuration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	private void DoTeleport()
	{
		Ray aimRay = GetAimRay();
		enemyFinder = new BullseyeSearch();
		enemyFinder.maxDistanceFilter = skillDistance;
		enemyFinder.searchOrigin = aimRay.origin;
		enemyFinder.searchDirection = aimRay.direction;
		enemyFinder.filterByLoS = false;
		enemyFinder.sortMode = BullseyeSearch.SortMode.Distance;
		enemyFinder.teamMaskFilter = TeamMask.allButNeutral;
		if ((bool)base.teamComponent)
		{
			enemyFinder.teamMaskFilter.RemoveTeam(base.teamComponent.teamIndex);
		}
		enemyFinder.RefreshCandidates();
		foundBullseye = enemyFinder.GetResults().LastOrDefault();
		modelTransform = GetModelTransform();
		if ((bool)characterModel)
		{
			characterModel.invisibilityCount++;
		}
		if ((bool)hurtboxGroup)
		{
			HurtBoxGroup hurtBoxGroup = hurtboxGroup;
			int hurtBoxesDeactivatorCounter = hurtBoxGroup.hurtBoxesDeactivatorCounter + 1;
			hurtBoxGroup.hurtBoxesDeactivatorCounter = hurtBoxesDeactivatorCounter;
		}
		if ((bool)base.characterMotor)
		{
			base.characterMotor.enabled = false;
		}
		Vector3 vector = base.inputBank.moveVector * skillDistance;
		destinationPressence = base.transform.position;
		startPressence = base.transform.position;
		NodeGraph groundNodes = SceneInfo.instance.groundNodes;
		Vector3 position = startPressence + vector;
		if ((bool)foundBullseye)
		{
			position = foundBullseye.transform.position;
		}
		NodeGraph.NodeIndex nodeIndex = groundNodes.FindClosestNode(position, base.characterBody.hullClassification);
		groundNodes.GetNodePosition(nodeIndex, out destinationPressence);
		destinationPressence += base.transform.position - base.characterBody.footPosition;
	}

	private void SetPosition(Vector3 newPosition)
	{
		if ((bool)base.characterMotor)
		{
			base.characterMotor.Motor.SetPositionAndRotation(newPosition, Quaternion.identity);
		}
	}

	private void ScaleObject(GameObject objectToScaleDown, bool scaleUp)
	{
		float valueEnd = (scaleUp ? 1f : 0f);
		float valueStart = (scaleUp ? 0f : 1f);
		ObjectScaleCurve component = objectToScaleDown.GetComponent<ObjectScaleCurve>();
		component.timeMax = portalScaleDuration;
		component.curveX = AnimationCurve.Linear(0f, valueStart, 1f, valueEnd);
		component.curveY = AnimationCurve.Linear(0f, valueStart, 1f, valueEnd);
		component.curveZ = AnimationCurve.Linear(0f, valueStart, 1f, valueEnd);
		component.overallCurve = AnimationCurve.EaseInOut(0f, valueStart, 1f, valueEnd);
		component.enabled = true;
	}

	public override void OnExit()
	{
		base.OnExit();
	}
}
