using RoR2;
using UnityEngine;

namespace EntityStates.ShrineHalcyonite;

public class ShrineHalcyoniteMidQuality : ShrineHalcyoniteBaseState
{
	[SerializeField]
	public float tierChangeMonsterCreditReduction;

	public override void OnEnter()
	{
		base.OnEnter();
		TierChange(8f);
		GoldSiphonNearbyBodyController.onHalcyonShrineGoldDrain += base.ModifyVisuals;
		parentShrineReference.activationDirector.monsterCredit += parentShrineReference.monsterCredit - tierChangeMonsterCreditReduction;
		parentShrineReference.activationDirector.SpendAllCreditsOnMapSpawns(parentShrineReference.gameObject.transform);
	}

	public override void OnExit()
	{
		base.OnExit();
		GoldSiphonNearbyBodyController.onHalcyonShrineGoldDrain -= base.ModifyVisuals;
	}
}
