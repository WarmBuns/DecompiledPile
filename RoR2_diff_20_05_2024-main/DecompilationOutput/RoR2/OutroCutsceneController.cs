using UnityEngine;
using UnityEngine.Playables;

namespace RoR2;

public class OutroCutsceneController : MonoBehaviour
{
	public PlayableDirector playableDirector;

	private int finishCount;

	public static OutroCutsceneController instance { get; private set; }

	public bool cutsceneIsFinished { get; private set; }

	public void Finish()
	{
		finishCount++;
		if (finishCount >= 1)
		{
			cutsceneIsFinished = true;
		}
	}

	private void OnEnable()
	{
		instance = SingletonHelper.Assign(instance, this);
	}

	private void OnDisable()
	{
		instance = SingletonHelper.Unassign(instance, this);
	}
}
