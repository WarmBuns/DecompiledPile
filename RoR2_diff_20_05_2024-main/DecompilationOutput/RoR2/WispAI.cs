using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace RoR2;

public class WispAI : MonoBehaviour
{
	private struct TargetSearchCandidate
	{
		public Transform transform;

		public Vector3 positionDiff;

		public float sqrDistance;
	}

	[Tooltip("The character to control.")]
	public GameObject body;

	[Tooltip("The enemy to target.")]
	public Transform targetTransform;

	[Tooltip("The skill to activate for a ranged attack.")]
	public GenericSkill fireSkill;

	[Tooltip("How close the character must be to the enemy to use a ranged attack.")]
	public float fireRange;

	private CharacterDirection bodyDirectionComponent;

	private CharacterMotor bodyMotorComponent;

	private static List<TargetSearchCandidate> candidateList = new List<TargetSearchCandidate>();

	private void Awake()
	{
		bodyDirectionComponent = body.GetComponent<CharacterDirection>();
		bodyMotorComponent = body.GetComponent<CharacterMotor>();
	}

	private void FixedUpdate()
	{
		if (!body)
		{
			return;
		}
		if (!targetTransform)
		{
			targetTransform = SearchForTarget();
		}
		if ((bool)targetTransform)
		{
			Vector3 vector = targetTransform.position - body.transform.position;
			bodyMotorComponent.moveDirection = vector;
			bodyDirectionComponent.moveVector = Vector3.Lerp(bodyDirectionComponent.moveVector, vector, Time.deltaTime);
			if ((bool)fireSkill && vector.sqrMagnitude < fireRange * fireRange)
			{
				fireSkill.ExecuteIfReady();
			}
		}
	}

	private Transform SearchForTarget()
	{
		Vector3 position = body.transform.position;
		Vector3 forward = bodyDirectionComponent.forward;
		ReadOnlyCollection<TeamComponent> teamMembers = TeamComponent.GetTeamMembers(TeamIndex.Player);
		for (int i = 0; i < teamMembers.Count; i++)
		{
			Transform transform = teamMembers[i].transform;
			Vector3 vector = transform.position - position;
			if (Vector3.Dot(forward, vector) > 0f)
			{
				candidateList.Add(new TargetSearchCandidate
				{
					transform = transform,
					positionDiff = vector,
					sqrDistance = vector.sqrMagnitude
				});
			}
		}
		candidateList.Sort((TargetSearchCandidate a, TargetSearchCandidate b) => (!(a.sqrDistance < b.sqrDistance)) ? ((a.sqrDistance != b.sqrDistance) ? 1 : 0) : (-1));
		Transform result = null;
		for (int j = 0; j < candidateList.Count; j++)
		{
			if (!Physics.Raycast(position, candidateList[j].positionDiff, Mathf.Sqrt(candidateList[j].sqrDistance), LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
			{
				result = candidateList[j].transform;
				break;
			}
		}
		candidateList.Clear();
		return result;
	}
}
