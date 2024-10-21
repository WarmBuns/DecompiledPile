using UnityEngine;

namespace RoR2;

public class MultiOptionPickerPanel : MonoBehaviour
{
	private int numberOfPickUpsSelected;

	public int numberOfPickUpsAvailable = 1;

	private EventFunctions eventFunctions;

	private void Start()
	{
		eventFunctions = base.gameObject.GetComponent<EventFunctions>();
	}

	public void AddPickupSelected()
	{
		numberOfPickUpsSelected++;
		if (numberOfPickUpsSelected >= numberOfPickUpsAvailable)
		{
			KillPanel();
		}
	}

	private void KillPanel()
	{
		eventFunctions.DestroySelf();
	}
}
