using System;

namespace RoR2.Achievements;

[RegisterAchievement("LoopOnce", "Items.BounceNearby", null, 3u, null)]
public class LoopOnceAchievement : BaseAchievement
{
	public override void OnInstall()
	{
		base.OnInstall();
		UserProfile obj = base.userProfile;
		obj.onStatsReceived = (Action)Delegate.Combine(obj.onStatsReceived, new Action(Check));
		Check();
	}

	public override void OnUninstall()
	{
		UserProfile obj = base.userProfile;
		obj.onStatsReceived = (Action)Delegate.Remove(obj.onStatsReceived, new Action(Check));
		base.OnUninstall();
	}

	private void Check()
	{
		if ((bool)Run.instance && Run.instance.GetType() == typeof(Run) && Run.instance.loopClearCount > 0)
		{
			SceneDef sceneDefForCurrentScene = SceneCatalog.GetSceneDefForCurrentScene();
			if ((bool)sceneDefForCurrentScene && (sceneDefForCurrentScene.sceneType == SceneType.Stage || sceneDefForCurrentScene.sceneType == SceneType.UntimedStage) && !sceneDefForCurrentScene.isFinalStage && sceneDefForCurrentScene.stageOrder == 1)
			{
				Grant();
			}
		}
	}
}
