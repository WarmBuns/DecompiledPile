using UnityEngine;

namespace RoR2.UI;

public class InspectPanelProvider : MonoBehaviour
{
	[SerializeField]
	private InspectPanelController _inspectPanel;

	public InspectPanelController InspectPanel => _inspectPanel;
}
