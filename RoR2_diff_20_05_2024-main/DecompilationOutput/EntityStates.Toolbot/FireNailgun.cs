using RoR2;
using UnityEngine;

namespace EntityStates.Toolbot;

public class FireNailgun : BaseNailgunState
{
	public static float baseRefireInterval;

	public static string spinUpSound;

	private float refireStopwatch;

	private uint loopSoundID;

	private BuffIndex skillsDisabledBuffIndex;

	protected override float GetBaseDuration()
	{
		return baseRefireInterval;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		loopSoundID = Util.PlaySound(spinUpSound, base.gameObject);
		base.animateNailgunFiring = true;
		refireStopwatch = duration;
		skillsDisabledBuffIndex = DLC2Content.Buffs.DisableAllSkills.buffIndex;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		refireStopwatch += GetDeltaTime();
		if (refireStopwatch >= duration)
		{
			PullCurrentStats();
			refireStopwatch -= duration;
			Ray ray = GetAimRay();
			TrajectoryAimAssist.ApplyTrajectoryAimAssist(ref ray, BaseNailgunState.maxDistance, base.gameObject);
			Vector3 direction = ray.direction;
			Vector3 axis = Vector3.Cross(Vector3.up, direction);
			float num = Mathf.Sin((float)fireNumber * 0.5f);
			Vector3 vector = Quaternion.AngleAxis(base.characterBody.spreadBloomAngle * num, axis) * direction;
			vector = Quaternion.AngleAxis((float)fireNumber * -65.454544f, direction) * vector;
			ray.direction = vector;
			FireBullet(ray, 1, 0f, 0f);
		}
		if (base.isAuthority && (!IsKeyDownAuthority() || base.characterBody.isSprinting || base.characterBody.HasBuff(skillsDisabledBuffIndex)))
		{
			outer.SetNextState(new NailgunSpinDown
			{
				activatorSkillSlot = base.activatorSkillSlot
			});
		}
	}

	public override void OnExit()
	{
		base.animateNailgunFiring = false;
		AkSoundEngine.StopPlayingID(loopSoundID);
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
