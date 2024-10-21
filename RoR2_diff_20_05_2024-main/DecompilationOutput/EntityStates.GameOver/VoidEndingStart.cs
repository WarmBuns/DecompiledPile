using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.GameOver;

public class VoidEndingStart : BaseGameOverControllerState
{
	[SerializeField]
	public float duration;

	private bool hasSceneExited;

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active && base.fixedAge >= duration && hasSceneExited)
		{
			outer.SetNextState(new VoidEndingFadeToBlack());
		}
	}

	public override void OnEnter()
	{
		base.OnEnter();
		SceneExitController.onFinishExit += OnFinishSceneExit;
	}

	public override void OnExit()
	{
		SceneExitController.onFinishExit -= OnFinishSceneExit;
		base.OnExit();
	}

	private void OnFinishSceneExit(SceneExitController obj)
	{
		hasSceneExited = true;
	}
}
