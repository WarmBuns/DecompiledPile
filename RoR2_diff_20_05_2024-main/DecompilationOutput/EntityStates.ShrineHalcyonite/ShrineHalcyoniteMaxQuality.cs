namespace EntityStates.ShrineHalcyonite;

public class ShrineHalcyoniteMaxQuality : ShrineHalcyoniteBaseState
{
	public override void OnEnter()
	{
		base.OnEnter();
		parentShrineReference.shrineGoldTop.SetActive(value: true);
		parentShrineReference.DrainConditionMet();
	}
}
