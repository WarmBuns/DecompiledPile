using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Assassin2;

public class Hide : BaseState
{
	private Transform modelTransform;

	public static GameObject hideEfffectPrefab;

	public static GameObject smokeEffectPrefab;

	public static Material destealthMaterial;

	private float stopwatch;

	private Vector3 blinkDestination = Vector3.zero;

	private Vector3 blinkStart = Vector3.zero;

	[Tooltip("the length of time to stay hidden")]
	public static float hiddenDuration = 5f;

	[Tooltip("the entire duration of the hidden state (hidden time + time after)")]
	public static float fullDuration = 10f;

	public static string beginSoundString;

	public static string endSoundString;

	private Animator animator;

	private CharacterModel characterModel;

	private HurtBoxGroup hurtboxGroup;

	private bool hidden;

	private GameObject smokeEffectInstance;

	private static int DisappearStateHash = Animator.StringToHash("Disappear");

	private static int AppearStateHash = Animator.StringToHash("Appear");

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(beginSoundString, base.gameObject);
		modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			animator = modelTransform.GetComponent<Animator>();
			characterModel = modelTransform.GetComponent<CharacterModel>();
			hurtboxGroup = modelTransform.GetComponent<HurtBoxGroup>();
			if ((bool)smokeEffectPrefab)
			{
				Transform transform = modelTransform;
				if ((bool)transform)
				{
					smokeEffectInstance = Object.Instantiate(smokeEffectPrefab, transform);
					ScaleParticleSystemDuration component = smokeEffectInstance.GetComponent<ScaleParticleSystemDuration>();
					if ((bool)component)
					{
						component.newDuration = component.initialDuration;
					}
				}
			}
		}
		if ((bool)hurtboxGroup)
		{
			HurtBoxGroup hurtBoxGroup = hurtboxGroup;
			int hurtBoxesDeactivatorCounter = hurtBoxGroup.hurtBoxesDeactivatorCounter + 1;
			hurtBoxGroup.hurtBoxesDeactivatorCounter = hurtBoxesDeactivatorCounter;
		}
		PlayAnimation("Gesture", DisappearStateHash);
		if ((bool)base.characterBody && NetworkServer.active)
		{
			base.characterBody.AddBuff(RoR2Content.Buffs.Cloak);
		}
		CreateHiddenEffect(Util.GetCorePosition(base.gameObject));
		if ((bool)base.healthComponent)
		{
			base.healthComponent.dontShowHealthbar = true;
		}
		hidden = true;
	}

	private void CreateHiddenEffect(Vector3 origin)
	{
		EffectData effectData = new EffectData();
		effectData.origin = origin;
		EffectManager.SpawnEffect(hideEfffectPrefab, effectData, transmit: false);
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
		if (stopwatch >= hiddenDuration && hidden)
		{
			Reveal();
		}
		if (base.isAuthority && stopwatch > fullDuration)
		{
			outer.SetNextStateToMain();
		}
	}

	private void Reveal()
	{
		Util.PlaySound(endSoundString, base.gameObject);
		CreateHiddenEffect(Util.GetCorePosition(base.gameObject));
		if ((bool)modelTransform && (bool)destealthMaterial)
		{
			TemporaryOverlayInstance temporaryOverlayInstance = TemporaryOverlayManager.AddOverlay(animator.gameObject);
			temporaryOverlayInstance.duration = 1f;
			temporaryOverlayInstance.destroyComponentOnEnd = true;
			temporaryOverlayInstance.originalMaterial = destealthMaterial;
			temporaryOverlayInstance.inspectorCharacterModel = animator.gameObject.GetComponent<CharacterModel>();
			temporaryOverlayInstance.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
			temporaryOverlayInstance.animateShaderAlpha = true;
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
		PlayAnimation("Gesture", AppearStateHash);
		if ((bool)base.characterBody && NetworkServer.active)
		{
			base.characterBody.RemoveBuff(RoR2Content.Buffs.Cloak);
		}
		if ((bool)base.healthComponent)
		{
			base.healthComponent.dontShowHealthbar = false;
		}
		if ((bool)smokeEffectInstance)
		{
			EntityState.Destroy(smokeEffectInstance);
		}
		hidden = false;
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
