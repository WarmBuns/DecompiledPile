using RoR2;
using RoR2.Navigation;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Vulture;

public class FlyToLand : BaseSkillState
{
	private float duration;

	private Vector3 targetPosition;

	public static float speedMultiplier;

	private ICharacterGravityParameterProvider characterGravityParameterProvider;

	private ICharacterFlightParameterProvider characterFlightParameterProvider;

	public override void OnEnter()
	{
		base.OnEnter();
		characterGravityParameterProvider = base.gameObject.GetComponent<ICharacterGravityParameterProvider>();
		characterFlightParameterProvider = base.gameObject.GetComponent<ICharacterFlightParameterProvider>();
		Vector3 footPosition = GetFootPosition();
		if (base.isAuthority)
		{
			bool flag = false;
			NodeGraph groundNodes = SceneInfo.instance.groundNodes;
			if ((bool)groundNodes)
			{
				NodeGraph.NodeIndex nodeIndex = groundNodes.FindClosestNodeWithFlagConditions(base.transform.position, base.characterBody.hullClassification, NodeFlags.None, NodeFlags.None, preventOverhead: false);
				flag = nodeIndex != NodeGraph.NodeIndex.invalid && groundNodes.GetNodePosition(nodeIndex, out targetPosition);
			}
			if (!flag)
			{
				outer.SetNextState(new Fly
				{
					activatorSkillSlot = base.activatorSkillSlot
				});
				duration = 0f;
				targetPosition = footPosition;
				return;
			}
		}
		Vector3 vector = targetPosition - footPosition;
		float num = moveSpeedStat * speedMultiplier;
		duration = vector.magnitude / num;
		if (characterGravityParameterProvider != null)
		{
			CharacterGravityParameters gravityParameters = characterGravityParameterProvider.gravityParameters;
			gravityParameters.channeledAntiGravityGranterCount++;
			characterGravityParameterProvider.gravityParameters = gravityParameters;
		}
		if (characterFlightParameterProvider != null)
		{
			CharacterFlightParameters flightParameters = characterFlightParameterProvider.flightParameters;
			flightParameters.channeledFlightGranterCount++;
			characterFlightParameterProvider.flightParameters = flightParameters;
		}
	}

	private Vector3 GetFootPosition()
	{
		if ((bool)base.characterBody)
		{
			return base.characterBody.footPosition;
		}
		return base.transform.position;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		Vector3 footPosition = GetFootPosition();
		base.characterMotor.moveDirection = (targetPosition - footPosition).normalized * speedMultiplier;
		if (base.isAuthority && (base.characterMotor.isGrounded || duration <= base.fixedAge))
		{
			outer.SetNextStateToMain();
		}
	}

	public override void OnExit()
	{
		if (characterFlightParameterProvider != null)
		{
			CharacterFlightParameters flightParameters = characterFlightParameterProvider.flightParameters;
			flightParameters.channeledFlightGranterCount--;
			characterFlightParameterProvider.flightParameters = flightParameters;
		}
		if (characterGravityParameterProvider != null)
		{
			CharacterGravityParameters gravityParameters = characterGravityParameterProvider.gravityParameters;
			gravityParameters.channeledAntiGravityGranterCount--;
			characterGravityParameterProvider.gravityParameters = gravityParameters;
		}
		Animator modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator)
		{
			modelAnimator.SetFloat("Flying", 0f);
		}
		base.OnExit();
	}

	public override void OnSerialize(NetworkWriter writer)
	{
		base.OnSerialize(writer);
		writer.Write(targetPosition);
	}

	public override void OnDeserialize(NetworkReader reader)
	{
		base.OnDeserialize(reader);
		targetPosition = reader.ReadVector3();
	}
}
