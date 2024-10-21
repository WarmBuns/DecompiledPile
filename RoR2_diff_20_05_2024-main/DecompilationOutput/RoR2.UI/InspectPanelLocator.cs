using UnityEngine;

namespace RoR2.UI;

public class InspectPanelLocator : MonoBehaviour
{
	public InspectPanelController InspectPanel { get; private set; }

	private void Awake()
	{
		InspectPanel = GetComponentInParent<InspectPanelProvider>().InspectPanel;
	}
}
