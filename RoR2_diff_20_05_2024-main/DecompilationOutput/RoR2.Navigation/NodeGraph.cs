using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using HG;
using Unity.Collections;
using UnityEngine;

namespace RoR2.Navigation;

[CreateAssetMenu(menuName = "RoR2/NodeGraph")]
[PreferBinarySerialization]
public class NodeGraph : ScriptableObject
{
	[Serializable]
	public struct NodeIndex : IEquatable<NodeIndex>
	{
		public int nodeIndex;

		public static readonly NodeIndex invalid = new NodeIndex(-1);

		public NodeIndex(int nodeIndex)
		{
			this.nodeIndex = nodeIndex;
		}

		public static bool operator ==(NodeIndex lhs, NodeIndex rhs)
		{
			return lhs.nodeIndex == rhs.nodeIndex;
		}

		public static bool operator !=(NodeIndex lhs, NodeIndex rhs)
		{
			return lhs.nodeIndex != rhs.nodeIndex;
		}

		public override bool Equals(object other)
		{
			if (other is NodeIndex)
			{
				return ((NodeIndex)other).nodeIndex == nodeIndex;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return nodeIndex;
		}

		public bool Equals(NodeIndex other)
		{
			return nodeIndex == other.nodeIndex;
		}
	}

	[Serializable]
	public struct LinkIndex
	{
		public int linkIndex;

		public static readonly LinkIndex invalid = new LinkIndex
		{
			linkIndex = -1
		};

		public static bool operator ==(LinkIndex lhs, LinkIndex rhs)
		{
			return lhs.linkIndex == rhs.linkIndex;
		}

		public static bool operator !=(LinkIndex lhs, LinkIndex rhs)
		{
			return lhs.linkIndex != rhs.linkIndex;
		}

		public override bool Equals(object other)
		{
			if (other is LinkIndex)
			{
				return ((LinkIndex)other).linkIndex == linkIndex;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return linkIndex;
		}
	}

	[Serializable]
	public struct LinkListIndex
	{
		public int index;

		public uint size;
	}

	[Serializable]
	public struct Node
	{
		public Vector3 position;

		public LinkListIndex linkListIndex;

		public HullMask forbiddenHulls;

		public SerializableBitArray lineOfSightMask;

		public byte gateIndex;

		public NodeFlags flags;
	}

	[Serializable]
	public struct Link
	{
		public NodeIndex nodeIndexA;

		public NodeIndex nodeIndexB;

		public float distanceScore;

		public float maxSlope;

		public float minJumpHeight;

		public int hullMask;

		public int jumpHullMask;

		public byte gateIndex;
	}

	private struct NodePositionGetter : IPosition3Getter<NodeIndex>
	{
		private readonly NodeGraph nodeGraph;

		public NodePositionGetter(NodeGraph nodeGraph)
		{
			this.nodeGraph = nodeGraph;
		}

		public Vector3 GetPosition3(NodeIndex item)
		{
			return nodeGraph.nodes[item.nodeIndex].position;
		}
	}

	public readonly struct PathRequestPosition
	{
		public readonly NodeIndex? nodeIndex;

		public readonly Vector3? position;

		private PathRequestPosition(NodeIndex nodeIndex)
		{
			this.nodeIndex = nodeIndex;
			position = null;
		}

		private PathRequestPosition(Vector3 position)
		{
			nodeIndex = null;
			this.position = position;
		}

		public static implicit operator PathRequestPosition(NodeIndex nodeIndex)
		{
			return new PathRequestPosition(nodeIndex);
		}

		public static implicit operator PathRequestPosition(Vector3 position)
		{
			return new PathRequestPosition(position);
		}
	}

	public class PathRequest
	{
		public Path path;

		public PathRequestPosition startPos;

		public PathRequestPosition endPos;

		public HullClassification hullClassification;

		public float maxSlope;

		public float maxJumpHeight;

		public float maxSpeed;

		public PathRequest()
		{
			Init();
		}

		public void Reset()
		{
			Init();
		}

		private void Init()
		{
			path = null;
			startPos = NodeIndex.invalid;
			endPos = NodeIndex.invalid;
			hullClassification = HullClassification.Human;
			maxSlope = 0f;
			maxJumpHeight = 0f;
			maxSpeed = 0f;
		}
	}

	private static class NodeFilters
	{
		public interface INodeCheckFilterComponent
		{
			bool CheckNode(NodeGraph nodeGraph, ref Node node);
		}

		public struct NodeCheckFilterAnd<TFilterA, TFilterB> : INodeCheckFilterComponent where TFilterA : INodeCheckFilterComponent where TFilterB : INodeCheckFilterComponent
		{
			private TFilterA filterA;

			private TFilterB filterB;

			public NodeCheckFilterAnd(TFilterA filterA, TFilterB filterB)
			{
				this.filterA = filterA;
				this.filterB = filterB;
			}

			public bool CheckNode(NodeGraph nodeGraph, ref Node node)
			{
				if (filterA.CheckNode(nodeGraph, ref node))
				{
					return filterB.CheckNode(nodeGraph, ref node);
				}
				return false;
			}
		}

		public struct NodeSearchFilter<TNodeCheckFilter> : IBlockMapSearchFilter<NodeIndex> where TNodeCheckFilter : INodeCheckFilterComponent
		{
			private readonly NodeGraph nodeGraph;

