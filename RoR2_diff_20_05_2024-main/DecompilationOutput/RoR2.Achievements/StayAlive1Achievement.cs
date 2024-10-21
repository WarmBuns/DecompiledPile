using System.Linq;
using UnityEngine;

namespace RoR2.Achievements;

[RegisterAchievement("StayAlive1", "Items.ExtraLife", null, 3u, null)]
public class StayAlive1Achievement : BaseAchievement
{
	private const float requirement = 1800f;

	public override void OnInstall()
	{
		base.OnInstall();
		RoR2Application.onUpdate += Check;
	}

	public override void OnUninstall()
	{
		RoR2Application.onUpdate -= Check;
		base.OnUninstall();
	}

	private void Check()
	{
		NetworkUser networkUser = NetworkUser.readOnlyLocalPlayersList.FirstOrDefault((NetworkUser v) => v.localUser == base.localUser);
		if (!networkUser)
		{
			return;
		}
		GameObject masterObject = networkUser.masterObject;
		if ((bool)masterObject)
		{
			CharacterMaster component = masterObject.GetComponent<CharacterMaster>();
			if ((bool)component && component.currentLifeStopwatch >= 1800f)
			{
				Grant();
			}
		}
	}
}
