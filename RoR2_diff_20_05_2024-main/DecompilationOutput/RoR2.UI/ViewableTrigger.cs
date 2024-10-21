using UnityEngine;

namespace RoR2.UI;

public class ViewableTrigger : MonoBehaviour
{
	[Tooltip("The name of the viewable to mark as viewed when this component becomes enabled.")]
	public string viewableName;

	private void OnEnable()
	{
		TriggerView(viewableName);
	}

	public static void TriggerView(string viewableName)
	{
		if (string.IsNullOrEmpty(viewableName))
		{
			return;
		}
		foreach (LocalUser readOnlyLocalUsers in LocalUserManager.readOnlyLocalUsersList)
		{
			readOnlyLocalUsers.userProfile.MarkViewableAsViewed(viewableName);
		}
	}
}
