using RoR2;
using UnityEngine;

namespace EntityStates;

public class GenericCharacterMain : BaseCharacterMain
{
	public delegate void MovementHitDelegate(ref CharacterMotor.MovementHitInfo movementHitInfo);

	private AimAnimator aimAnimator;

	protected bool jumpInputReceived;

	protected bool sprintInputReceived;

	private Vector3 moveVector = Vector3.zero;

	private Vector3 aimDirection = Vector3.forward;

	private int emoteRequest = -1;

	private bool hasAimAnimator;

	public override void OnEnter()
	{
		base.OnEnter();
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			aimAnimator = modelTransform.GetComponent<AimAnimator>();
			if ((bool)aimAnimator)
			{
				aimAnimator.enabled = true;
			}
		}
		hasAimAnimator = aimAnimator;
	}

	public override void OnExit()
	{
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			AimAnimator component = modelTransform.GetComponent<AimAnimator>();
			if ((bool)component)
			{
				component.enabled = false;
			}
		}
		if (base.isAuthority)
		{
			if ((bool)base.characterMotor)
			{
				base.characterMotor.moveDirection = Vector3.zero;
			}
			if ((bool)base.railMotor)
			{
				base.railMotor.inputMoveVector = Vector3.zero;
			}
		}
		base.OnExit();
	}

	public override void Update()
	{
		base.Update();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		GatherInputs();
		HandleMovements();
		PerformInputs();
	}

	public virtual void HandleMovements()
	{
		if (useRootMotion)
		{
			if (hasCharacterMotor)
			{
				base.characterMotor.moveDirection = Vector3.zero;
			}
			if (hasRailMotor)
			{
				base.railMotor.inputMoveVector = moveVector;
			}
		}
		else
		{
			if (hasCharacterMotor)
			{
				base.characterMotor.moveDirection = moveVector;
			}
			if (hasRailMotor)
			{
				base.railMotor.inputMoveVector = moveVector;
			}
		}
		_ = base.isGrounded;
		if (!hasRailMotor && hasCharacterDirection && hasCharacterBody)
		{
			if (hasAimAnimator && aimAnimator.aimType == AimAnimator.AimType.Smart)
			{
				Vector3 vector = ((moveVector == Vector3.zero) ? base.characterDirection.forward : moveVector);
				float num = Vector3.Angle(aimDirection, vector);
				float num2 = Mathf.Max(aimAnimator.pitchRangeMax + aimAnimator.pitchGiveupRange, aimAnimator.yawRangeMax + aimAnimator.yawGiveupRange);
				base.characterDirection.moveVector = (((bool)base.characterBody && base.characterBody.shouldAim && num > num2) ? aimDirection : vector);
			}
			else
			{
				base.characterDirection.moveVector = (((bool)base.characterBody && base.characterBody.shouldAim) ? aimDirection : moveVector);
			}
		}
		if (!base.isAuthority)
		{
			return;
		}
		ProcessJump();
		if (hasCharacterBody)
		{
			bool isSprinting = sprintInputReceived;
			if (moveVector.magnitude <= 0.5f)
			{
				isSprinting = false;
			}
			base.characterBody.isSprinting = isSprinting;
		}
	}

	public static void ApplyJumpVelocity(CharacterMotor characterMotor, CharacterBody characterBody, float horizontalBonus, float verticalBonus, bool vault = false)
	{
		Vector3 moveDirection = characterMotor.moveDirection;
		if (vault)
		{
			characterMotor.velocity = moveDirection;
		}
		else
		{
			moveDirection.y = 0f;
			float magnitude = moveDirection.magnitude;
			if (magnitude > 0f)
			{
				moveDirection /= magnitude;
			}
			Vector3 velocity = moveDirection * characterBody.moveSpeed * horizontalBonus;
			velocity.y = characterBody.jumpPower * verticalBonus;
			characterMotor.velocity = velocity;
		}
		characterMotor.Motor.ForceUnground();
	}

	public virtual void ProcessJump()
	{
		if (!hasCharacterMotor)
		{
			return;
		}
		bool flag = false;
		bool flag2 = false;
		if (!jumpInputReceived || !base.characterBody || base.characterMotor.jumpCount >= base.characterBody.maxJumpCount)
		{
			return;
		}
		int itemCount = base.characterBody.inventory.GetItemCount(RoR2Content.Items.JumpBoost);
		float horizontalBonus = 1f;
		float verticalBonus = 1f;
		if (base.characterMotor.jumpCount >= base.characterBody.baseJumpCount)
		{
			flag = true;
			horizontalBonus = 1.5f;
			verticalBonus = 1.5f;
		}
		else if ((float)itemCount > 0f && base.characterBody.isSprinting)
		{
			float num = base.characterBody.acceleration * base.characterMotor.airControl;
			if (base.characterBody.moveSpeed > 0f && num > 0f)
			{
				flag2 = true;
				float num2 = Mathf.Sqrt(10f * (float)itemCount / num);
				float num3 = base.characterBody.moveSpeed / num;
				horizontalBonus = (num2 + num3) / num3;
			}
		}
		ApplyJumpVelocity(base.characterMotor, base.characterBody, horizontalBonus, verticalBonus);
		if (hasModelAnimator)
		{
			int layerIndex = base.modelAnimator.GetLayerIndex("Body");
			if (layerIndex >= 0)
			{
				if (base.characterMotor.jumpCount == 0 || base.characterBody.baseJumpCount == 1)
				{
					base.modelAnimator.CrossFadeInFixedTime("Jump", smoothingParameters.intoJumpTransitionTime, layerIndex);
				}
				else
				{
					base.modelAnimator.CrossFadeInFixedTime("BonusJump", smoothingParameters.intoJumpTransitionTime, layerIndex);
				}
			}
		}
		if (flag)
		{
			EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/FeatherEffect"), new EffectData
			{
				origin = base.characterBody.footPosition
			}, transmit: true);
		}
		else if (base.characterMotor.jumpCount > 0)
		{
			EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/CharacterLandImpact"), new EffectData
			{
				origin = base.characterBody.footPosition,
				scale = base.characterBody.radius
			}, transmit: true);
		}
		if (flag2)
		{
			EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/BoostJumpEffect"), new EffectData
			{
				origin = base.characterBody.footPosition,
				rotation = Util.QuaternionSafeLookRotation(base.characterMotor.velocity)
			}, transmit: true);
		}
		base.characterMotor.jumpCount++;
		base.characterBody.onJump?.Invoke();
	}

	protected virtual bool CanExecuteSkill(GenericSkill skillSlot)
	{
		return true;
	}

	protected void PerformInputs()
	{
		if (base.isAuthority)
		{
			if (hasSkillLocator)
			{
				HandleSkill(base.skillLocator.primary, ref base.inputBank.skill1);
				HandleSkill(base.skillLocator.secondary, ref base.inputBank.skill2);
				HandleSkill(base.skillLocator.utility, ref base.inputBank.skill3);
				HandleSkill(base.skillLocator.special, ref base.inputBank.skill4);
			}
			jumpInputReceived = false;
			sprintInputReceived = false;
		}
		void HandleSkill(GenericSkill skillSlot, ref InputBankTest.ButtonState buttonState)
		{
			if (buttonState.down && (bool)skillSlot && (!skillSlot.mustKeyPress || !buttonState.hasPressBeenClaimed) && CanExecuteSkill(skillSlot) && skillSlot.ExecuteIfReady())
			{
				buttonState.hasPressBeenClaimed = true;
			}
		}
	}

	protected void GatherInputs()
	{
		if (hasInputBank)
		{
			moveVector = base.inputBank.moveVector;
			aimDirection = base.inputBank.aimDirection;
			emoteRequest = base.inputBank.emoteRequest;
			base.inputBank.emoteRequest = -1;
			jumpInputReceived |= base.inputBank.jump.justPressed;
			jumpInputReceived &= !base.inputBank.jump.hasPressBeenClaimed;
			sprintInputReceived |= base.inputBank.sprint.down;
		}
	}
}
