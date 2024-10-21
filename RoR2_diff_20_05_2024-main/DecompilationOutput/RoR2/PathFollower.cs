using System;
using System.Collections.Generic;
using HG;
using RoR2.Navigation;
using UnityEngine;

namespace RoR2;

public class PathFollower
{
	public class Drawer : IDisposable
	{
		private PathFollower _target;

		private WireMeshBuilder wireMeshBuilder;

		private DebugOverlay.MeshDrawer passDistanceDrawer;

		private DebugOverlay.MeshDrawer pathDrawer;

		private List<Path.Waypoint> waypointCache;

		private int cachedCurrentWaypoint;

		public PathFollower target
		{
			get
			{
				return _target;
			}
			set
			{
				if (_target != value)
				{
					_target = value;
					pathDrawer.enabled = _target != null;
					passDistanceDrawer.enabled &= _target != null;
					waypointCache.Clear();
				}
			}
		}

		public Drawer(PathFollower target)
		{
			cachedCurrentWaypoint = -1;
			waypointCache = new List<Path.Waypoint>();
			wireMeshBuilder = new WireMeshBuilder();
			passDistanceDrawer = DebugOverlay.GetMeshDrawer();
			pathDrawer = DebugOverlay.GetMeshDrawer();
			int num = 32;
			float sliceSize = 360f / (float)num;
			Vector3 halfForward = Vector3.forward * 0.5f;
			Vector3 p = GetCirclePosition(-1);
			for (int j = 0; j < num; j++)
			{
				Vector3 vector = GetCirclePosition(j);
				wireMeshBuilder.AddLine(p, Color.yellow, vector, Color.yellow);
				p = vector;
			}
			passDistanceDrawer.hasMeshOwnership = true;
			passDistanceDrawer.mesh = wireMeshBuilder.GenerateMesh();
			wireMeshBuilder.Clear();
			pathDrawer.mesh = new Mesh();
			RoR2Application.onFixedUpdate += FixedUpdate;
			this.target = target;
			Vector3 GetCirclePosition(int i)
			{
				return Quaternion.AngleAxis((float)i * sliceSize, Vector3.up) * halfForward;
			}
		}

		private void SetWaypoints(List<Path.Waypoint> newWaypoints, int newCurrentWaypoint)
		{
			if (!ListUtils.SequenceEquals(waypointCache, newWaypoints) || !cachedCurrentWaypoint.Equals(newCurrentWaypoint))
			{
				waypointCache.Clear();
				waypointCache.AddRange(newWaypoints);
				cachedCurrentWaypoint = newCurrentWaypoint;
				RebuildPathMesh();
			}
		}

		private void RebuildPathMesh()
		{
			List<Path.Waypoint> waypoints = target.waypoints;
			Color nodeColor;
			Color traversedPathColor;
			Color currentPathColor;
			Color untraversedPathColor;
			Color drawPathColor;
			if (waypoints.Count > 1)
			{
				nodeColor = new Color(1f, 0.5f, 0.5f, 0.25f);
				traversedPathColor = new Color(0.5f, 0.5f, 0.5f, 0.25f);
				currentPathColor = new Color(1f, 0.5f, 0.5f, 1f);
				untraversedPathColor = new Color(0.5f, 0f, 0f, 0.25f);
				target.nodeGraph.GetNodePosition(waypoints[0].nodeIndex, out var position2);
				drawPathColor = untraversedPathColor;
				DrawNode(position2);
				for (int j = 1; j < waypoints.Count; j++)
				{
					UpdateDrawPathColor(j);
					target.nodeGraph.GetNodePosition(waypoints[j].nodeIndex, out var position3);
					wireMeshBuilder.AddLine(position2, drawPathColor, position3, drawPathColor);
					DrawNode(position3);
					position2 = position3;
				}
			}
			wireMeshBuilder.GenerateMesh(pathDrawer.mesh);
			wireMeshBuilder.Clear();
			void DrawNode(Vector3 position)
			{
				wireMeshBuilder.AddLine(position - Vector3.up, nodeColor, position + Vector3.up, nodeColor);
			}
			void UpdateDrawPathColor(int i)
			{
				if (cachedCurrentWaypoint == i)
				{
					drawPathColor = currentPathColor;
				}
				else if (cachedCurrentWaypoint < i)
				{
					drawPathColor = untraversedPathColor;
				}
				else
				{
					drawPathColor = traversedPathColor;
				}
			}
		}

		private void FixedUpdate()
		{
			if (target != null)
			{
				passDistanceDrawer.enabled = target.currentWaypoint < target.waypoints.Count;
				if (passDistanceDrawer.enabled)
				{
					target.nodeGraph.GetNodePosition(target.waypoints[target.currentWaypoint].nodeIndex, out var position);
					passDistanceDrawer.transform.position = position;
					passDistanceDrawer.transform.localScale = Vector3.one * target.debugPassDistance;
				}
				SetWaypoints(target.waypoints, target.currentWaypoint);
			}
		}

		public void Dispose()
		{
			target = null;
			RoR2Application.onFixedUpdate -= FixedUpdate;
			pathDrawer.Dispose();
			passDistanceDrawer.Dispose();
			wireMeshBuilder.Dispose();
		}
	}

	private Vector3 currentPosition;

	private const float waypointPassDistance = 2f;

	private const float waypointPassYTolerance = 2f;

	private List<Path.Waypoint> waypoints = new List<Path.Waypoint>();

	private int currentWaypoint;

