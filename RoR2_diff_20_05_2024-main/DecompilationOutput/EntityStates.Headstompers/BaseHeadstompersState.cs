using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace EntityStates.Headstompers;

public class BaseHeadstompersState : EntityState
{
	private static readonly List<BaseHeadstompersState> instancesList = new List<BaseHeadstompersState>();

	protected NetworkedBodyAttachment networkedBodyAttachment;

	protected GameObject bodyGameObject;

	protected CharacterBody body;

	protected CharacterMotor bodyMotor;

	protected InputBankTest bodyInputBank;

	protected bool jumpButtonDown
	{
		get
		{
			if ((bool)bodyInputBank)
			{
				return bodyInputBank.jump.down;
			}
			return false;
		}
	}

	protected bool slamButtonDown
	{
		get
		{
			if ((bool)bodyInputBank)
			{
				return bodyInputBank.interact.down;
			}
			return false;
		}
	}

	protected bool isGrounded
	{
		get
		{
			if ((bool)bodyMotor)
			{
				return bodyMotor.isGrounded;
			}
			return false;
		}
	}

	public static BaseHeadstompersState FindForBody(CharacterBody body)
	{
		for (int i = 0; i < instancesList.Count; i++)
		{
			if ((object)instancesList[i].body == body)
			{
				return instancesList[i];
			}
		}
		return null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		instancesList.Add(this);
		networkedBodyAttachment = GetComponent<NetworkedBodyAttachment>();
		if ((bool)networkedBodyAttachment)
		{
			bodyGameObject = networkedBodyAttachment.attachedBodyObject;
			body = networkedBodyAttachment.attachedBody;
			if ((bool)bodyGameObject)
			{
				bodyMotor = bodyGameObject.GetComponent<CharacterMotor>();
				bodyInputBank = bodyGameObject.GetComponent<InputBankTest>();
			}
		}
	}

	public override void OnExit()
	{
		instancesList.Remove(this);
		base.OnExit();
	}

	protected bool ReturnToIdleIfGroundedAuthority()
	{
		if ((bool)bodyMotor && bodyMotor.isGrounded)
		{
			outer.SetNextState(new HeadstompersIdle());
			return true;
		}
		return false;
	}
}
