using System.Collections.Generic;
using RoR2;
using RoR2.HudOverlay;
using RoR2.UI;
using UnityEngine;
using UnityEngine.UI;

namespace EntityStates.Seeker;

public class MeditationUI : BaseSkillState
{
	[Header("UI")]
	[SerializeField]
	public GameObject scopeOverlayPrefab;

	[SerializeField]
	public float duration = 5f;

	[SerializeField]
	public float timePenalty = 1f;

	[SerializeField]
	public float failureCooldownPenalty = 5f;

	[SerializeField]
	public float animDuration;

	[SerializeField]
	public float smallHopVelocity;

	[SerializeField]
	public Sprite primaryIcon;

	[SerializeField]
	public Sprite secondaryIcon;

	[SerializeField]
	public Sprite utilityIcon;

	[SerializeField]
	public Sprite specialIcon;

	[SerializeField]
	public SeekerController seekerController;

	[SerializeField]
	public GameObject lotusPrefab;

	[SerializeField]
	public float wrapUpDuration;

	[SerializeField]
	public GameObject successEffectPrefab;

	[SerializeField]
	public GameObject crosshairOverridePrefab;

	[SerializeField]
	public string startSoundString;

	[SerializeField]
	public string stopSoundString;

	[SerializeField]
	public string timeoutSoundString;

	[SerializeField]
	public string successSoundString;

	[SerializeField]
	public string inputCorrectSoundString;

	[SerializeField]
	public string inputFailSoundString;

	[Header("Healing Explosion")]
	[SerializeField]
	public float healingExplosionAmount = 10f;

	[SerializeField]
	public float healingOverShield = 5f;

	[SerializeField]
	public float blastRadius = 5f;

	[SerializeField]
	public float damageCoefficient = 5f;

	[SerializeField]
	public float blastForce = 5f;

	[SerializeField]
	public Vector3 blastBonusForce;

	[SerializeField]
	public float blastProcCoefficient = 5f;

	[SerializeField]
	public float percentPerGate = 0.75f;

	[SerializeField]
	public float overShieldPercent = 0.25f;

	[SerializeField]
	public float startingOverShieldPercentage = 0.33f;

	private CrosshairUtils.OverrideRequest crosshairOverrideRequest;

	private GameObject lotus;

	private OverlayController overlayController;

	private bool meditationSuccess;

	private ICharacterGravityParameterProvider characterGravityParameterProvider;

	private ChildLocator overlayInstanceChildLocator;

	private ChildLocator lotusChildLocator;

	private List<ImageFillController> fillUiList = new List<ImageFillController>();

	private CameraTargetParams.AimRequest aimRequest;

	private bool spawnedLotus;

	private bool failed;

	private bool meditationWrapUp;

	private bool wrapUpAnimationPlayed;

	private float wrapUpTimer;

	private float startingYaw;

	private EntityStateMachine bodyStateMachine;

	private float explosionHealingMod;

	private bool upReady = true;

	private bool downReady = true;

	private bool rightReady = true;

	private bool leftReady = true;

	private static string[] c = new string[5] { "ArrowInput1", "ArrowInput2", "ArrowInput3", "ArrowInput4", "ArrowInput5" };

