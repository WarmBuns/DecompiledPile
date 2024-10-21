using EntityStates.Mage.Weapon;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.GlobalSkills.LunarNeedle;

public class ThrowLunarSecondary : BaseThrowBombState
{
	[SerializeField]
	public float minSpeed;

	[SerializeField]
	public float maxSpeed;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string playbackRateParam;

	protected override void PlayThrowAnimation()
	{
		PlayAnimation(animationLayerName, animationStateName, playbackRateParam, duration);
	}

	protected override void ModifyProjectile(ref FireProjectileInfo projectileInfo)
	{
		projectileInfo.speedOverride = Util.Remap(charge, 0f, 1f, minSpeed, maxSpeed);
		projectileInfo.useSpeedOverride = true;
	}
}
