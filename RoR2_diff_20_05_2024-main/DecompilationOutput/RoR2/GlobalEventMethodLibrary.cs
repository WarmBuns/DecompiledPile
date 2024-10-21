using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Playables;

namespace RoR2;

[CreateAssetMenu(menuName = "RoR2/Singletons/GlobalEventMethodLibrary")]
public class GlobalEventMethodLibrary : ScriptableObject
{
	public void RunAdvanceStageServer(SceneDef nextScene)
	{
		if (NetworkServer.active && (bool)Run.instance)
		{
			Run.instance.AdvanceStage(nextScene);
		}
	}

	public void RunBeginGameOverServer(GameEndingDef endingDef)
	{
		if (NetworkServer.active && (bool)Run.instance)
		{
			Run.instance.BeginGameOver(endingDef);
		}
	}

	public void LogMessage(string message)
	{
	}

	public void ForcePlayableDirectorFinish(PlayableDirector playableDirector)
	{
		playableDirector.time = playableDirector.duration;
	}

	public new void DestroyObject(Object obj)
	{
		Object.Destroy(obj);
	}

	public void DisableAllSiblings(Transform transform)
	{
		if (!transform)
		{
			return;
		}
		Transform parent = transform.parent;
		if (!parent)
		{
			return;
		}
		int i = 0;
		for (int childCount = parent.childCount; i < childCount; i++)
		{
			Transform child = parent.GetChild(i);
			if ((bool)child && child != transform)
			{
				child.gameObject.SetActive(value: false);
			}
		}
	}

	public void DisableAllSiblings(GameObject gameObject)
	{
		if ((bool)gameObject)
		{
			DisableAllSiblings(gameObject.transform);
		}
	}

	public void ActivateGameObjectIfServer(GameObject gameObject)
	{
		if ((bool)gameObject && NetworkServer.active)
		{
			gameObject.SetActive(value: true);
		}
	}

	public void DeactivateGameObjectIfServer(GameObject gameObject)
	{
		if ((bool)gameObject && NetworkServer.active)
		{
			gameObject.SetActive(value: false);
		}
	}
}
