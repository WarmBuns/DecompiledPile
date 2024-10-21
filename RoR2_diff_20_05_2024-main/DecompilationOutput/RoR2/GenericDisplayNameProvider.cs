using UnityEngine;

namespace RoR2;

public class GenericDisplayNameProvider : MonoBehaviour, IDisplayNameProvider
{
	public string displayToken;

	public string GetDisplayName()
	{
		return Language.GetString(displayToken);
	}

	public void SetDisplayToken(string newDisplayToken)
	{
		displayToken = newDisplayToken;
	}
}
