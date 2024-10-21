using System.Linq;
using RoR2;
using UnityEngine;

namespace EntityStates.GrandParentBoss;

public class PortalFist : BaseState
{
	public static float baseDuration;

	public static GameObject portalInEffectPrefab;

	public static GameObject portalOutEffectPrefab;

	public static string portalMuzzleString;

	public static string fistMeshChildLocatorString;

	public static string fistBoneChildLocatorString;

	public static string mecanimFistVisibilityString;

	public static float fistOverlayDuration;

	public static Material fistOverlayMaterial;

	public static float targetSearchMaxDistance;

	private GameObject fistMeshGameObject;

	private GameObject fistBoneGameObject;

	private GameObject fistTargetGameObject;

	private string fistTargetMuzzleString;

	private Animator modelAnimator;

	private CharacterModel characterModel;

	private float duration;

	private bool fistWasOutOfPortal = true;

	public override void OnEnter()
	{
		base.OnEnter();
		fistMeshGameObject = FindModelChild(fistMeshChildLocatorString).gameObject;
		fistBoneGameObject = FindModelChild(fistBoneChildLocatorString).gameObject;
		modelAnimator = GetModelAnimator();
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			characterModel = modelTransform.GetComponent<CharacterModel>();
		}
		duration = baseDuration / attackSpeedStat;
		PlayCrossfade("Body", "PortalFist", "PortalFist.playbackRate", duration, 0.3f);
		Transform transform = FindModelChild("PortalFistTargetRig");
		BullseyeSearch bullseyeSearch = new BullseyeSearch();
		bullseyeSearch.viewer = base.characterBody;
		bullseyeSearch.searchOrigin = base.characterBody.corePosition;
		bullseyeSearch.searchDirection = base.characterBody.corePosition;
		bullseyeSearch.maxDistanceFilter = targetSearchMaxDistance;
		bullseyeSearch.teamMaskFilter = TeamMask.GetEnemyTeams(GetTeam());
		bullseyeSearch.teamMaskFilter.RemoveTeam(TeamIndex.Neutral);
		bullseyeSearch.sortMode = BullseyeSearch.SortMode.DistanceAndAngle;
		bullseyeSearch.RefreshCandidates();
		HurtBox hurtBox = bullseyeSearch.GetResults().FirstOrDefault();
		if ((bool)hurtBox)
		{
			transform.position = hurtBox.transform.position;
			fistTargetMuzzleString = $"PortalFistTargetPosition{Random.Range(1, 5).ToString()}";
			fistTargetGameObject = FindModelChild(fistTargetMuzzleString).gameObject;
		}
	}

	public override void Update()
	{
		base.Update();
		bool flag = modelAnimator.GetFloat(mecanimFistVisibilityString) > 0.5f;
		if (flag != fistWasOutOfPortal)
		{
			fistWasOutOfPortal = flag;
			EffectManager.SimpleMuzzleFlash(flag ? portalOutEffectPrefab : portalInEffectPrefab, base.gameObject, portalMuzzleString, transmit: false);
			EffectManager.SimpleMuzzleFlash(flag ? portalOutEffectPrefab : portalInEffectPrefab, base.gameObject, fistTargetMuzzleString, transmit: false);
			if ((bool)characterModel && (bool)fistOverlayMaterial)
			{
				TemporaryOverlayInstance temporaryOverlayInstance = TemporaryOverlayManager.AddOverlay(characterModel.gameObject);
				temporaryOverlayInstance.duration = fistOverlayDuration;
				temporaryOverlayInstance.destroyComponentOnEnd = true;
				temporaryOverlayInstance.originalMaterial = fistOverlayMaterial;
				temporaryOverlayInstance.inspectorCharacterModel = characterModel;
				temporaryOverlayInstance.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
				temporaryOverlayInstance.animateShaderAlpha = true;
			}
		}
		if ((bool)fistTargetGameObject && !flag)
		{
			fistBoneGameObject.transform.SetPositionAndRotation(fistTargetGameObject.transform.position, fistTargetGameObject.transform.rotation);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge > duration)
		{
			outer.SetNextStateToMain();
		}
	}
}
