using RoR2;
using RoR2.Navigation;
using UnityEngine;

namespace EntityStates.AncientWispMonster;

public class ChargeRain : BaseState
{
	public static float baseDuration = 3f;

	public static GameObject effectPrefab;

	public static GameObject delayPrefab;

	private float duration;

	private static int chargeRainStateHash = Animator.StringToHash("ChargeRain");

	private static int chargeRainplaybackRateParamHash = Animator.StringToHash("ChargeRain.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		if ((bool)base.rigidbodyMotor)
		{
			base.rigidbodyMotor.moveVector = Vector3.zero;
		}
		PlayAnimation("Body", chargeRainStateHash, chargeRainplaybackRateParamHash, duration);
		if (Physics.Raycast(base.transform.position, Vector3.down, out var hitInfo, 999f, LayerIndex.world.mask))
		{
			NodeGraph groundNodes = SceneInfo.instance.groundNodes;
			NodeGraph.NodeIndex nodeIndex = groundNodes.FindClosestNode(hitInfo.point, HullClassification.BeetleQueen);
			groundNodes.GetNodePosition(nodeIndex, out var position);
			base.transform.position = position + Vector3.up * 2f;
		}
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
			outer.SetNextState(new ChannelRain());
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
