using Rewired;
using UnityEngine;

namespace RoR2.UI;

public class DefaultSettings : MonoBehaviour
{
	public void DefaultControllerMapping(Player player)
	{
		if (player == null)
		{
			return;
		}
		Controller lastActiveController = player.controllers.GetLastActiveController(ControllerType.Joystick);
		if (lastActiveController == null)
		{
			return;
		}
		JoystickMap defaultJoystickMap = DefaultControllerMaps.GetDefaultJoystickMap(lastActiveController.hardwareTypeGuid);
		if (defaultJoystickMap == null)
		{
			return;
		}
		LocalUser localUser = LocalUserManager.FindLocalUser(player);
		if (localUser != null)
		{
			localUser.userProfile.joystickMap = defaultJoystickMap;
			localUser.userProfile.UpdateJoystickMap(lastActiveController.hardwareTypeGuid, defaultJoystickMap);
			localUser.ApplyUserProfileBindingsToRewiredPlayer();
			localUser.userProfile.RequestEventualSave();
			MPEventSystem.RefreshAllControllerGlyphs();
			if (InputBindingDisplayController.onBindingsChanged != null)
			{
				InputBindingDisplayController.onBindingsChanged();
			}
		}
	}

	public void RestoreDefaultControllerSettings(Player player)
	{
		DefaultControllerMapping(player);
	}
}
