using System;
using System.Collections.Generic;
using RoR2.Navigation;

namespace RoR2;

public class Path : IDisposable
{
	public struct Waypoint : IEquatable<Waypoint>
	{
		public NodeGraph.NodeIndex nodeIndex;

		public float minJumpHeight;

		public bool Equals(Waypoint other)
		{
			if (nodeIndex.Equals(other.nodeIndex))
			{
				return minJumpHeight.Equals(other.minJumpHeight);
			}
			return false;
		}

		public override bool Equals(object other)
		{
			if (other is Waypoint other2)
			{
				return Equals(other2);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (nodeIndex.GetHashCode() * 397) ^ minJumpHeight.GetHashCode();
		}
	}

	private static readonly Stack<Waypoint[]> waypointsBufferPool = new Stack<Waypoint[]>();

	private const int pooledBufferSize = 64;

	private Waypoint[] waypointsBuffer;

	private int firstWaypointIndex;

	public NodeGraph nodeGraph { get; private set; }

	public PathStatus status { get; set; }

	public int waypointsCount { get; private set; }

	public Waypoint this[int i] => waypointsBuffer[firstWaypointIndex + i];

	private static Waypoint[] GetWaypointsBuffer()
	{
		if (waypointsBufferPool.Count == 0)
		{
			return new Waypoint[64];
		}
		return waypointsBufferPool.Pop();
	}

	private static void FreeWaypointsBuffer(Waypoint[] buffer)
	{
		if (buffer.Length == 64)
		{
			waypointsBufferPool.Push(buffer);
		}
	}

	public Path(NodeGraph nodeGraph)
	{
		this.nodeGraph = nodeGraph;
		waypointsBuffer = GetWaypointsBuffer();
		firstWaypointIndex = waypointsBuffer.Length;
	}

	public void Dispose()
	{
		FreeWaypointsBuffer(waypointsBuffer);
	}

	public void PushWaypointToFront(NodeGraph.NodeIndex nodeIndex, float minJumpHeight)
	{
		if (waypointsCount + 1 >= waypointsBuffer.Length)
		{
			Waypoint[] array = waypointsBuffer;
			waypointsBuffer = new Waypoint[waypointsCount + 32];
			Array.Copy(array, 0, waypointsBuffer, waypointsBuffer.Length - array.Length, array.Length);
			FreeWaypointsBuffer(array);
		}
		firstWaypointIndex = waypointsBuffer.Length - ++waypointsCount;
		waypointsBuffer[firstWaypointIndex] = new Waypoint
		{
			nodeIndex = nodeIndex,
			minJumpHeight = minJumpHeight
		};
	}

	public void WriteWaypointsToList(List<Waypoint> waypointsList)
	{
		if (waypointsList.Capacity < waypointsList.Count + waypointsCount)
		{
			waypointsList.Capacity = waypointsList.Count + waypointsCount;
		}
		for (int i = firstWaypointIndex; i < waypointsBuffer.Length; i++)
		{
			waypointsList.Add(waypointsBuffer[i]);
		}
	}

	public void Clear()
	{
		status = PathStatus.Invalid;
		waypointsCount = 0;
		firstWaypointIndex = waypointsBuffer.Length;
	}
}
