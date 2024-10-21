using System;
using UnityEngine;

namespace RoR2;

public class UnlockableGranter : MonoBehaviour
{
	[Obsolete("'unlockableString' will be discontinued. Use 'unlockableDef' instead.", false)]
	[Tooltip("'unlockableString' will be discontinued. Use 'unlockableDef' instead.")]
	public string unlockableString;

	public UnlockableDef unlockableDef;

	public void GrantUnlockable(Interactor interactor)
	{
		string value = unlockableString;
		if (!unlockableDef && !string.IsNullOrEmpty(value))
		{
			unlockableDef = UnlockableCatalog.GetUnlockableDef(value);
		}
		CharacterBody component = interactor.GetComponent<CharacterBody>();
		if ((bool)component)
		{
			Run.instance.GrantUnlockToSinglePlayer(unlockableDef, component);
		}
	}
}