	public override void OnEnter()
	{
		base.OnEnter();
		EntityStateMachine[] components = base.characterBody.gameObject.GetComponents<EntityStateMachine>();
		foreach (EntityStateMachine entityStateMachine in components)
		{
			if (entityStateMachine.customName == "Body")
			{
				bodyStateMachine = entityStateMachine;
			}
		}
		bodyStateMachine.SetNextState(new Idle());
		explosionHealingMod = base.characterBody.maxHealth;
		if (explosionHealingMod < base.characterBody.baseMaxHealth)
		{
			explosionHealingMod = base.characterBody.baseMaxHealth;
		}
		base.healthComponent.AddBarrierAuthority(explosionHealingMod * startingOverShieldPercentage);
		seekerController = base.characterBody.transform.GetComponent<SeekerController>();
		if (base.isAuthority)
		{
			seekerController.RandomizeMeditationInputs();
		}
		SmallHop(base.characterMotor, smallHopVelocity);
		if ((bool)base.characterMotor)
		{
			base.characterMotor.walkSpeedPenaltyCoefficient = 0f;
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
		PlayAnimation("FullBody, Override", "MeditationStart", "Meditation.playbackRate", animDuration);
		characterGravityParameterProvider = base.gameObject.GetComponent<ICharacterGravityParameterProvider>();
		if (characterGravityParameterProvider != null)
		{
			CharacterGravityParameters gravityParameters = characterGravityParameterProvider.gravityParameters;
			gravityParameters.channeledAntiGravityGranterCount++;
			characterGravityParameterProvider.gravityParameters = gravityParameters;
		}
		overlayController = HudOverlayManager.AddOverlay(base.gameObject, new OverlayCreationParams
		{
			prefab = scopeOverlayPrefab,
			childLocatorEntry = "CrosshairExtras"
		});
		overlayController.onInstanceAdded += OnOverlayInstanceAdded;
		overlayController.onInstanceRemove += OnOverlayInstanceRemoved;
		crosshairOverrideRequest = CrosshairUtils.RequestOverrideForBody(base.characterBody, crosshairOverridePrefab, CrosshairUtils.OverridePriority.Skill);
		CameraTargetParams component = base.gameObject.GetComponent<CameraTargetParams>();
		if ((bool)component)
		{
			aimRequest = component.RequestAimType(CameraTargetParams.AimType.ZoomedOut);
		}
		startingYaw = base.characterDirection.yaw;
		Util.PlaySound(startSoundString, base.gameObject);
	}

	public override void Update()
	{
		if (seekerController.meditationUIDirty)
		{
			seekerController.meditationUIDirty = false;
			UpdateUIInputSequence();
		}
		base.characterDirection.yaw = startingYaw;
		base.characterDirection.moveVector = base.characterDirection.forward;
		base.Update();
		if (meditationWrapUp)
		{
			wrapUpTimer += Time.deltaTime;
			if (wrapUpTimer > wrapUpDuration)
			{
				outer.SetNextStateToMain();
			}
			if (!wrapUpAnimationPlayed)
			{
				wrapUpAnimationPlayed = true;
				if (meditationSuccess)
				{
					PlayAnimation("FullBody, Override", "MeditationSuccess", "Meditation.playbackRate", animDuration);
					Util.PlaySound(successSoundString, base.gameObject);
					lotus.GetComponentInChildren<Animator>().Play("MeditationSuccess");
					EffectManager.SpawnEffect(successEffectPrefab, new EffectData
					{
						origin = base.characterBody.corePosition
					}, transmit: true);
					if (base.isAuthority)
					{
						float num = 0f;
						float num2 = 0f;
						seekerController.IncrementChakraGate();
						if (base.characterBody.HasBuff(DLC2Content.Buffs.ChakraBuff))
						{
							num2 = base.characterBody.GetBuffCount(DLC2Content.Buffs.ChakraBuff);
							num = num2 * percentPerGate;
						}
						seekerController.CallCmdTriggerHealPulse((healingExplosionAmount * num2 + 0.04f) * explosionHealingMod, base.characterBody.corePosition, blastRadius);
						BlastAttack blastAttack = new BlastAttack();
						blastAttack.attacker = base.characterBody.gameObject;
						blastAttack.baseDamage = (damageCoefficient + num) * base.characterBody.damage;
						blastAttack.baseForce = blastForce;
						blastAttack.bonusForce = blastBonusForce;
						blastAttack.attackerFiltering = AttackerFiltering.NeverHitSelf;
						blastAttack.crit = base.characterBody.RollCrit();
						blastAttack.damageColorIndex = DamageColorIndex.Item;
						blastAttack.inflictor = base.gameObject;
						blastAttack.position = base.characterBody.corePosition;
						blastAttack.procChainMask = default(ProcChainMask);
						blastAttack.procCoefficient = blastProcCoefficient;
						blastAttack.radius = blastRadius;
						blastAttack.falloffModel = BlastAttack.FalloffModel.None;
						blastAttack.teamIndex = base.teamComponent.teamIndex;
						blastAttack.Fire();
					}
				}
				else
				{
					PlayAnimation("FullBody, Override", "MeditationFail", "Meditation.playbackRate", animDuration);
					Util.PlaySound(timeoutSoundString, base.gameObject);
					lotus.GetComponentInChildren<Animator>().Play("MeditationFail");
					outer.SetNextStateToMain();
				}
			}
		}
		int num3 = -1;
		if (base.inputBank.rawMoveUp.justPressed)
		{
			num3 = 0;
		}
		if (base.inputBank.rawMoveDown.justPressed)
		{
			num3 = 1;
		}
		if (base.inputBank.rawMoveRight.justPressed)
		{
			num3 = 2;
		}
		if (base.inputBank.rawMoveLeft.justPressed)
		{
			num3 = 3;
		}
		if (seekerController.meditationInputStep > 4)
		{
			meditationSuccess = true;
			meditationWrapUp = true;
		}
		if (num3 == seekerController.GetNextMeditationStep())
		{
			if (!wrapUpAnimationPlayed && seekerController.meditationInputStep < 5)
			{
				Util.PlaySound(inputCorrectSoundString, base.gameObject);
				seekerController.meditationInputStep++;
			}
		}
		else if (num3 != -1)
		{
			Util.PlaySound(inputFailSoundString, base.gameObject);
			base.fixedAge += timePenalty;
		}
		if (base.fixedAge > 0.4f)
		{
			base.characterMotor.velocity = new Vector3(0f, 0f, 0f);
			if (base.inputBank.skill4.justPressed)
			{
				meditationWrapUp = true;
			}
			Transform modelTransform = GetModelTransform();
			if ((bool)modelTransform && !spawnedLotus)
			{
				spawnedLotus = true;
				lotusChildLocator = modelTransform.GetComponent<ChildLocator>();
				lotus = Object.Instantiate(lotusPrefab, lotusChildLocator.FindChild("Lotus"), worldPositionStays: false);
			}
		}
		if (base.fixedAge > duration)
		{
			failed = true;
			meditationWrapUp = true;
		}
		else
		{
			UpdateUI();
		}
	}

	private void SetupInputUIIcons()
	{
		for (int i = 0; i < 5; i++)
		{
			switch (seekerController.meditationStepAndSequence[i + 1])
			{
			case 0:
				overlayInstanceChildLocator.FindChild(c[i]).GetComponent<RectTransform>().rotation = Quaternion.Euler(0f, 0f, 0f);
				break;
			case 1:
				overlayInstanceChildLocator.FindChild(c[i]).GetComponent<RectTransform>().rotation = Quaternion.Euler(0f, 0f, -180f);
				break;
			case 2:
				overlayInstanceChildLocator.FindChild(c[i]).GetComponent<RectTransform>().rotation = Quaternion.Euler(0f, 0f, -90f);
				break;
			case 3:
				overlayInstanceChildLocator.FindChild(c[i]).GetComponent<RectTransform>().rotation = Quaternion.Euler(0f, 0f, -270f);
				break;
			}
		}
		UpdateUIInputSequence();
	}

	private void UpdateUIInputSequence()
	{
		if (base.isAuthority)
		{
			Color32 color = new Color32(byte.MaxValue, 233, 124, byte.MaxValue);
			for (int i = 0; i < seekerController.meditationInputStep; i++)
			{
				overlayInstanceChildLocator.FindChild(c[i]).GetComponent<Image>().color = color;
			}
		}
	}

	private void UpdateUI()
	{
		foreach (ImageFillController fillUi in fillUiList)
		{
			fillUi.SetTValue(base.fixedAge / duration);
		}
	}

	public override void OnExit()
	{
		PlayAnimation("FullBody, Override", "Empty");
		Util.PlaySound(stopSoundString, base.gameObject);
		if (overlayController != null)
		{
			HudOverlayManager.RemoveOverlay(overlayController);
			overlayController = null;
		}
		aimRequest?.Dispose();
		crosshairOverrideRequest?.Dispose();
		if (characterGravityParameterProvider != null)
		{
			CharacterGravityParameters gravityParameters = characterGravityParameterProvider.gravityParameters;
			gravityParameters.channeledAntiGravityGranterCount--;
			characterGravityParameterProvider.gravityParameters = gravityParameters;
		}
		if ((bool)base.characterMotor)
		{
			base.characterMotor.walkSpeedPenaltyCoefficient = 1f;
		}
		if ((bool)lotus)
		{
			lotus.GetComponent<DestroyOnTimer>().duration = 0.01f;
		}
		if (failed)
		{
			base.skillLocator.special.temporaryCooldownPenalty += failureCooldownPenalty;
		}
		bodyStateMachine.SetNextStateToMain();
		base.OnExit();
	}

	private void OnOverlayInstanceAdded(OverlayController controller, GameObject instance)
	{
		fillUiList.Add(instance.GetComponent<ImageFillController>());
		overlayInstanceChildLocator = instance.GetComponent<ChildLocator>();
		SetupInputUIIcons();
	}

	private void OnOverlayInstanceRemoved(OverlayController controller, GameObject instance)
	{
		fillUiList.Remove(instance.GetComponent<ImageFillController>());
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
