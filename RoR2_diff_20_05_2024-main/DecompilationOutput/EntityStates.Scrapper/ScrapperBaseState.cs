using RoR2;

namespace EntityStates.Scrapper;

public class ScrapperBaseState : BaseState
{
	protected PickupPickerController pickupPickerController;

	protected ScrapperController scrapperController;

	protected virtual bool enableInteraction => true;

	public override void OnEnter()
	{
		base.OnEnter();
		pickupPickerController = GetComponent<PickupPickerController>();
		scrapperController = GetComponent<ScrapperController>();
		pickupPickerController.SetAvailable(enableInteraction);
	}
}
