using RoR2;
using UnityEngine;

namespace EntityStates.GoldGat;

public class BaseGoldGatState : EntityState
{
	protected NetworkedBodyAttachment networkedBodyAttachment;

	protected GameObject bodyGameObject;

	protected CharacterBody body;

	protected ChildLocator gunChildLocator;

	protected Animator gunAnimator;

	protected Transform gunTransform;

	protected CharacterMaster bodyMaster;

	protected EquipmentSlot bodyEquipmentSlot;

	protected InputBankTest bodyInputBank;

	protected AimAnimator bodyAimAnimator;

	public bool shouldFire;

	private bool linkedToDisplay;

	public override void OnEnter()
	{
		base.OnEnter();
		networkedBodyAttachment = GetComponent<NetworkedBodyAttachment>();
		if (!networkedBodyAttachment)
		{
			return;
		}
		bodyGameObject = networkedBodyAttachment.attachedBodyObject;
		body = networkedBodyAttachment.attachedBody;
		if ((bool)bodyGameObject)
		{
			bodyMaster = body.master;
			bodyInputBank = bodyGameObject.GetComponent<InputBankTest>();
			bodyEquipmentSlot = body.equipmentSlot;
			ModelLocator component = body.GetComponent<ModelLocator>();
			if ((bool)component)
			{
				bodyAimAnimator = component.modelTransform.GetComponent<AimAnimator>();
			}
			LinkToDisplay();
		}
	}

	private void LinkToDisplay()
	{
		if (linkedToDisplay || !bodyEquipmentSlot)
		{
			return;
		}
		gunTransform = bodyEquipmentSlot.FindActiveEquipmentDisplay();
		if ((bool)gunTransform)
		{
			gunChildLocator = gunTransform.GetComponentInChildren<ChildLocator>();
			if ((bool)gunChildLocator && (bool)base.modelLocator)
			{
				base.modelLocator.modelTransform = gunChildLocator.transform;
				gunAnimator = GetModelAnimator();
				linkedToDisplay = true;
			}
		}
	}

	public override void FixedUpdate()
	{
		base.Update();
		if (base.isAuthority && (bool)bodyInputBank)
		{
			if (bodyInputBank.activateEquipment.justPressed)
			{
				shouldFire = !shouldFire;
			}
			if (body.inventory.GetItemCount(RoR2Content.Items.AutoCastEquipment) > 0)
			{
				shouldFire = true;
			}
		}
		LinkToDisplay();
		if ((bool)bodyAimAnimator && (bool)gunAnimator)
		{
			bodyAimAnimator.UpdateAnimatorParameters(gunAnimator, -45f, 45f, 0f, 0f);
		}
	}

	protected bool CheckReturnToIdle()
	{
		if (!base.isAuthority)
		{
			return false;
		}
		if (((bool)bodyMaster && bodyMaster.money == 0) || !shouldFire)
		{
			outer.SetNextState(new GoldGatIdle
			{
				shouldFire = shouldFire
			});
			return true;
		}
		return false;
	}
}
