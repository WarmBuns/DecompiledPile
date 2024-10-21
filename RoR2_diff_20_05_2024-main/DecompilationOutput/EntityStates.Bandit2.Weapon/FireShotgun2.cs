using RoR2;
using UnityEngine;

namespace EntityStates.Bandit2.Weapon;

public class FireShotgun2 : Bandit2FirePrimaryBase
{
	[SerializeField]
	public float minFixedSpreadYaw;

	[SerializeField]
	public float maxFixedSpreadYaw;

	protected override void ModifyBullet(BulletAttack bulletAttack)
	{
		base.ModifyBullet(bulletAttack);
		bulletAttack.bulletCount = 1u;
		bulletAttack.allowTrajectoryAimAssist = false;
	}

	protected override void FireBullet(Ray aimRay)
	{
		StartAimMode(aimRay, 3f);
		DoFireEffects();
		PlayFireAnimation();
		AddRecoil(-1f * recoilAmplitudeY, -1.5f * recoilAmplitudeY, -1f * recoilAmplitudeX, 1f * recoilAmplitudeX);
		if (base.isAuthority)
		{
			Vector3 rhs = Vector3.Cross(Vector3.up, aimRay.direction);
			Vector3 axis = Vector3.Cross(aimRay.direction, rhs);
			float num = 0f;
			if ((bool)base.characterBody)
			{
				num = base.characterBody.spreadBloomAngle;
			}
			float angle = 0f;
			float num2 = 0f;
			if (bulletCount > 1)
			{
				num2 = Random.Range(minFixedSpreadYaw + num, maxFixedSpreadYaw + num) * 2f;
				angle = num2 / (float)(bulletCount - 1);
			}
			TrajectoryAimAssist.ApplyTrajectoryAimAssist(ref aimRay, maxDistance, base.gameObject);
			Vector3 direction = Quaternion.AngleAxis((0f - num2) * 0.5f, axis) * aimRay.direction;
			Quaternion quaternion = Quaternion.AngleAxis(angle, axis);
			Ray aimRay2 = new Ray(aimRay.origin, direction);
			for (int i = 0; i < bulletCount; i++)
			{
				BulletAttack bulletAttack = GenerateBulletAttack(aimRay2);
				ModifyBullet(bulletAttack);
				bulletAttack.Fire();
				aimRay2.direction = quaternion * aimRay2.direction;
			}
		}
	}
}
