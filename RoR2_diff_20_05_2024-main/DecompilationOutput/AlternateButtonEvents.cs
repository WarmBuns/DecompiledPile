using RoR2.UI;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(HGButton))]
public class AlternateButtonEvents : MonoBehaviour
{
	public UnityEvent onAlternateClick;

	public UnityEvent onTertiaryClick;

	public void InvokeAltClick()
	{
		if (onAlternateClick != null)
		{
			onAlternateClick.Invoke();
		}
	}

	public void InvokeTertiaryClick()
	{
		if (onTertiaryClick != null)
		{
			onTertiaryClick.Invoke();
		}
	}
}
