using RoR2;
using UnityEngine;

namespace EntityStates.ShrineHalcyonite;

public class ShrineHalcyoniteLowQuality : ShrineHalcyoniteBaseState
{
	[SerializeField]
	public float tierChangeMonsterCreditReduction;

	public override void OnEnter()
	{
		base.OnEnter();
		GoldSiphonNearbyBodyController.onHalcyonShrineGoldDrain += base.ModifyVisuals;
		TierChange(5f);
		parentShrineReference.activationDirector.monsterCredit += parentShrineReference.monsterCredit - tierChangeMonsterCreditReduction;
		parentShrineReference.activationDirector.SpendAllCreditsOnMapSpawns(parentShrineReference.gameObject.transform);
	}

	public override void OnExit()
	{
		base.OnExit();
		GoldSiphonNearbyBodyController.onHalcyonShrineGoldDrain -= base.ModifyVisuals;
	}
}