			private TNodeCheckFilter nodeCheckFilter;

			public NodeSearchFilter(NodeGraph nodeGraph, TNodeCheckFilter nodeCheckFilter)
			{
				this.nodeGraph = nodeGraph;
				this.nodeCheckFilter = nodeCheckFilter;
			}

			public bool CheckItem(NodeIndex item, ref bool shouldFinish)
			{
				return nodeCheckFilter.CheckNode(nodeGraph, ref nodeGraph.nodes[item.nodeIndex]);
			}
		}

		public readonly struct NodeHullFilter : INodeCheckFilterComponent
		{
			private readonly HullMask hullMask;

			public NodeHullFilter(HullMask hullMask)
			{
				this.hullMask = hullMask;
			}

			public NodeHullFilter(HullClassification hullClassification)
			{
				hullMask = (HullMask)(1 << (int)hullClassification);
			}

			public bool CheckNode(NodeGraph nodeGraph, ref Node node)
			{
				return (node.forbiddenHulls & hullMask) == 0;
			}
		}

		public readonly struct NodeFlagsFilter : INodeCheckFilterComponent
		{
			private readonly NodeFlags requiredFlags;

			private readonly NodeFlags forbiddenFlags;

			public NodeFlagsFilter(NodeFlags requiredFlags, NodeFlags forbiddenFlags)
			{
				this.requiredFlags = requiredFlags;
				this.forbiddenFlags = forbiddenFlags;
			}

			public bool CheckNode(NodeGraph nodeGraph, ref Node node)
			{
				NodeFlags flags = node.flags;
				if ((flags & forbiddenFlags) == 0)
				{
					return (flags & requiredFlags) == requiredFlags;
				}
				return false;
			}
		}

		[StructLayout(LayoutKind.Sequential, Size = 1)]
		public readonly struct NodeAvailableFilter : INodeCheckFilterComponent
		{
			public bool CheckNode(NodeGraph nodeGraph, ref Node node)
			{
				if (node.gateIndex != 0)
				{
					return nodeGraph.openGates[node.gateIndex];
				}
				return true;
			}
		}

		public struct NodeMinDistanceFilter : INodeCheckFilterComponent
		{
			private readonly Vector3 position;

			private readonly float minDistanceSqr;

			public NodeMinDistanceFilter(Vector3 position, float minDistance)
			{
				this.position = position;
				minDistanceSqr = minDistance * minDistance;
			}

			public bool CheckNode(NodeGraph nodeGraph, ref Node node)
			{
				return (node.position - position).sqrMagnitude >= minDistanceSqr;
			}
		}

		public struct NodeRaycastFilter : INodeCheckFilterComponent
		{
			private readonly Vector3 raycastOrigin;

			private readonly Vector3 raycastOffset;

			private readonly int maxRaycasts;

			private int raycastsPerformed;

			public NodeRaycastFilter(Vector3 raycastOrigin, Vector3 raycastOffset, int maxRaycasts)
			{
				this.raycastOrigin = raycastOrigin + raycastOffset;
				this.raycastOffset = raycastOffset;
				this.maxRaycasts = maxRaycasts;
				raycastsPerformed = 0;
			}

			public bool CheckNode(NodeGraph nodeGraph, ref Node node)
			{
				if (raycastsPerformed < maxRaycasts)
				{
					raycastsPerformed++;
					if (Physics.Linecast(raycastOrigin, node.position + raycastOffset, out var _, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
					{
						return false;
					}
				}
				return true;
			}
		}

		public struct NodeOverheadFilter : INodeCheckFilterComponent
		{
			private Vector3 position;

			private bool enabled;

			public NodeOverheadFilter(Vector3 position, bool enabled)
			{
				this.position = position;
				this.enabled = enabled;
			}

			public bool CheckNode(NodeGraph nodeGraph, ref Node node)
			{
				if (enabled && Vector3.Dot((node.position - position).normalized, Vector3.up) > 0.70710677f)
				{
					return false;
				}
				return true;
			}
		}

		public static NodeSearchFilter<TFilter> Create<TFilter>(NodeGraph nodeGraph, TFilter nodeCheckFilter) where TFilter : INodeCheckFilterComponent
		{
			return new NodeSearchFilter<TFilter>(nodeGraph, nodeCheckFilter);
		}

		public static NodeCheckFilterAnd<TFilterA, TFilterB> And<TFilterA, TFilterB>(TFilterA filterA, TFilterB filterB) where TFilterA : INodeCheckFilterComponent where TFilterB : INodeCheckFilterComponent
		{
			return new NodeCheckFilterAnd<TFilterA, TFilterB>(filterA, filterB);
		}

		public static NodeCheckFilterAnd<TFilterA, NodeCheckFilterAnd<TFilterB, TFilterC>> And<TFilterA, TFilterB, TFilterC>(TFilterA filterA, TFilterB filterB, TFilterC filterC) where TFilterA : INodeCheckFilterComponent where TFilterB : INodeCheckFilterComponent where TFilterC : INodeCheckFilterComponent
		{
			return And(filterA, And(filterB, filterC));
		}

		public static NodeCheckFilterAnd<TFilterA, NodeCheckFilterAnd<TFilterB, NodeCheckFilterAnd<TFilterC, TFilterD>>> And<TFilterA, TFilterB, TFilterC, TFilterD>(TFilterA filterA, TFilterB filterB, TFilterC filterC, TFilterD filterD) where TFilterA : INodeCheckFilterComponent where TFilterB : INodeCheckFilterComponent where TFilterC : INodeCheckFilterComponent where TFilterD : INodeCheckFilterComponent
		{
			return And(filterA, And(filterB, filterC, filterD));
		}

		public static NodeCheckFilterAnd<TFilterA, NodeCheckFilterAnd<TFilterB, NodeCheckFilterAnd<TFilterC, NodeCheckFilterAnd<TFilterD, TFilterE>>>> And<TFilterA, TFilterB, TFilterC, TFilterD, TFilterE>(TFilterA filterA, TFilterB filterB, TFilterC filterC, TFilterD filterD, TFilterE filterE) where TFilterA : INodeCheckFilterComponent where TFilterB : INodeCheckFilterComponent where TFilterC : INodeCheckFilterComponent where TFilterD : INodeCheckFilterComponent where TFilterE : INodeCheckFilterComponent
		{
			return And(filterA, And(filterB, filterC, filterD, filterE));
		}
	}

