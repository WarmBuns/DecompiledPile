using RoR2;
using RoR2.Navigation;
using UnityEngine;

namespace EntityStates.AncientWispMonster;

public class EndRain : BaseState
{
	public static float baseDuration = 3f;

	public static GameObject effectPrefab;

	public static GameObject delayPrefab;

	private float duration;

	private static int endRainHash = Animator.StringToHash("EndRain");

	private static int endRainParamHash = Animator.StringToHash("EndRain.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		if ((bool)base.rigidbodyMotor)
		{
			base.rigidbodyMotor.moveVector = Vector3.zero;
		}
		PlayAnimation("Body", endRainHash, endRainParamHash, duration);
		NodeGraph airNodes = SceneInfo.instance.airNodes;
		NodeGraph.NodeIndex nodeIndex = airNodes.FindClosestNode(base.transform.position, base.characterBody.hullClassification);
		airNodes.GetNodePosition(nodeIndex, out var position);
		base.transform.position = position;
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void Update()
	{
		base.Update();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
