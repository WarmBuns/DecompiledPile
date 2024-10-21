using RoR2;

namespace EntityStates.ShrineRebirth;

public class ShrineRebirthEntityStates : EntityState
{
	protected ShrineRebirthController _shrineController;

	protected PickupPickerController _pickupPickerController;

	protected PurchaseInteraction _pi;

	protected NetworkUIPromptController _netUIPromptController;

	public override void OnEnter()
	{
		base.OnEnter();
		_shrineController = base.gameObject.GetComponent<ShrineRebirthController>();
		if (_shrineController.isForTesting)
		{
			outer.SetNextStateToMain();
		}
	}

	public override void OnExit()
	{
		base.OnExit();
	}
}
