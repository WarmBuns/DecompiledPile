using RoR2;
using UnityEngine;

namespace EntityStates.ShrineHalcyonite;

public class ShrineHalcyoniteNoQuality : ShrineHalcyoniteBaseState
{
	[SerializeField]
	public float tierChangeMonsterCreditReduction;

	public override void OnEnter()
	{
		base.OnEnter();
		parentShrineReference.TrackInteractions();
		shrineHalcyoniteBubble.SetActive(value: true);
		parentShrineReference.activationDirector.monsterCredit += parentShrineReference.monsterCredit - tierChangeMonsterCreditReduction;
		parentShrineReference.activationDirector.SpendAllCreditsOnMapSpawns(parentShrineReference.gameObject.transform);
		GoldSiphonNearbyBodyController.onHalcyonShrineGoldDrain += base.ModifyVisuals;
		Util.PlaySound("Play_obj_shrineHalcyonite_idle_loop", base.gameObject);
	}

	public override void OnExit()
	{
		base.OnExit();
		GoldSiphonNearbyBodyController.onHalcyonShrineGoldDrain -= base.ModifyVisuals;
	}
}
