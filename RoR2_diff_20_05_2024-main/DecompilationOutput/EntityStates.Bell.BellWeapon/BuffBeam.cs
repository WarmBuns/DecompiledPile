using System.Linq;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Bell.BellWeapon;

public class BuffBeam : BaseState
{
	public static float duration;

	public static GameObject buffBeamPrefab;

	public static AnimationCurve beamWidthCurve;

	public static string playBeamSoundString;

	public static string stopBeamSoundString;

	public HurtBox target;

	private float healTimer;

	private float healInterval;

	private float healChunk;

	private CharacterBody targetBody;

	private GameObject buffBeamInstance;

	private BezierCurveLine healBeamCurve;

	private Transform muzzleTransform;

	private Transform beamTipTransform;

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(playBeamSoundString, base.gameObject);
		Ray aimRay = GetAimRay();
		BullseyeSearch bullseyeSearch = new BullseyeSearch();
		bullseyeSearch.teamMaskFilter = TeamMask.none;
		if ((bool)base.teamComponent)
		{
			bullseyeSearch.teamMaskFilter.AddTeam(base.teamComponent.teamIndex);
		}
		bullseyeSearch.filterByLoS = false;
		bullseyeSearch.maxDistanceFilter = 50f;
		bullseyeSearch.maxAngleFilter = 180f;
		bullseyeSearch.searchOrigin = aimRay.origin;
		bullseyeSearch.searchDirection = aimRay.direction;
		bullseyeSearch.sortMode = BullseyeSearch.SortMode.Angle;
		bullseyeSearch.RefreshCandidates();
		bullseyeSearch.FilterOutGameObject(base.gameObject);
		target = bullseyeSearch.GetResults().FirstOrDefault();
		Debug.LogFormat("Buffing target {0}", target);
		if ((bool)target)
		{
			targetBody = target.healthComponent.body;
			targetBody.AddBuff(RoR2Content.Buffs.HiddenInvincibility.buffIndex);
		}
		string childName = "Muzzle";
		Transform modelTransform = GetModelTransform();
		if (!modelTransform)
		{
			return;
		}
		ChildLocator component = modelTransform.GetComponent<ChildLocator>();
		if ((bool)component)
		{
			muzzleTransform = component.FindChild(childName);
			buffBeamInstance = Object.Instantiate(buffBeamPrefab);
			ChildLocator component2 = buffBeamInstance.GetComponent<ChildLocator>();
			if ((bool)component2)
			{
				beamTipTransform = component2.FindChild("BeamTip");
			}
			healBeamCurve = buffBeamInstance.GetComponentInChildren<BezierCurveLine>();
		}
	}

	private void UpdateHealBeamVisuals()
	{
		float widthMultiplier = beamWidthCurve.Evaluate(base.age / duration);
		healBeamCurve.lineRenderer.widthMultiplier = widthMultiplier;
		healBeamCurve.v0 = muzzleTransform.forward * 3f;
		healBeamCurve.transform.position = muzzleTransform.position;
		if ((bool)target)
		{
			beamTipTransform.position = targetBody.mainHurtBox.transform.position;
		}
	}

	public override void Update()
	{
		base.Update();
		UpdateHealBeamVisuals();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if ((base.fixedAge >= duration || target == null) && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override void OnExit()
	{
		Util.PlaySound(stopBeamSoundString, base.gameObject);
		EntityState.Destroy(buffBeamInstance);
		if ((bool)targetBody)
		{
			targetBody.RemoveBuff(RoR2Content.Buffs.HiddenInvincibility.buffIndex);
		}
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Any;
	}

	public override void OnSerialize(NetworkWriter writer)
	{
		base.OnSerialize(writer);
		HurtBoxReference.FromHurtBox(target).Write(writer);
	}

	public override void OnDeserialize(NetworkReader reader)
	{
		base.OnDeserialize(reader);
		HurtBoxReference hurtBoxReference = default(HurtBoxReference);
		hurtBoxReference.Read(reader);
		target = hurtBoxReference.ResolveGameObject()?.GetComponent<HurtBox>();
	}
}