	[SerializeField]
	private Node[] nodes = Array.Empty<Node>();

	[SerializeField]
	private Link[] links = Array.Empty<Link>();

	[SerializeField]
	private List<string> gateNames = new List<string> { "" };

	private bool[] openGates = new bool[256];

	public Vector3 cellSize = new Vector3(15f, 30f, 15f);

	private BlockMap<NodeIndex, NodePositionGetter> blockMap = new BlockMap<NodeIndex, NodePositionGetter>();

	private FixedSizeArrayPool<bool> boolPerNodePool = new FixedSizeArrayPool<bool>(0);

	private FixedSizeArrayPool<float> floatPerNodePool = new FixedSizeArrayPool<float>(0);

	private FixedSizeArrayPool<NodeIndex> nodePerNodePool = new FixedSizeArrayPool<NodeIndex>(0);

	private FixedSizeArrayPool<LinkIndex> linkPerNodePool = new FixedSizeArrayPool<LinkIndex>(0);

	private const float overheadDotLimit = 0.70710677f;

	private void OnNodeCountChanged()
	{
		boolPerNodePool.lengthOfArrays = nodes.Length;
		floatPerNodePool.lengthOfArrays = nodes.Length;
		nodePerNodePool.lengthOfArrays = nodes.Length;
		linkPerNodePool.lengthOfArrays = nodes.Length;
	}

	private void Awake()
	{
		OnNodeCountChanged();
		RebuildBlockMap();
	}

