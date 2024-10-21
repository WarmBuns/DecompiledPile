using RoR2;
using UnityEngine;

namespace EntityStates;

public class ShockState : BaseState
{
	public static GameObject stunVfxPrefab;

	public float shockDuration = 1f;

	public static float shockInterval = 0.1f;

	public static float shockStrength = 1f;

	public static float healthFractionToForceExit = 0.1f;

	public static string enterSoundString;

	public static string exitSoundString;

	private float shockTimer;

	private Animator animator;

	private TemporaryOverlayInstance temporaryOverlay;

	private float healthFraction;

	private GameObject stunVfxInstance;

	private static int Hurt1StateHash = Animator.StringToHash("Hurt1");

	private static int Hurt2StateHash = Animator.StringToHash("Hurt2");

	public override void OnEnter()
	{
		base.OnEnter();
		animator = GetModelAnimator();
		if ((bool)base.sfxLocator && base.sfxLocator.barkSound != "")
		{
			Util.PlaySound(base.sfxLocator.barkSound, base.gameObject);
		}
		Util.PlaySound(enterSoundString, base.gameObject);
		PlayAnimation("Body", (Random.Range(0, 2) == 0) ? Hurt1StateHash : Hurt2StateHash);
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			CharacterModel component = modelTransform.GetComponent<CharacterModel>();
			if ((bool)component)
			{
				temporaryOverlay = TemporaryOverlayManager.AddOverlay(base.gameObject);
				temporaryOverlay.duration = shockDuration;
				temporaryOverlay.originalMaterial = LegacyResourcesAPI.Load<Material>("Materials/matIsShocked");
				temporaryOverlay.AddToCharacterModel(component);
			}
		}
		stunVfxInstance = Object.Instantiate(stunVfxPrefab, base.transform);
		stunVfxInstance.GetComponent<ScaleParticleSystemDuration>().newDuration = shockDuration;
		if ((bool)base.characterBody.healthComponent)
		{
			healthFraction = base.characterBody.healthComponent.combinedHealthFraction;
		}
		if ((bool)base.characterBody)
		{
			base.characterBody.isSprinting = false;
		}
		if ((bool)base.characterDirection)
		{
			base.characterDirection.moveVector = base.characterDirection.forward;
		}
		if ((bool)base.rigidbodyMotor)
		{
			base.rigidbodyMotor.moveVector = Vector3.zero;
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		shockTimer -= GetDeltaTime();
		float combinedHealthFraction = base.characterBody.healthComponent.combinedHealthFraction;
		if (shockTimer <= 0f)
		{
			shockTimer += shockInterval;
			PlayShockAnimation();
		}
		if (base.fixedAge > shockDuration || healthFraction - combinedHealthFraction > healthFractionToForceExit)
		{
			outer.SetNextStateToMain();
		}
	}

	public override void OnExit()
	{
		if (temporaryOverlay != null)
		{
			temporaryOverlay.Destroy();
		}
		if ((bool)stunVfxInstance)
		{
			EntityState.Destroy(stunVfxInstance);
		}
		Util.PlaySound(exitSoundString, base.gameObject);
		base.OnExit();
	}

	private void PlayShockAnimation()
	{
		string layerName = "Flinch";
		int layerIndex = animator.GetLayerIndex(layerName);
		if (layerIndex >= 0)
		{
			animator.SetLayerWeight(layerIndex, shockStrength);
			animator.Play("FlinchStart", layerIndex);
		}
	}
}
