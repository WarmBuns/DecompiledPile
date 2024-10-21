using RoR2;
using RoR2.Hologram;
using UnityEngine;

namespace EntityStates.Interactables.GoldBeacon;

public class GoldBeaconBaseState : BaseState
{
	protected void SetReady(bool ready)
	{
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			PrintController component = modelTransform.GetComponent<PrintController>();
			component.paused = !ready;
			if (!ready)
			{
				component.age = 0f;
			}
			ChildLocator component2 = modelTransform.GetComponent<ChildLocator>();
			if ((bool)component2)
			{
				component2.FindChild("Purchased").gameObject.SetActive(ready);
			}
		}
		PurchaseInteraction component3 = GetComponent<PurchaseInteraction>();
		if ((bool)component3)
		{
			component3.SetAvailable(!ready);
		}
		HologramProjector component4 = GetComponent<HologramProjector>();
		if ((bool)component4)
		{
			component4.hologramPivot.gameObject.SetActive(!ready);
		}
	}
}
