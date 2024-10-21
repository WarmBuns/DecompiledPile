using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.LaserTurbine;

public class AimState : LaserTurbineBaseState
{
	public static float targetAcquisitionRadius;

	private bool foundTarget;

	protected override bool shouldFollow => false;

	public override void OnEnter()
	{
		base.OnEnter();
		if (!base.isAuthority)
		{
			return;
		}
		TeamMask enemyTeams = TeamMask.GetEnemyTeams(base.ownerBody.teamComponent.teamIndex);
		HurtBox[] hurtBoxes = new SphereSearch
		{
			radius = targetAcquisitionRadius,
			mask = LayerIndex.entityPrecise.mask,
			origin = base.transform.position,
			queryTriggerInteraction = QueryTriggerInteraction.UseGlobal
		}.RefreshCandidates().FilterCandidatesByHurtBoxTeam(enemyTeams).OrderCandidatesByDistance()
			.FilterCandidatesByDistinctHurtBoxEntities()
			.GetHurtBoxes();
		float blastRadius = FireMainBeamState.secondBombPrefab.GetComponent<ProjectileImpactExplosion>().blastRadius;
		int num = -1;
		int num2 = 0;
		for (int i = 0; i < hurtBoxes.Length; i++)
		{
			HurtBox[] hurtBoxes2 = new SphereSearch
			{
				radius = blastRadius,
				mask = LayerIndex.entityPrecise.mask,
				origin = hurtBoxes[i].transform.position,
				queryTriggerInteraction = QueryTriggerInteraction.UseGlobal
			}.RefreshCandidates().FilterCandidatesByHurtBoxTeam(enemyTeams).FilterCandidatesByDistinctHurtBoxEntities()
				.GetHurtBoxes();
			if (hurtBoxes2.Length > num2)
			{
				num = i;
				num2 = hurtBoxes2.Length;
			}
		}
		if (num != -1)
		{
			base.simpleRotateToDirection.targetRotation = Quaternion.LookRotation(hurtBoxes[num].transform.position - base.transform.position);
			foundTarget = true;
		}
	}

	public override void Update()
	{
		base.Update();
		if (base.isAuthority)
		{
			if (foundTarget)
			{
				outer.SetNextState(new ChargeMainBeamState());
			}
			else
			{
				outer.SetNextState(new ReadyState());
			}
		}
	}
}