	private NodeGraph.NodeIndex previousNode = NodeGraph.NodeIndex.invalid;

	private float debugPassDistance;

	public NodeGraph nodeGraph { get; private set; }

	public NodeGraph.NodeIndex nextNode { get; private set; } = NodeGraph.NodeIndex.invalid;

	public bool isFinished => currentWaypoint >= waypoints.Count;

	public bool isOnJumpLink => false;

	public bool hasPassedFirstWaypoint => currentWaypoint > 0;

	public bool nextWaypointNeedsJump
	{
		get
		{
			if (waypoints.Count > 0 && currentWaypoint < waypoints.Count)
			{
				return waypoints[currentWaypoint].minJumpHeight > 0f;
			}
			return false;
		}
	}

	private static float DistanceXZ(Vector3 a, Vector3 b)
	{
		a.y = 0f;
		b.y = 0f;
		return Vector3.Distance(a, b);
	}

	public float CalculateJumpVelocityNeededToReachNextWaypoint(float moveSpeed)
	{
		if (!nextWaypointNeedsJump)
		{
			return 0f;
		}
		Vector3 a = currentPosition;
		if (!GetNextNodePosition(out var nextPosition))
		{
			return 0f;
		}
		return Trajectory.CalculateInitialYSpeed(Trajectory.CalculateGroundTravelTime(moveSpeed, DistanceXZ(a, nextPosition)), nextPosition.y - a.y);
	}

	public void UpdatePosition(Vector3 newPosition)
	{
		Vector3 vector = currentPosition;
		currentPosition = newPosition;
		if (GetNextNodePosition(out var nextPosition))
		{
			Vector3 vector2 = nextPosition - currentPosition;
			Vector3 vector3 = vector2;
			vector3.y = 0f;
			float num = 2f;
			Vector3 inNormal = currentPosition - vector;
			Plane plane = new Plane(inNormal, nextPosition);
			if (plane.GetSide(currentPosition) != plane.GetSide(vector))
			{
				num += 2f;
			}
			if (waypoints.Count > currentWaypoint + 1 && waypoints[currentWaypoint + 1].minJumpHeight > 0f)
			{
				num = 0.5f;
			}
			debugPassDistance = num;
			if (num * num >= vector3.sqrMagnitude && Mathf.Abs(vector2.y) <= 2f)
			{
				SetWaypoint(currentWaypoint + 1);
			}
		}
		_ = nextNode != NodeGraph.NodeIndex.invalid;
	}

	private void SetWaypoint(int newWaypoint)
	{
		currentWaypoint = Math.Min(newWaypoint, waypoints.Count);
		if (currentWaypoint == waypoints.Count)
		{
			nextNode = NodeGraph.NodeIndex.invalid;
			previousNode = NodeGraph.NodeIndex.invalid;
		}
		else
		{
			nextNode = waypoints[currentWaypoint].nodeIndex;
			previousNode = ((currentWaypoint > 0) ? waypoints[currentWaypoint - 1].nodeIndex : NodeGraph.NodeIndex.invalid);
		}
	}

	public void Reset()
	{
		nodeGraph = null;
		nextNode = NodeGraph.NodeIndex.invalid;
		previousNode = NodeGraph.NodeIndex.invalid;
		waypoints.Clear();
		currentWaypoint = 0;
	}

	public void SetPath(Path newPath)
	{
		if (nodeGraph != newPath.nodeGraph)
		{
			Reset();
			nodeGraph = newPath.nodeGraph;
		}
		waypoints.Clear();
		newPath.WriteWaypointsToList(waypoints);
		currentWaypoint = 0;
		for (int i = 1; i < waypoints.Count; i++)
		{
			if (waypoints[i].nodeIndex == nextNode && waypoints[i - 1].nodeIndex == previousNode)
			{
				currentWaypoint = i;
				break;
			}
		}
		SetWaypoint(currentWaypoint);
	}

	private void GetPreviousAndNextNodePositions(out Vector3? previousPosition, out Vector3? nextPosition)
	{
		previousPosition = null;
		nextPosition = null;
		if (!(nodeGraph == null))
		{
			if (nodeGraph.GetNodePosition(previousNode, out var position))
			{
				previousPosition = position;
			}
			if (nodeGraph.GetNodePosition(nextNode, out position))
			{
				nextPosition = position;
			}
		}
	}

	private bool GetNextNodePosition(out Vector3 nextPosition)
	{
		if (nodeGraph != null && nextNode != NodeGraph.NodeIndex.invalid && nodeGraph.GetNodePosition(nextNode, out nextPosition))
		{
			return true;
		}
		nextPosition = currentPosition;
		return false;
	}

	public Vector3? GetNextPosition()
	{
		if (GetNextNodePosition(out var nextPosition))
		{
			return nextPosition;
		}
		return null;
	}

	public void DebugDrawPath(Color color, float duration)
	{
		for (int i = 1; i < waypoints.Count; i++)
		{
			nodeGraph.GetNodePosition(waypoints[i].nodeIndex, out var position);
			nodeGraph.GetNodePosition(waypoints[i - 1].nodeIndex, out var position2);
			Debug.DrawLine(position, position2, color, duration);
		}
		for (int j = 0; j < waypoints.Count; j++)
		{
			nodeGraph.GetNodePosition(waypoints[j].nodeIndex, out var position3);
			Debug.DrawLine(position3 + Vector3.up, position3 - Vector3.up, color, duration);
		}
	}
}
