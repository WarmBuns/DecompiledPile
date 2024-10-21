using UnityEngine;

namespace EntityStates.Geode;

public class GeodeInert : GeodeEntityStates
{
	[SerializeField]
	public float reactivationDuration;

	public override void OnEnter()
	{
		base.OnEnter();
		cleansingIndicator.SetActive(value: false);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= reactivationDuration && geodeController.ShouldRegenerate)
		{
			outer.SetNextState(new GeodeRegenerate());
		}
		else if (!geodeController.ShouldRegenerate)
		{
			EntityState.Destroy(base.gameObject);
		}
	}
}
