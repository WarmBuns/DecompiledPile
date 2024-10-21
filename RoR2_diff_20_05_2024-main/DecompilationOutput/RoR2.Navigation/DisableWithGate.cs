using UnityEngine;

namespace RoR2.Navigation;

public class DisableWithGate : MonoBehaviour
{
	public string gateToMatch;

	public bool invert;

	private void Start()
	{
		if ((bool)SceneInfo.instance && SceneInfo.instance.groundNodes.IsGateOpen(gateToMatch) == invert)
		{
			base.gameObject.SetActive(value: false);
		}
	}
}
