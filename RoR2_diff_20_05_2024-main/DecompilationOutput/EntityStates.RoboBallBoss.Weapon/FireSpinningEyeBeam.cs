using EntityStates.RoboBallMini.Weapon;
using UnityEngine;

namespace EntityStates.RoboBallBoss.Weapon;

public class FireSpinningEyeBeam : FireEyeBeam
{
	private Transform eyeBeamOriginTransform;

	public override void OnEnter()
	{
		string customName = outer.customName;
		eyeBeamOriginTransform = FindModelChild(customName);
		muzzleString = customName;
		base.OnEnter();
	}

	public override Ray GetLaserRay()
	{
		Ray result = default(Ray);
		if ((bool)eyeBeamOriginTransform)
		{
			result.origin = eyeBeamOriginTransform.position;
			result.direction = eyeBeamOriginTransform.forward;
		}
		return result;
	}

	public override bool ShouldFireLaser()
	{
		return true;
	}
}
