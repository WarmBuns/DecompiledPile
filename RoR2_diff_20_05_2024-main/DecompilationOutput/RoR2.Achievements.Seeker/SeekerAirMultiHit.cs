using EntityStates.Seeker;
using UnityEngine;

namespace RoR2.Achievements.Seeker;

[RegisterAchievement("SeekerAirMultiHit", "Skills.Seeker.SoulSearch", null, 3u, typeof(SeekerAirMultiHitServerAchievement))]
public class SeekerAirMultiHit : BaseAchievement
{
	private class SeekerAirMultiHitServerAchievement : BaseServerAchievement
	{
		private int hitCount;

		private bool hasShotSpiritOrb;

		public override void OnInstall()
		{
			base.OnInstall();
			SpiritPunch.onSpiritOrbFired += OnSpiritPunch;
			BlastAttack.BlastAttackMultiHit += MultiAirborneBlastAttack;
		}

		public override void OnUninstall()
		{
			BlastAttack.BlastAttackMultiHit -= MultiAirborneBlastAttack;
			SpiritPunch.onSpiritOrbFired -= OnSpiritPunch;
			base.OnUninstall();
		}

		private void MultiAirborneBlastAttack(BlastAttack.HitPoint[] hitPoints, GameObject attacker)
		{
			hitCount = 0;
			if (!(attacker != null))
			{
				return;
			}
			CharacterMaster master = attacker.GetComponent<CharacterBody>().master;
			if ((object)master == null || (object)master != base.networkUser.master || !hasShotSpiritOrb)
			{
				return;
			}
			for (int i = 0; i < hitPoints.Length; i++)
			{
				BlastAttack.HitPoint hitPoint = hitPoints[i];
				HealthComponent healthComponent = (hitPoint.hurtBox ? hitPoint.hurtBox.healthComponent : null);
				if ((bool)healthComponent)
				{
					CharacterBody body = healthComponent.body;
					if (VictimInAir(body))
					{
						hitCount++;
					}
				}
			}
			if (hitCount >= requirement)
			{
				Grant();
			}
			else
			{
				hasShotSpiritOrb = false;
			}
		}

		private bool VictimInAir(CharacterBody victimBody)
		{
			bool flag = false;
			if (victimBody.baseNameToken == "WISP_BODY_NAME" || victimBody.baseNameToken == "GREATERWISP_BODY_NAME")
			{
				flag = true;
			}
			return ((bool)victimBody && (bool)victimBody.characterMotor && !victimBody.characterMotor.isGrounded) || flag;
		}

		private void OnSpiritPunch(bool spiritOrbFired)
		{
			hasShotSpiritOrb = spiritOrbFired;
		}
	}

	private static readonly int requirement = 3;

	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("SeekerBody");
	}

	protected override void OnBodyRequirementMet()
	{
		base.OnBodyRequirementMet();
		SetServerTracked(shouldTrack: true);
	}

	protected override void OnBodyRequirementBroken()
	{
		SetServerTracked(shouldTrack: false);
		base.OnBodyRequirementBroken();
	}
}
