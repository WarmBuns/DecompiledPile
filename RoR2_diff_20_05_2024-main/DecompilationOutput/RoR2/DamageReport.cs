using System.Text;
using UnityEngine;

namespace RoR2;

public class DamageReport
{
	public readonly HealthComponent victim;

	public readonly CharacterBody victimBody;

	public readonly BodyIndex victimBodyIndex;

	public readonly TeamIndex victimTeamIndex;

	public readonly CharacterMaster victimMaster;

	public readonly bool victimIsElite;

	public readonly bool victimIsBoss;

	public readonly bool victimIsChampion;

	public readonly DamageInfo damageInfo;

	public readonly GameObject attacker;

	public readonly CharacterBody attackerBody;

	public readonly BodyIndex attackerBodyIndex;

	public readonly TeamIndex attackerTeamIndex;

	public readonly CharacterMaster attackerMaster;

	public readonly CharacterMaster attackerOwnerMaster;

	public readonly BodyIndex attackerOwnerBodyIndex;

	public readonly DotController.DotIndex dotType;

	public readonly float damageDealt;

	public readonly float combinedHealthBeforeDamage;

	public readonly bool hitLowHealth;

	public bool isFriendlyFire
	{
		get
		{
			if (attackerTeamIndex == victimTeamIndex)
			{
				return attackerTeamIndex != TeamIndex.None;
			}
			return false;
		}
	}

	public bool isFallDamage => (ulong)(damageInfo.damageType & DamageType.FallDamage) != 0;

	public DamageReport(DamageInfo damageInfo, HealthComponent victim, float damageDealt, float combinedHealthBeforeDamage)
	{
		this.damageInfo = damageInfo;
		this.victim = victim;
		victimBody = (victim ? victim.body : null);
		if ((bool)victimBody)
		{
			victimBodyIndex = victimBody.bodyIndex;
			victimMaster = victimBody.master;
			victimIsElite = victimBody.isElite;
			victimIsBoss = victimBody.isBoss;
			victimIsChampion = victimBody.isChampion;
			if ((bool)victimBody.teamComponent)
			{
				victimTeamIndex = victimBody.teamComponent.teamIndex;
			}
			else
			{
				victimTeamIndex = TeamIndex.None;
			}
		}
		else
		{
			victimBodyIndex = BodyIndex.None;
			victimTeamIndex = TeamIndex.None;
			victimIsElite = false;
			victimIsBoss = false;
			victimIsChampion = false;
		}
		attacker = damageInfo.attacker;
		attackerBody = (attacker ? attacker.GetComponent<CharacterBody>() : null);
		if ((bool)attackerBody)
		{
			attackerBodyIndex = attackerBody.bodyIndex;
			if ((bool)attackerBody.teamComponent)
			{
				attackerTeamIndex = attackerBody.teamComponent.teamIndex;
			}
			else
			{
				attackerTeamIndex = TeamIndex.None;
			}
			attackerOwnerBodyIndex = BodyIndex.None;
			attackerMaster = attackerBody.master;
			if ((bool)attackerMaster && (bool)attackerMaster.minionOwnership)
			{
				attackerOwnerMaster = attackerMaster.minionOwnership.ownerMaster;
				if ((bool)attackerOwnerMaster)
				{
					CharacterBody body = attackerOwnerMaster.GetBody();
					if ((bool)body)
					{
						attackerOwnerBodyIndex = body.bodyIndex;
					}
				}
			}
		}
		else
		{
			attackerBodyIndex = BodyIndex.None;
			attackerTeamIndex = TeamIndex.None;
			attackerOwnerBodyIndex = BodyIndex.None;
		}
		if ((bool)victim)
		{
			hitLowHealth = victim.isHealthLow;
		}
		else
		{
			hitLowHealth = false;
		}
		if (damageInfo != null)
		{
			dotType = damageInfo.dotIndex;
		}
		this.damageDealt = damageDealt;
		this.combinedHealthBeforeDamage = combinedHealthBeforeDamage;
	}

	public StringBuilder AppendToStringBuilderMultiline(StringBuilder stringBuilder)
	{
		stringBuilder.Append("VictimBody=").AppendLine(ObjToString(victimBody));
		stringBuilder.Append("VictimTeamIndex=").AppendLine(victimTeamIndex.ToString());
		stringBuilder.Append("VictimMaster=").AppendLine(ObjToString(victimMaster));
		stringBuilder.Append("AttackerBody=").AppendLine(ObjToString(attackerBody));
		stringBuilder.Append("AttackerTeamIndex=").AppendLine(attackerTeamIndex.ToString());
		stringBuilder.Append("AttackerMaster=").AppendLine(ObjToString(attackerMaster));
		stringBuilder.Append("Inflictor=").AppendLine(ObjToString(damageInfo.inflictor));
		stringBuilder.Append("Damage=").AppendLine(damageInfo.damage.ToString());
		stringBuilder.Append("Crit=").AppendLine(damageInfo.crit.ToString());
		stringBuilder.Append("ProcChainMask=").AppendLine(damageInfo.procChainMask.ToString());
		stringBuilder.Append("ProcCoefficient=").AppendLine(damageInfo.procCoefficient.ToString());
		stringBuilder.Append("DamageType=").AppendLine(damageInfo.damageType.ToString());
		stringBuilder.Append("DotIndex=").AppendLine(damageInfo.dotIndex.ToString());
		stringBuilder.Append("DamageColorIndex=").AppendLine(damageInfo.damageColorIndex.ToString());
		stringBuilder.Append("Position=").AppendLine(damageInfo.position.ToString());
		stringBuilder.Append("Force=").AppendLine(damageInfo.force.ToString());
		stringBuilder.Append("Rejected=").AppendLine(damageInfo.rejected.ToString());
		return stringBuilder;
		static string ObjToString(object obj)
		{
			if (obj == null)
			{
				return "null";
			}
			return obj.ToString();
		}
	}

	public override string ToString()
	{
		return AppendToStringBuilderMultiline(new StringBuilder()).ToString();
	}
}
