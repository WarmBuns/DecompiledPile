using UnityEngine;

namespace RoR2;

public class StageCountObjectFilter : MonoBehaviour
{
	[Tooltip("The minimum stage required to start enabling objects.")]
	public int minimumStageClearCount;

	[Tooltip("The objects to activate or deactivate, depending on the stage count.")]
	public GameObject[] gameObjects;

	private void Start()
	{
		if (!Run.instance)
		{
			return;
		}
		bool active = Run.instance.stageClearCount >= minimumStageClearCount;
		for (int i = 0; i < gameObjects.Length; i++)
		{
			GameObject gameObject = gameObjects[i];
			if ((bool)gameObject)
			{
				gameObject.SetActive(active);
			}
		}
	}
}