	private void RebuildBlockMap()
	{
		NativeArray<NodeIndex> nativeArray = new NativeArray<NodeIndex>(nodes.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
		for (int i = 0; i < nodes.Length; i++)
		{
			nativeArray[i] = new NodeIndex(i);
		}
		try
		{
			blockMap.SetCellSize(cellSize);
			blockMap.Set(new NativeArrayIListWrapper<NodeIndex>(nativeArray), nativeArray.Length, new NodePositionGetter(this));
		}
		finally
		{
			nativeArray.Dispose();
		}
	}

	public void Clear()
	{
		nodes = Array.Empty<Node>();
		links = Array.Empty<Link>();
		gateNames.Clear();
		gateNames.Add("");
		blockMap.Reset();
		OnNodeCountChanged();
	}

	public void SetNodes(ReadOnlyCollection<MapNode> mapNodes, ReadOnlyCollection<SerializableBitArray> lineOfSightMasks)
	{
		Clear();
		Dictionary<MapNode, NodeIndex> dictionary = new Dictionary<MapNode, NodeIndex>();
		List<Node> list = new List<Node>();
		List<Link> list2 = new List<Link>();
		for (int i = 0; i < mapNodes.Count; i++)
		{
			MapNode key = mapNodes[i];
			dictionary[key] = new NodeIndex(i);
		}
		for (int j = 0; j < mapNodes.Count; j++)
		{
			MapNode mapNode = mapNodes[j];
			NodeIndex nodeIndexA = dictionary[mapNode];
			int count = list2.Count;
			for (int k = 0; k < mapNode.links.Count; k++)
			{
				MapNode.Link link = mapNode.links[k];
				if (!dictionary.ContainsKey(link.nodeB))
				{
					Debug.LogErrorFormat(link.nodeB, "[{0}] Node {1} was not registered.", k, link.nodeB);
				}
				list2.Add(new Link
				{
					nodeIndexA = nodeIndexA,
					nodeIndexB = dictionary[link.nodeB],
					distanceScore = link.distanceScore,
					minJumpHeight = link.minJumpHeight,
					hullMask = link.hullMask,
					jumpHullMask = link.jumpHullMask,
					gateIndex = RegisterGateName(link.gateName)
				});
			}
			HullMask hullMask = mapNode.forbiddenHulls;
			for (HullClassification hullClassification = HullClassification.Human; hullClassification < HullClassification.Count; hullClassification++)
			{
				bool flag = false;
				int num = 1 << (int)hullClassification;
				List<MapNode.Link> list3 = mapNode.links;
				for (int l = 0; l < list3.Count; l++)
				{
					if ((list3[l].hullMask & num) != 0)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					hullMask = (HullMask)((int)hullMask | num);
				}
			}
			list.Add(new Node
			{
				position = mapNode.transform.position,
				linkListIndex = new LinkListIndex
				{
					index = count,
					size = (uint)mapNode.links.Count
				},
				forbiddenHulls = hullMask,
				flags = mapNode.flags,
				lineOfSightMask = new SerializableBitArray(lineOfSightMasks[j]),
				gateIndex = RegisterGateName(mapNode.gateName)
			});
		}
		nodes = list.ToArray();
		links = list2.ToArray();
		OnNodeCountChanged();
		RebuildBlockMap();
	}

	public Vector3 GetQuadraticCoordinates(float t, Vector3 startPos, Vector3 apexPos, Vector3 endPos)
	{
		return Mathf.Pow(1f - t, 2f) * startPos + 2f * t * (1f - t) * apexPos + Mathf.Pow(t, 2f) * endPos;
	}

	public Mesh GenerateLinkDebugMesh(HullMask hullMask)
	{
		using WireMeshBuilder wireMeshBuilder = new WireMeshBuilder();
		Link[] array = links;
		for (int i = 0; i < array.Length; i++)
		{
			Link link = array[i];
			if (((uint)link.hullMask & (uint)hullMask) == 0)
			{
				continue;
			}
			Vector3 position = nodes[link.nodeIndexA.nodeIndex].position;
			Vector3 position2 = nodes[link.nodeIndexB.nodeIndex].position;
			Vector3 vector = (position + position2) * 0.5f;
			bool num = ((uint)link.jumpHullMask & (uint)hullMask) != 0;
			Color color = (num ? Color.cyan : Color.green);
			if (num)
			{
				Vector3 apexPos = vector;
				apexPos.y = position.y + link.minJumpHeight;
				int num2 = 8;
				Vector3 p = position;
				for (int j = 1; j <= num2; j++)
				{
					if (j > num2 / 2)
					{
						color.a = 0.1f;
					}
					Vector3 quadraticCoordinates = GetQuadraticCoordinates((float)j / (float)num2, position, apexPos, position2);
					wireMeshBuilder.AddLine(p, color, quadraticCoordinates, color);
					p = quadraticCoordinates;
				}
			}
			else
			{
				Color c = color;
				c.a = 0.1f;
				wireMeshBuilder.AddLine(position, color, (position + position2) * 0.5f, c);
			}
		}
		return wireMeshBuilder.GenerateMesh();
	}

	public void DebugDrawLinks(HullClassification hull)
	{
		int num = 1 << (int)hull;
		Link[] array = links;
		for (int i = 0; i < array.Length; i++)
		{
			Link link = array[i];
			if ((link.hullMask & num) == 0)
			{
				continue;
			}
			Vector3 position = nodes[link.nodeIndexA.nodeIndex].position;
			Vector3 position2 = nodes[link.nodeIndexB.nodeIndex].position;
			Vector3 vector = (position + position2) * 0.5f;
			bool num2 = (link.jumpHullMask & num) != 0;
			Color color = (num2 ? Color.cyan : Color.green);
			if (num2)
			{
				Vector3 apexPos = vector;
				apexPos.y = position.y + link.minJumpHeight;
				int num3 = 8;
				Vector3 start = position;
				for (int j = 1; j <= num3; j++)
				{
					if (j > num3 / 2)
					{
						color.a = 0.1f;
					}
					Vector3 quadraticCoordinates = GetQuadraticCoordinates((float)j / (float)num3, position, apexPos, position2);
					Debug.DrawLine(start, quadraticCoordinates, color, 10f);
					start = quadraticCoordinates;
				}
			}
			else
			{
				Debug.DrawLine(position, vector, color, 10f, depthTest: false);
				Color color2 = color;
				color2.a = 0.1f;
				Debug.DrawLine(vector, position2, color2, 10f, depthTest: false);
			}
		}
	}

	public void DebugDrawPath(Vector3 startPos, Vector3 endPos)
	{
		Path path = new Path(this);
		ComputePath(new PathRequest
		{
			startPos = startPos,
			endPos = endPos,
			path = path,
			hullClassification = HullClassification.Human
		}).Wait();
		if (path.status == PathStatus.Valid)
		{
			for (int i = 1; i < path.waypointsCount; i++)
			{
				Debug.DrawLine(nodes[path[i - 1].nodeIndex.nodeIndex].position, nodes[path[i].nodeIndex.nodeIndex].position, Color.red, 10f);
			}
		}
	}

	public void DebugHighlightNodesWithNoLinks()
	{
		Node[] array = nodes;
		for (int i = 0; i < array.Length; i++)
		{
			Node node = array[i];
			if (node.linkListIndex.size == 0)
			{
				Debug.DrawRay(node.position, Vector3.up * 100f, Color.cyan, 60f);
			}
		}
	}

	public int GetNodeCount()
	{
		return nodes.Length;
	}

	public List<NodeIndex> GetActiveNodesForHullMask(HullMask hullMask)
	{
		List<NodeIndex> list = new List<NodeIndex>(nodes.Length);
		GetActiveNodesForHullMask(hullMask, list);
		return list;
	}

	public void GetActiveNodesForHullMask(HullMask hullMask, List<NodeIndex> dest)
	{
		dest.Capacity = Math.Max(dest.Capacity, nodes.Length);
		for (int i = 0; i < nodes.Length; i++)
		{
			if ((nodes[i].forbiddenHulls & hullMask) == 0 && (nodes[i].gateIndex == 0 || openGates[nodes[i].gateIndex]))
			{
				dest.Add(new NodeIndex(i));
			}
		}
	}

	public List<NodeIndex> GetActiveNodesForHullMaskWithFlagConditions(HullMask hullMask, NodeFlags requiredFlags, NodeFlags forbiddenFlags)
	{
		List<NodeIndex> list = new List<NodeIndex>(nodes.Length);
		GetActiveNodesForHullMaskWithFlagConditions(hullMask, requiredFlags, forbiddenFlags, list);
		return list;
	}

	public void GetActiveNodesForHullMaskWithFlagConditions(HullMask hullMask, NodeFlags requiredFlags, NodeFlags forbiddenFlags, List<NodeIndex> dest)
	{
		for (int i = 0; i < nodes.Length; i++)
		{
			NodeFlags flags = nodes[i].flags;
			if ((flags & forbiddenFlags) == 0 && (flags & requiredFlags) == requiredFlags && (nodes[i].forbiddenHulls & hullMask) == 0 && (nodes[i].gateIndex == 0 || openGates[nodes[i].gateIndex]))
			{
				dest.Add(new NodeIndex(i));
			}
		}
	}

	public List<NodeIndex> FindNodesInRange(Vector3 position, float minRange, float maxRange, HullMask hullMask)
	{
		List<NodeIndex> list = new List<NodeIndex>();
		FindNodesInRange(position, minRange, maxRange, hullMask, list);
		return list;
	}

	public void FindNodesInRange(Vector3 position, float minRange, float maxRange, HullMask hullMask, List<NodeIndex> dest)
	{
		NodeFilters.NodeSearchFilter<NodeFilters.NodeCheckFilterAnd<NodeFilters.NodeHullFilter, NodeFilters.NodeCheckFilterAnd<NodeFilters.NodeAvailableFilter, NodeFilters.NodeMinDistanceFilter>>> filter = NodeFilters.Create(this, NodeFilters.And(new NodeFilters.NodeHullFilter(hullMask), default(NodeFilters.NodeAvailableFilter), new NodeFilters.NodeMinDistanceFilter(position, minRange)));
		blockMap.GetNearestItemsWhichPassFilter(position, maxRange, ref filter, dest);
	}

	public List<NodeIndex> FindNodesInRangeWithFlagConditions(Vector3 position, float minRange, float maxRange, HullMask hullMask, NodeFlags requiredFlags, NodeFlags forbiddenFlags, bool preventOverhead)
	{
		List<NodeIndex> list = new List<NodeIndex>();
		FindNodesInRangeWithFlagConditions(position, minRange, maxRange, hullMask, requiredFlags, forbiddenFlags, preventOverhead, list);
		return list;
	}

	public void FindNodesInRangeWithFlagConditions(Vector3 position, float minRange, float maxRange, HullMask hullMask, NodeFlags requiredFlags, NodeFlags forbiddenFlags, bool preventOverhead, List<NodeIndex> dest)
	{
		NodeFilters.NodeSearchFilter<NodeFilters.NodeCheckFilterAnd<NodeFilters.NodeFlagsFilter, NodeFilters.NodeCheckFilterAnd<NodeFilters.NodeHullFilter, NodeFilters.NodeCheckFilterAnd<NodeFilters.NodeAvailableFilter, NodeFilters.NodeCheckFilterAnd<NodeFilters.NodeOverheadFilter, NodeFilters.NodeMinDistanceFilter>>>>> filter = NodeFilters.Create(this, NodeFilters.And(new NodeFilters.NodeFlagsFilter(requiredFlags, forbiddenFlags), new NodeFilters.NodeHullFilter(hullMask), default(NodeFilters.NodeAvailableFilter), new NodeFilters.NodeOverheadFilter(position, preventOverhead), new NodeFilters.NodeMinDistanceFilter(position, minRange)));
		blockMap.GetNearestItemsWhichPassFilter(position, maxRange, ref filter, dest);
	}

	public bool GetNodePosition(NodeIndex nodeIndex, out Vector3 position)
	{
		if (nodeIndex != NodeIndex.invalid && nodeIndex.nodeIndex < nodes.Length)
		{
			position = nodes[nodeIndex.nodeIndex].position;
			return true;
		}
		position = Vector3.zero;
		return false;
	}

	public bool GetNodeFlags(NodeIndex nodeIndex, out NodeFlags flags)
	{
		if (nodeIndex != NodeIndex.invalid && nodeIndex.nodeIndex < nodes.Length)
		{
			flags = nodes[nodeIndex.nodeIndex].flags;
			return true;
		}
		flags = NodeFlags.None;
		return false;
	}

	public LinkIndex[] GetActiveNodeLinks(NodeIndex nodeIndex)
	{
		if (nodeIndex != NodeIndex.invalid && nodeIndex.nodeIndex < nodes.Length)
		{
			LinkListIndex linkListIndex = nodes[nodeIndex.nodeIndex].linkListIndex;
			LinkIndex[] array = new LinkIndex[linkListIndex.size];
			int index = linkListIndex.index;
			for (int i = 0; i < linkListIndex.size; i++)
			{
				array[i] = new LinkIndex
				{
					linkIndex = index++
				};
			}
			return array;
		}
		return null;
	}

	public void GetActiveNodeLinks(NodeIndex nodeIndex, List<LinkIndex> results)
	{
		if (nodeIndex != NodeIndex.invalid && nodeIndex.nodeIndex < nodes.Length)
		{
			LinkListIndex linkListIndex = nodes[nodeIndex.nodeIndex].linkListIndex;
			int index = linkListIndex.index;
			for (int i = 0; i < linkListIndex.size; i++)
			{
				results.Add(new LinkIndex
				{
					linkIndex = index++
				});
			}
		}
	}

	public bool TestNodeLineOfSight(NodeIndex nodeIndexA, NodeIndex nodeIndexB)
	{
		if (nodeIndexA != NodeIndex.invalid && nodeIndexA.nodeIndex < nodes.Length && nodeIndexB != NodeIndex.invalid && nodeIndexB.nodeIndex < nodes.Length)
		{
			return nodes[nodeIndexA.nodeIndex].lineOfSightMask[nodeIndexB.nodeIndex];
		}
		return false;
	}

	public bool GetPositionAlongLink(LinkIndex linkIndex, float t, out Vector3 position)
	{
		if (linkIndex != LinkIndex.invalid && linkIndex.linkIndex < links.Length)
		{
			position = Vector3.LerpUnclamped(nodes[links[linkIndex.linkIndex].nodeIndexA.nodeIndex].position, nodes[links[linkIndex.linkIndex].nodeIndexB.nodeIndex].position, t);
			return true;
		}
		position = Vector3.zero;
		return false;
	}

	public bool IsLinkSuitableForHull(LinkIndex linkIndex, HullClassification hullClassification)
	{
		if (linkIndex != LinkIndex.invalid && linkIndex.linkIndex < links.Length)
		{
			if ((links[linkIndex.linkIndex].hullMask & (1 << (int)hullClassification)) != 0)
			{
				if (links[linkIndex.linkIndex].gateIndex != 0)
				{
					return openGates[links[linkIndex.linkIndex].gateIndex];
				}
				return true;
			}
			return false;
		}
		return false;
	}

	public bool IsLinkSuitableForHull(LinkIndex linkIndex, HullMask hullMask)
	{
		if (linkIndex != LinkIndex.invalid && linkIndex.linkIndex < links.Length)
		{
			if (((uint)links[linkIndex.linkIndex].hullMask & (uint)hullMask) != 0)
			{
				if (links[linkIndex.linkIndex].gateIndex != 0)
				{
					return openGates[links[linkIndex.linkIndex].gateIndex];
				}
				return true;
			}
			return false;
		}
		return false;
	}

	public NodeIndex GetLinkStartNode(LinkIndex linkIndex)
	{
		if (linkIndex != LinkIndex.invalid && linkIndex.linkIndex < links.Length)
		{
			return links[linkIndex.linkIndex].nodeIndexA;
		}
		return NodeIndex.invalid;
	}

	public NodeIndex GetLinkEndNode(LinkIndex linkIndex)
	{
		if (linkIndex != LinkIndex.invalid && linkIndex.linkIndex < links.Length)
		{
			return links[linkIndex.linkIndex].nodeIndexB;
		}
		return NodeIndex.invalid;
	}

	public NodeIndex FindClosestNode(Vector3 position, HullClassification hullClassification, float maxDistance = float.PositiveInfinity)
	{
		NodeFilters.NodeSearchFilter<NodeFilters.NodeCheckFilterAnd<NodeFilters.NodeHullFilter, NodeFilters.NodeAvailableFilter>> filter = NodeFilters.Create(this, NodeFilters.And(new NodeFilters.NodeHullFilter(hullClassification), default(NodeFilters.NodeAvailableFilter)));
		if (blockMap.GetNearestItemWhichPassesFilter(position, maxDistance, ref filter, out var dest))
		{
			return dest;
		}
		return NodeIndex.invalid;
	}

	public NodeIndex FindClosestNodeWithRaycast(Vector3 position, HullClassification hullClassification, float maxDistance, int maxRaycasts)
	{
		NodeFilters.NodeSearchFilter<NodeFilters.NodeCheckFilterAnd<NodeFilters.NodeHullFilter, NodeFilters.NodeCheckFilterAnd<NodeFilters.NodeAvailableFilter, NodeFilters.NodeRaycastFilter>>> filter = NodeFilters.Create(this, NodeFilters.And(new NodeFilters.NodeHullFilter(hullClassification), default(NodeFilters.NodeAvailableFilter), new NodeFilters.NodeRaycastFilter(position, new Vector3(0f, 0.2f, 0f), 3)));
		if (blockMap.GetNearestItemWhichPassesFilter(position, maxDistance, ref filter, out var dest))
		{
			return dest;
		}
		return NodeIndex.invalid;
	}

	public NodeIndex FindClosestNodeWithFlagConditions(Vector3 position, HullClassification hullClassification, NodeFlags requiredFlags, NodeFlags forbiddenFlags, bool preventOverhead)
	{
		NodeFilters.NodeSearchFilter<NodeFilters.NodeCheckFilterAnd<NodeFilters.NodeFlagsFilter, NodeFilters.NodeCheckFilterAnd<NodeFilters.NodeHullFilter, NodeFilters.NodeCheckFilterAnd<NodeFilters.NodeAvailableFilter, NodeFilters.NodeOverheadFilter>>>> filter = NodeFilters.Create(this, NodeFilters.And(new NodeFilters.NodeFlagsFilter(requiredFlags, forbiddenFlags), new NodeFilters.NodeHullFilter(hullClassification), default(NodeFilters.NodeAvailableFilter), new NodeFilters.NodeOverheadFilter(position, preventOverhead)));
		if (blockMap.GetNearestItemWhichPassesFilter(position, float.PositiveInfinity, ref filter, out var dest))
		{
			return dest;
		}
		return NodeIndex.invalid;
	}

	private float HeuristicCostEstimate(Vector3 startPos, Vector3 endPos)
	{
		return Vector3.Distance(startPos, endPos);
	}

	private static float DistanceXZ(Vector3 a, Vector3 b)
	{
		a.y = 0f;
		b.y = 0f;
		return Vector3.Distance(a, b);
	}

	private static void ArrayRemoveNodeIndex(NodeIndex[] array, NodeIndex value, int count)
	{
		for (int i = 0; i < count; i++)
		{
			if (array[i] == value)
			{
				array[i] = array[count - 1];
				break;
			}
		}
	}

	private static void ArrayRemoveNodeIndexFromCachedPosition(NodeIndex[] array, int arrayIndex, int count)
	{
		array[arrayIndex] = array[count - 1];
	}

	public PathTask ComputePath(PathRequest pathRequest)
	{
		bool[] array = boolPerNodePool.Request();
		bool[] array2 = boolPerNodePool.Request();
		NodeIndex[] array3 = nodePerNodePool.Request();
		LinkIndex[] array4 = linkPerNodePool.Request();
		ArrayUtils.SetAll(array4, in LinkIndex.invalid);
		float[] array5 = floatPerNodePool.Request();
		ArrayUtils.SetAll(array5, float.PositiveInfinity);
		float[] array6 = floatPerNodePool.Request();
		ArrayUtils.SetAll(array6, float.PositiveInfinity);
		try
		{
			ResolveNodePosition(in pathRequest.startPos, out var dest2, 100f, 2);
			ResolveNodePosition(in pathRequest.endPos, out var dest3, 500f, 0);
			PathTask pathTask = new PathTask(pathRequest.path);
			pathTask.status = PathTask.TaskStatus.Running;
			if (dest2.nodeIndex == NodeIndex.invalid.nodeIndex || dest3.nodeIndex == NodeIndex.invalid.nodeIndex)
			{
				pathRequest.path.Clear();
				pathTask.status = PathTask.TaskStatus.Complete;
				pathTask.wasReachable = false;
				return pathTask;
			}
			int num = 1 << (int)pathRequest.hullClassification;
			array2[dest2.nodeIndex] = true;
			int num2 = 1;
			array3[0] = dest2;
			array5[dest2.nodeIndex] = 0f;
			array6[dest2.nodeIndex] = HeuristicCostEstimate(nodes[dest2.nodeIndex].position, nodes[dest3.nodeIndex].position);
			int arrayIndex = -1;
			while (num2 > 0)
			{
				NodeIndex nodeIndex = NodeIndex.invalid;
				float num3 = float.PositiveInfinity;
				for (int i = 0; i < num2; i++)
				{
					int nodeIndex2 = array3[i].nodeIndex;
					if (array6[nodeIndex2] <= num3)
					{
						num3 = array6[nodeIndex2];
						nodeIndex = new NodeIndex(nodeIndex2);
						arrayIndex = i;
					}
				}
				if (nodeIndex.nodeIndex == dest3.nodeIndex)
				{
					ReconstructPath(pathRequest.path, array4, array4[nodeIndex.nodeIndex], pathRequest);
					pathTask.status = PathTask.TaskStatus.Complete;
					pathTask.wasReachable = true;
					return pathTask;
				}
				array2[nodeIndex.nodeIndex] = false;
				ArrayRemoveNodeIndexFromCachedPosition(array3, arrayIndex, num2);
				num2--;
				array[nodeIndex.nodeIndex] = true;
				LinkListIndex linkListIndex = nodes[nodeIndex.nodeIndex].linkListIndex;
				LinkIndex linkIndex = default(LinkIndex);
				linkIndex.linkIndex = linkListIndex.index;
				LinkIndex linkIndex2 = linkIndex;
				linkIndex = default(LinkIndex);
				linkIndex.linkIndex = linkListIndex.index + (int)linkListIndex.size;
				for (LinkIndex linkIndex3 = linkIndex; linkIndex2.linkIndex < linkIndex3.linkIndex; linkIndex2.linkIndex++)
				{
					Link link = links[linkIndex2.linkIndex];
					NodeIndex nodeIndexB = link.nodeIndexB;
					if (array[nodeIndexB.nodeIndex])
					{
						continue;
					}
					if ((num & link.jumpHullMask) != 0 && links[linkIndex2.linkIndex].minJumpHeight > 0f)
					{
						Vector3 position = nodes[link.nodeIndexA.nodeIndex].position;
						Vector3 position2 = nodes[link.nodeIndexB.nodeIndex].position;
						if (Trajectory.CalculateApex(Trajectory.CalculateInitialYSpeed(Trajectory.CalculateGroundTravelTime(pathRequest.maxSpeed, DistanceXZ(position, position2)), position2.y - position.y)) > pathRequest.maxJumpHeight)
						{
							continue;
						}
					}
					if ((link.hullMask & num) != 0 && (link.gateIndex == 0 || openGates[link.gateIndex]))
					{
						float distanceScore = link.distanceScore;
						float num4 = array5[nodeIndex.nodeIndex] + distanceScore;
						if (!array2[nodeIndexB.nodeIndex])
						{
							array2[nodeIndexB.nodeIndex] = true;
							array3[num2] = nodeIndexB;
							num2++;
						}
						else if (num4 >= array5[nodeIndexB.nodeIndex])
						{
							continue;
						}
						array4[nodeIndexB.nodeIndex] = linkIndex2;
						array5[nodeIndexB.nodeIndex] = num4;
						array6[nodeIndexB.nodeIndex] = num4 + HeuristicCostEstimate(nodes[nodeIndexB.nodeIndex].position, nodes[dest3.nodeIndex].position);
					}
				}
			}
			pathRequest.path.Clear();
			pathTask.status = PathTask.TaskStatus.Complete;
			return pathTask;
		}
		catch (Exception ex)
		{
			Debug.LogError($"ComputePath exception:  {ex}\n\n{ex.StackTrace}");
		}
		finally
		{
			floatPerNodePool.Return(array6, FixedSizeArrayPool<float>.ClearType.NONE);
			floatPerNodePool.Return(array5, FixedSizeArrayPool<float>.ClearType.NONE);
			linkPerNodePool.Return(array4, FixedSizeArrayPool<LinkIndex>.ClearType.NONE);
			nodePerNodePool.Return(array3, FixedSizeArrayPool<NodeIndex>.ClearType.CLEAR);
			boolPerNodePool.Return(array2, FixedSizeArrayPool<bool>.ClearType.CLEAR);
			boolPerNodePool.Return(array, FixedSizeArrayPool<bool>.ClearType.CLEAR);
		}
		return null;
		void ResolveNodePosition(in PathRequestPosition pathRequestPosition, out NodeIndex dest, float maxSearchDistance, int maxRaycasts)
		{
			if (pathRequestPosition.nodeIndex.HasValue)
			{
				dest = pathRequestPosition.nodeIndex.Value;
			}
			else if (pathRequestPosition.position.HasValue)
			{
				dest = FindClosestNodeWithRaycast(pathRequestPosition.position.Value, pathRequest.hullClassification, maxSearchDistance, maxRaycasts);
			}
			else
			{
				dest = NodeIndex.invalid;
			}
		}
	}

	private LinkIndex Resolve(LinkIndex[] cameFrom, LinkIndex current)
	{
		if (current.linkIndex < 0 || current.linkIndex > links.Length)
		{
			Debug.LogFormat("Link {0} is out of range [0,{1})", current.linkIndex, links.Length);
		}
		NodeIndex nodeIndexA = links[current.linkIndex].nodeIndexA;
		return cameFrom[nodeIndexA.nodeIndex];
	}

	private void ReconstructPath(Path path, LinkIndex[] cameFrom, LinkIndex current, PathRequest pathRequest)
	{
		int num = 1 << (int)pathRequest.hullClassification;
		path.Clear();
		if (current != LinkIndex.invalid)
		{
			path.PushWaypointToFront(links[current.linkIndex].nodeIndexB, 0f);
		}
		while (current != LinkIndex.invalid)
		{
			NodeIndex nodeIndexB = links[current.linkIndex].nodeIndexB;
			float minJumpHeight = 0f;
			if ((num & links[current.linkIndex].jumpHullMask) != 0 && links[current.linkIndex].minJumpHeight > 0f)
			{
				Vector3 position = nodes[links[current.linkIndex].nodeIndexA.nodeIndex].position;
				Vector3 position2 = nodes[links[current.linkIndex].nodeIndexB.nodeIndex].position;
				minJumpHeight = Trajectory.CalculateApex(Trajectory.CalculateInitialYSpeed(Trajectory.CalculateGroundTravelTime(pathRequest.maxSpeed, DistanceXZ(position, position2)), position2.y - position.y));
			}
			path.PushWaypointToFront(nodeIndexB, minJumpHeight);
			if (cameFrom[links[current.linkIndex].nodeIndexA.nodeIndex] == LinkIndex.invalid)
			{
				path.PushWaypointToFront(links[current.linkIndex].nodeIndexA, 0f);
			}
			current = cameFrom[links[current.linkIndex].nodeIndexA.nodeIndex];
		}
		path.status = PathStatus.Valid;
	}

	private byte RegisterGateName(string gateName)
	{
		if (string.IsNullOrEmpty(gateName))
		{
			return 0;
		}
		int num = gateNames.IndexOf(gateName);
		if (num == -1)
		{
			num = gateNames.Count;
			if (num >= 256)
			{
				Debug.LogErrorFormat(this, "Nodegraph cannot have more than 255 gate names. Nodegraph={0} gateName={1}", this, gateName);
				num = 0;
			}
			else
			{
				gateNames.Add(gateName);
			}
		}
		return (byte)num;
	}

	public bool IsGateOpen(string gateName)
	{
		int num = gateNames.IndexOf(gateName);
		if (num == -1)
		{
			return false;
		}
		return openGates[num];
	}

	public void SetGateState(string gateName, bool open)
	{
		TrySetGateState(gateName, open);
	}

	public bool TrySetGateState(string gateName, bool open)
	{
		int num = gateNames.IndexOf(gateName);
		if (num == -1)
		{
			return false;
		}
		openGates[num] = open;
		return true;
	}
}
