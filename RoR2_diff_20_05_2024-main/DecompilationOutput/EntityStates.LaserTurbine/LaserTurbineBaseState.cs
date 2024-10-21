using RoR2;
using UnityEngine;

namespace EntityStates.LaserTurbine;

public class LaserTurbineBaseState : EntityState
{
	private GenericOwnership genericOwnership;

	private SimpleLeash simpleLeash;

	private MemoizedGetComponent<CharacterBody> bodyGetComponent;

	protected LaserTurbineController laserTurbineController { get; private set; }

	protected SimpleRotateToDirection simpleRotateToDirection { get; private set; }

	protected CharacterBody ownerBody => bodyGetComponent.Get(genericOwnership?.ownerObject);

	protected virtual bool shouldFollow => true;

	public override void OnEnter()
	{
		base.OnEnter();
		genericOwnership = GetComponent<GenericOwnership>();
		simpleLeash = GetComponent<SimpleLeash>();
		simpleRotateToDirection = GetComponent<SimpleRotateToDirection>();
		laserTurbineController = GetComponent<LaserTurbineController>();
	}

	protected InputBankTest GetInputBank()
	{
		return ownerBody?.inputBank;
	}

	protected Ray GetAimRay()
	{
		return new Ray(base.transform.position, base.transform.forward);
	}

	protected Transform GetMuzzleTransform()
	{
		return base.transform;
	}

	public override void Update()
	{
		base.Update();
		if ((bool)ownerBody && shouldFollow)
		{
			simpleLeash.leashOrigin = ownerBody.corePosition;
			simpleRotateToDirection.targetRotation = Quaternion.LookRotation(ownerBody.inputBank.aimDirection);
		}
	}

	protected float GetDamage()
	{
		float num = 1f;
		if ((bool)ownerBody)
		{
			num = ownerBody.damage;
			if ((bool)ownerBody.inventory)
			{
				num *= (float)ownerBody.inventory.GetItemCount(RoR2Content.Items.LaserTurbine);
			}
		}
		return num;
	}
}
