using RoR2;
using UnityEngine;

namespace EntityStates.Commando;

public class MainState : BaseState
{
	private Animator modelAnimator;

	private GenericSkill skill1;

	private GenericSkill skill2;

	private GenericSkill skill3;

	private GenericSkill skill4;

	private bool skill1InputRecieved;

	private bool skill2InputRecieved;

	private bool skill3InputRecieved;

	private bool skill4InputRecieved;

	private Vector3 previousPosition;

	private Vector3 estimatedVelocity;

	private static int isMovingParamHash = Animator.StringToHash("isMoving");

	private static int walkSpeedParamHash = Animator.StringToHash("walkSpeed");

	private static int forwardSpeedParamHash = Animator.StringToHash("forwardSpeed");

	private static int rightSpeedParamHash = Animator.StringToHash("rightSpeed");

	public override void OnEnter()
	{
		base.OnEnter();
		GenericSkill[] components = base.gameObject.GetComponents<GenericSkill>();
		for (int i = 0; i < components.Length; i++)
		{
			if (components[i].skillName == "FirePistol")
			{
				skill1 = components[i];
			}
			else if (components[i].skillName == "FireFMJ")
			{
				skill2 = components[i];
			}
			else if (components[i].skillName == "Roll")
			{
				skill3 = components[i];
			}
			else if (components[i].skillName == "FireBarrage")
			{
				skill4 = components[i];
			}
		}
		modelAnimator = GetModelAnimator();
		previousPosition = base.transform.position;
		if ((bool)modelAnimator)
		{
			int layerIndex = modelAnimator.GetLayerIndex("Body");
			modelAnimator.CrossFadeInFixedTime("Walk", 0.1f, layerIndex);
			modelAnimator.Update(0f);
		}
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			AimAnimator component = modelTransform.GetComponent<AimAnimator>();
			if ((bool)component)
			{
				component.enabled = true;
			}
		}
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
		if (base.isAuthority && (bool)base.characterMotor)
		{
			base.characterMotor.moveDirection = Vector3.zero;
		}
		base.OnExit();
	}

	public override void Update()
	{
		base.Update();
		if (base.inputBank.skill1.down)
		{
			skill1InputRecieved = true;
		}
		if (base.inputBank.skill2.down)
		{
			skill2InputRecieved = true;
		}
		if (base.inputBank.skill3.down)
		{
			skill3InputRecieved = true;
		}
		if (base.inputBank.skill4.down)
		{
			skill4InputRecieved = true;
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		Vector3 position = base.transform.position;
		float deltaTime = GetDeltaTime();
		if (deltaTime != 0f)
		{
			estimatedVelocity = (position - previousPosition) / deltaTime;
		}
		if (base.isAuthority)
		{
			Vector3 moveVector = base.inputBank.moveVector;
			if ((bool)base.characterMotor)
			{
				base.characterMotor.moveDirection = moveVector;
				if (skill3InputRecieved)
				{
					if ((bool)skill3)
					{
						skill3.ExecuteIfReady();
					}
					skill3InputRecieved = false;
				}
			}
			if ((bool)base.characterDirection)
			{
				if ((bool)base.characterBody && base.characterBody.shouldAim)
				{
					base.characterDirection.moveVector = base.inputBank.aimDirection;
				}
				else
				{
					base.characterDirection.moveVector = moveVector;
				}
			}
			if (skill1InputRecieved)
			{
				if ((bool)skill1)
				{
					skill1.ExecuteIfReady();
				}
				skill1InputRecieved = false;
			}
			if (skill2InputRecieved)
			{
				if ((bool)skill2)
				{
					skill2.ExecuteIfReady();
				}
				skill2InputRecieved = false;
			}
			if (skill4InputRecieved)
			{
				if ((bool)skill4)
				{
					skill4.ExecuteIfReady();
				}
				skill4InputRecieved = false;
			}
		}
		if ((bool)modelAnimator && (bool)base.characterDirection)
		{
			Vector3 lhs = estimatedVelocity;
			lhs.y = 0f;
			Vector3 forward = base.characterDirection.forward;
			Vector3 rhs = Vector3.Cross(Vector3.up, forward);
			float magnitude = lhs.magnitude;
			float value = Vector3.Dot(lhs, forward);
			float value2 = Vector3.Dot(lhs, rhs);
			modelAnimator.SetBool(isMovingParamHash, magnitude != 0f);
			modelAnimator.SetFloat(walkSpeedParamHash, magnitude);
			modelAnimator.SetFloat(forwardSpeedParamHash, value, 0.2f, GetDeltaTime());
			modelAnimator.SetFloat(rightSpeedParamHash, value2, 0.2f, GetDeltaTime());
		}
		previousPosition = position;
	}
}
