using UnityEngine;

namespace RoR2.UI;

public class UILayerKeyProxy : MonoBehaviour, IUILayerKeyProvider
{
	public UILayerKey ProxiedLayer;

	public UILayerKey GetUILayerKey()
	{
		return ProxiedLayer;
	}
}
