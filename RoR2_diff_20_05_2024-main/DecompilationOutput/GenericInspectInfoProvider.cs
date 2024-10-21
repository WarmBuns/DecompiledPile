using RoR2;
using RoR2.UI;
using UnityEngine;

public class GenericInspectInfoProvider : MonoBehaviour, IInspectInfoProvider, IHasInspectHintOverride
{
	public InspectDef InspectInfo;

	[Tooltip("Set this to a non-empty token to override the default Inspect hint on the HUD.")]
	public string inspectHintOverrideToken;

	public bool CanBeInspected()
	{
		return true;
	}

	public InspectInfo GetInfo()
	{
		return InspectInfo;
	}

	public bool GetInspectHintOverride(out string hintOverride)
	{
		if (inspectHintOverrideToken.Length > 0)
		{
			hintOverride = Language.GetString(inspectHintOverrideToken);
			return true;
		}
		hintOverride = null;
		return false;
	}
}
