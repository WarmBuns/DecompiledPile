using RoR2;
using RoR2.Navigation;
using UnityEngine;

namespace EntityStates.ParentMonster;

public class LoomingPresence : BaseState
{
	private Transform modelTransform;

	public static GameObject blinkPrefab;

	public static Material destealthMaterial;

	private float stopwatch;

	private Vector3 blinkDestination = Vector3.zero;

	private Vector3 blinkStart = Vector3.zero;

	public static float duration = 0.3f;

	public static float blinkDistance = 25f;

	public static string beginSoundString;

	public static string endSoundString;

	public static float destealthDuration;

	private CharacterModel characterModel;

	private HurtBoxGroup hurtboxGroup;

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(beginSoundString, base.gameObject);
		modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			characterModel = modelTransform.GetComponent<CharacterModel>();
			hurtboxGroup = modelTransform.GetComponent<HurtBoxGroup>();
		}
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
		if (base.isAuthority)
		{
			Vector3 vector = base.inputBank.aimDirection * blinkDistance;
			blinkDestination = base.transform.position;
			blinkStart = base.transform.position;
			NodeGraph groundNodes = SceneInfo.instance.groundNodes;
			NodeGraph.NodeIndex nodeIndex = groundNodes.FindClosestNode(base.transform.position + vector, base.characterBody.hullClassification);
			groundNodes.GetNodePosition(nodeIndex, out blinkDestination);
			blinkDestination += base.transform.position - base.characterBody.footPosition;
			vector = blinkDestination - blinkStart;
			CreateBlinkEffect(Util.GetCorePosition(base.gameObject), vector);
		}
	}

	private void CreateBlinkEffect(Vector3 origin, Vector3 direction)
	{
		EffectData effectData = new EffectData();
		effectData.rotation = Util.QuaternionSafeLookRotation(direction);
		effectData.origin = origin;
		EffectManager.SpawnEffect(blinkPrefab, effectData, transmit: true);
	}

	private void SetPosition(Vector3 newPosition)
	{
		if ((bool)base.characterMotor)
		{
			base.characterMotor.Motor.SetPositionAndRotation(newPosition, Quaternion.identity);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		stopwatch += GetDeltaTime();
		if ((bool)base.characterMotor && (bool)base.characterDirection)
		{
			base.characterMotor.velocity = Vector3.zero;
		}
		SetPosition(Vector3.Lerp(blinkStart, blinkDestination, stopwatch / duration));
		if (stopwatch >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override void OnExit()
	{
		Util.PlaySound(endSoundString, base.gameObject);
		modelTransform = GetModelTransform();
		if ((bool)base.characterDirection)
		{
			base.characterDirection.forward = GetAimRay().direction;
		}
		if ((bool)modelTransform && (bool)destealthMaterial)
		{
			TemporaryOverlayInstance temporaryOverlayInstance = TemporaryOverlayManager.AddOverlay(modelTransform.gameObject);
			temporaryOverlayInstance.duration = destealthDuration;
			temporaryOverlayInstance.destroyComponentOnEnd = true;
			temporaryOverlayInstance.originalMaterial = destealthMaterial;
			temporaryOverlayInstance.inspectorCharacterModel = modelTransform.gameObject.GetComponent<CharacterModel>();
			temporaryOverlayInstance.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
			temporaryOverlayInstance.animateShaderAlpha = true;
		}
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
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
