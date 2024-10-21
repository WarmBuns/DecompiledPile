using RoR2.Navigation;
using UnityEngine;

namespace RoR2;

[RequireComponent(typeof(CharacterBody))]
public class RailMotor : MonoBehaviour
{
	public Vector3 inputMoveVector;

	public Vector3 rootMotion;

	private Animator modelAnimator;

	private InputBankTest inputBank;

	private NodeGraph railGraph;

	private NodeGraph.NodeIndex nodeA;

	private NodeGraph.NodeIndex nodeB;

	private NodeGraph.LinkIndex currentLink;

	private CharacterBody characterBody;

	private CharacterDirection characterDirection;

	private float linkLerp;

	private Vector3 projectedMoveVector;

	private Vector3 nodeAPosition;

	private Vector3 nodeBPosition;

	private Vector3 linkVector;

	private float linkLength;

	private float currentMoveSpeed;

	private bool useRootMotion;

	private static int isMovingParamHash = Animator.StringToHash("isMoving");

	private void Start()
	{
		characterDirection = GetComponent<CharacterDirection>();
		inputBank = GetComponent<InputBankTest>();
		characterBody = GetComponent<CharacterBody>();
		railGraph = SceneInfo.instance.railNodes;
		ModelLocator component = GetComponent<ModelLocator>();
		if ((bool)component)
		{
			modelAnimator = component.modelTransform.GetComponent<Animator>();
		}
		nodeA = railGraph.FindClosestNode(base.transform.position, characterBody.hullClassification);
		NodeGraph.LinkIndex[] activeNodeLinks = railGraph.GetActiveNodeLinks(nodeA);
		currentLink = activeNodeLinks[0];
		UpdateNodeAndLinkInfo();
		useRootMotion = characterBody.rootMotionInMainState;
	}

	private void UpdateNodeAndLinkInfo()
	{
		nodeA = railGraph.GetLinkStartNode(currentLink);
		nodeB = railGraph.GetLinkEndNode(currentLink);
		railGraph.GetNodePosition(nodeA, out nodeAPosition);
		railGraph.GetNodePosition(nodeB, out nodeBPosition);
		linkVector = nodeBPosition - nodeAPosition;
		linkLength = linkVector.magnitude;
	}

	private void FixedUpdate()
	{
		UpdateNodeAndLinkInfo();
		if ((bool)inputBank)
		{
			bool value = false;
			if (inputMoveVector.sqrMagnitude > 0f)
			{
				value = true;
				characterDirection.moveVector = linkVector;
				if (linkLerp == 0f || linkLerp == 1f)
				{
					NodeGraph.NodeIndex nodeIndex = ((linkLerp != 0f) ? nodeB : nodeA);
					NodeGraph.LinkIndex[] activeNodeLinks = railGraph.GetActiveNodeLinks(nodeIndex);
					float num = -1f;
					NodeGraph.LinkIndex linkIndex = currentLink;
					Debug.DrawRay(base.transform.position, inputMoveVector, Color.green);
					NodeGraph.LinkIndex[] array = activeNodeLinks;
					foreach (NodeGraph.LinkIndex linkIndex2 in array)
					{
						NodeGraph.NodeIndex linkStartNode = railGraph.GetLinkStartNode(linkIndex2);
						NodeGraph.NodeIndex linkEndNode = railGraph.GetLinkEndNode(linkIndex2);
						if (!(linkStartNode != nodeIndex))
						{
							railGraph.GetNodePosition(linkStartNode, out var position);
							railGraph.GetNodePosition(linkEndNode, out var position2);
							Vector3 dir = position2 - position;
							Vector3 rhs = new Vector3(dir.x, 0f, dir.z);
							Debug.DrawRay(position, dir, Color.red);
							float num2 = Vector3.Dot(inputMoveVector, rhs);
							if (num2 > num)
							{
								num = num2;
								linkIndex = linkIndex2;
							}
						}
					}
					if (linkIndex != currentLink)
					{
						currentLink = linkIndex;
						UpdateNodeAndLinkInfo();
						linkLerp = 0f;
					}
				}
			}
			modelAnimator.SetBool(isMovingParamHash, value);
			if (useRootMotion)
			{
				TravelLink();
			}
			else
			{
				TravelLink();
			}
		}
		base.transform.position = Vector3.Lerp(nodeAPosition, nodeBPosition, linkLerp);
	}

	private void TravelLink()
	{
		projectedMoveVector = Vector3.Project(inputMoveVector, linkVector);
		projectedMoveVector = projectedMoveVector.normalized;
		if (characterBody.rootMotionInMainState)
		{
			currentMoveSpeed = rootMotion.magnitude / Time.fixedDeltaTime;
			rootMotion = Vector3.zero;
		}
		else
		{
			currentMoveSpeed = Mathf.MoveTowards(target: (!(projectedMoveVector.sqrMagnitude > 0f)) ? 0f : (characterBody.moveSpeed * inputMoveVector.magnitude), current: currentMoveSpeed, maxDelta: characterBody.acceleration * Time.fixedDeltaTime);
		}
		if (currentMoveSpeed > 0f)
		{
			Vector3 lhs = projectedMoveVector * currentMoveSpeed;
			float num = currentMoveSpeed / linkLength * Mathf.Sign(Vector3.Dot(lhs, linkVector)) * Time.fixedDeltaTime;
			linkLerp = Mathf.Clamp01(linkLerp + num);
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawSphere(nodeAPosition, 0.5f);
		Gizmos.color = Color.yellow;
		Gizmos.DrawSphere(nodeBPosition, 0.5f);
		Gizmos.DrawLine(nodeAPosition, nodeBPosition);
	}
}
