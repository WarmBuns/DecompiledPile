using System.Collections.Generic;
using HG;
using RoR2;
using RoR2.Audio;
using RoR2.Navigation;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.InfiniteTowerSafeWard;

public class Travelling : BaseSafeWardState
{
	[SerializeField]
	public float radius;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string enterSoundString;

	[SerializeField]
	public float minDistanceToNewLocation;

	[SerializeField]
	public float maxDistanceToNewLocation;

	[SerializeField]
	public float travelSpeed;

	[SerializeField]
	public float travelHeight;

	[SerializeField]
	public float pathMaxSlope;

	[SerializeField]
	public float pathMaxJumpHeight;

	[SerializeField]
	public float pathMaxSpeed;

	[SerializeField]
	public int pathNodeInclusionPeriod;

	[SerializeField]
	public string voSoundString;

	[SerializeField]
	public float minimumVoDelay;

	[SerializeField]
	public float maximumVoDelay;

	[SerializeField]
	public LoopSoundDef loopSoundDef;

	private LoopSoundManager.SoundLoopPtr loopPtr;

	private const HullMask wardHullMask = HullMask.Human;

	private const HullClassification pathHullClassification = HullClassification.Human;

	private bool didFail;

	private NodeGraph groundNodeGraph;

	private List<NodeGraph.NodeIndex> potentialEndNodes;

	private List<Vector3> catmullRomPoints = new List<Vector3>();

	private Vector3 rotationVelocity;

	private Path groundPath;

	private int catmullRomIndex;

	private CatmullRom3 currentCurve;

	private float tCurve;

	private float voTimer;

	private Xoroshiro128Plus rng;

	public Travelling()
	{
	}

	public Travelling(Xoroshiro128Plus rng)
	{
		this.rng = rng;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		didFail = false;
		PlayAnimation(animationLayerName, animationStateName);
		Util.PlaySound(enterSoundString, base.gameObject);
		ResetVoTimer();
		if ((bool)loopSoundDef)
		{
			loopPtr = LoopSoundManager.PlaySoundLoopLocal(base.gameObject, loopSoundDef);
		}
		if ((bool)zone)
		{
			zone.Networkradius = radius;
		}
		if (NetworkServer.active)
		{
			groundNodeGraph = SceneInfo.instance.GetNodeGraph(MapNodeGroup.GraphType.Ground);
			potentialEndNodes = groundNodeGraph.FindNodesInRangeWithFlagConditions(base.transform.position, 0f, minDistanceToNewLocation, HullMask.Human, NodeFlags.TeleporterOK, NodeFlags.None, preventOverhead: false);
			Util.ShuffleList(potentialEndNodes, rng);
			List<NodeGraph.NodeIndex> list = groundNodeGraph.FindNodesInRangeWithFlagConditions(base.transform.position, minDistanceToNewLocation, maxDistanceToNewLocation, HullMask.Human, NodeFlags.TeleporterOK, NodeFlags.None, preventOverhead: false);
			Util.ShuffleList(list, rng);
			potentialEndNodes.AddRange(list);
			groundPath = new Path(groundNodeGraph);
		}
	}

	public override void OnExit()
	{
		LoopSoundManager.StopSoundLoopLocal(loopPtr);
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		voTimer -= GetDeltaTime();
		if (voTimer <= 0f)
		{
			ResetVoTimer();
			Util.PlaySound(voSoundString, base.gameObject);
		}
		if (!NetworkServer.active)
		{
			return;
		}
		if (!didFail)
		{
			if (currentCurve == null)
			{
				if (potentialEndNodes.Count > 0)
				{
					EvaluateNextEndpoint();
				}
				else
				{
					didFail = true;
					Debug.LogError("SafeWard failed to find endpoint!");
				}
			}
			else
			{
				tCurve = currentCurve.AdvanceTByDistance(tCurve, travelSpeed * GetDeltaTime());
				tCurve = Mathf.Min(1f, tCurve);
				base.transform.position = currentCurve.Evaluate(tCurve);
				Vector3 vector = currentCurve.EvaluateDerivative(tCurve);
				vector.y = 0f;
				vector = vector.normalized;
				Vector3 forward = Vector3.SmoothDamp(base.transform.forward, vector, ref rotationVelocity, 0.5f);
				base.transform.forward = forward;
				while (tCurve >= 1f && GetRemainingCurveSegmentCount() > 0)
				{
					tCurve -= 1f;
					catmullRomIndex++;
					UpdateCurveSegmentPoints();
				}
			}
		}
		if (didFail || (GetRemainingCurveSegmentCount() <= 0 && tCurve >= 1f))
		{
			outer.SetNextState(new Burrow());
		}
	}

	private void EvaluateNextEndpoint()
	{
		int index = potentialEndNodes.Count - 1;
		NodeGraph.NodeIndex nodeIndex = potentialEndNodes[index];
		potentialEndNodes.RemoveAt(index);
		NodeGraph.PathRequest pathRequest = new NodeGraph.PathRequest
		{
			startPos = base.transform.position,
			endPos = nodeIndex,
			hullClassification = HullClassification.Human,
			maxJumpHeight = pathMaxJumpHeight,
			maxSlope = pathMaxSlope,
			maxSpeed = pathMaxSpeed,
			path = groundPath
		};
		PathTask pathTask = groundNodeGraph.ComputePath(pathRequest);
		if (pathTask == null || pathTask.status != PathTask.TaskStatus.Complete || !pathTask.wasReachable || pathTask.path.waypointsCount <= 1 || !groundNodeGraph.GetNodePosition(nodeIndex, out var _))
		{
			return;
		}
		_ = 1f / (float)(pathTask.path.waypointsCount - 1);
		for (int i = 0; i < pathTask.path.waypointsCount; i++)
		{
			if (i % pathNodeInclusionPeriod != 0 && i != pathTask.path.waypointsCount - 1)
			{
				continue;
			}
			Path.Waypoint waypoint = pathTask.path[i];
			if (groundNodeGraph.GetNodePosition(waypoint.nodeIndex, out var position2))
			{
				if (i > 0 && i < pathTask.path.waypointsCount - 1)
				{
					position2.y += travelHeight;
				}
				catmullRomPoints.Add(position2);
			}
		}
		if (catmullRomPoints.Count > 1)
		{
			Vector3 item = 2f * catmullRomPoints[0] - catmullRomPoints[1];
			catmullRomPoints.Insert(0, item);
			Vector3 item2 = 2f * catmullRomPoints[catmullRomPoints.Count - 1] - catmullRomPoints[catmullRomPoints.Count - 2];
			catmullRomPoints.Add(item2);
			DirectorCore.instance.AddOccupiedNode(groundNodeGraph, nodeIndex);
			catmullRomIndex = 0;
			currentCurve = new CatmullRom3();
			tCurve = 0f;
			UpdateCurveSegmentPoints();
		}
		else
		{
			catmullRomPoints.Clear();
		}
	}

	private void UpdateCurveSegmentPoints()
	{
		currentCurve.SetPoints(catmullRomPoints[catmullRomIndex], catmullRomPoints[catmullRomIndex + 1], catmullRomPoints[catmullRomIndex + 2], catmullRomPoints[catmullRomIndex + 3]);
	}

	private int GetRemainingCurveSegmentCount()
	{
		return catmullRomPoints.Count - 4 - catmullRomIndex;
	}

	private void ResetVoTimer()
	{
		voTimer = Random.Range(minimumVoDelay, maximumVoDelay);
	}
}
