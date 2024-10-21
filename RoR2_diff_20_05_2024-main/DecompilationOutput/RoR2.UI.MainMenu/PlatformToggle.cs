using UnityEngine;

namespace RoR2.UI.MainMenu;

public class PlatformToggle : MonoBehaviour
{
	public bool Steam;

	public bool XboxOne;

	public bool XboxSeries;

	public bool PS4;

	public bool PS5;

	public bool Switch;

	private void Awake()
	{
		base.gameObject.SetActive(Steam);
	}
}
