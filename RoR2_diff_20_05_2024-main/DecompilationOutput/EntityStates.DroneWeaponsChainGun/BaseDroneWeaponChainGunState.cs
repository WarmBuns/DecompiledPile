using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace EntityStates.DroneWeaponsChainGun;

public abstract class BaseDroneWeaponChainGunState : EntityState
{
	private const string aimAnimatorChildName = "AimAnimator";

	[SerializeField]
	public float pitchRangeMin;

	[SerializeField]
	public float pitchRangeMax;

	[SerializeField]
	public float yawRangeMin;

	[SerializeField]
	public float yawRangeMax;

	protected NetworkedBodyAttachment networkedBodyAttachment;

	protected GameObject bodyGameObject;

	protected CharacterBody body;

	protected List<ChildLocator> gunChildLocators;

	protected List<Animator> gunAnimators;

	protected AimAnimator bodyAimAnimator;

	protected TeamComponent bodyTeamComponent;

	private bool linkedToDisplay;

	public override void OnEnter()
	{
		base.OnEnter();
		networkedBodyAttachment = GetComponent<NetworkedBodyAttachment>();
		if ((bool)networkedBodyAttachment)
		{
			bodyGameObject = networkedBodyAttachment.attachedBodyObject;
			body = networkedBodyAttachment.attachedBody;
			if ((bool)bodyGameObject && (bool)body)
			{
				ModelLocator component = body.GetComponent<ModelLocator>();
				if ((bool)component)
				{
					bodyAimAnimator = component.modelTransform.GetComponent<AimAnimator>();
					bodyTeamComponent = body.GetComponent<TeamComponent>();
				}
			}
		}
		LinkToDisplay();
	}

	private void LinkToDisplay()
	{
		if (linkedToDisplay)
		{
			return;
		}
		gunAnimators = new List<Animator>();
		gunChildLocators = new List<ChildLocator>();
		if (!networkedBodyAttachment)
		{
			return;
		}
		bodyGameObject = networkedBodyAttachment.attachedBodyObject;
		body = networkedBodyAttachment.attachedBody;
		if (!bodyGameObject || !body)
		{
			return;
		}
		ModelLocator component = body.GetComponent<ModelLocator>();
		if (!component || !component.modelTransform)
		{
			return;
		}
		bodyAimAnimator = component.modelTransform.GetComponent<AimAnimator>();
		bodyTeamComponent = body.GetComponent<TeamComponent>();
		CharacterModel component2 = component.modelTransform.GetComponent<CharacterModel>();
		if (!component2)
		{
			return;
		}
		List<GameObject> itemDisplayObjects = component2.GetItemDisplayObjects(DLC1Content.Items.DroneWeaponsDisplay1.itemIndex);
		itemDisplayObjects.AddRange(component2.GetItemDisplayObjects(DLC1Content.Items.DroneWeaponsDisplay2.itemIndex));
		foreach (GameObject item in itemDisplayObjects)
		{
			ChildLocator component3 = item.GetComponent<ChildLocator>();
			if ((bool)component3)
			{
				gunChildLocators.Add(component3);
				Animator animator = component3.FindChildComponent<Animator>("AimAnimator");
				if ((bool)animator)
				{
					gunAnimators.Add(animator);
				}
			}
		}
	}

	public void PassDisplayLinks(List<ChildLocator> gunChildLocators, List<Animator> gunAnimators)
	{
		if (!linkedToDisplay)
		{
			linkedToDisplay = true;
			this.gunAnimators = gunAnimators;
			this.gunChildLocators = gunChildLocators;
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		LinkToDisplay();
		if (!bodyAimAnimator)
		{
			return;
		}
		foreach (Animator gunAnimator in gunAnimators)
		{
			bodyAimAnimator.UpdateAnimatorParameters(gunAnimator, pitchRangeMin, pitchRangeMax, yawRangeMin, yawRangeMax);
		}
	}

	protected Transform FindChild(string childName)
	{
		foreach (ChildLocator gunChildLocator in gunChildLocators)
		{
			Transform transform = gunChildLocator.FindChild(childName);
			if ((bool)transform)
			{
				return transform;
			}
		}
		return null;
	}

	protected Ray GetAimRay()
	{
		if ((bool)body.inputBank)
		{
			return new Ray(body.inputBank.aimOrigin, body.inputBank.aimDirection);
		}
		return new Ray(base.transform.position, base.transform.forward);
	}
}
