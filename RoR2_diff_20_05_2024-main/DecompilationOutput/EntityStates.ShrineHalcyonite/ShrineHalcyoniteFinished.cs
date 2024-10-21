using RoR2;

namespace EntityStates.ShrineHalcyonite;

public class ShrineHalcyoniteFinished : ShrineHalcyoniteBaseState
{
	public override void OnEnter()
	{
		base.OnEnter();
		if (shrineHalcyoniteBubble.activeInHierarchy)
		{
			Util.PlaySound("Stop_obj_shrineHalcyonite_idle_loop", base.gameObject);
			shrineHalcyoniteBubble.SetActive(value: false);
		}
	}
}
