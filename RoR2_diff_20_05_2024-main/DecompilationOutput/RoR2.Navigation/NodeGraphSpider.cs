using System;
using System.Collections;
using System.Collections.Generic;
using HG;
using JetBrains.Annotations;

namespace RoR2.Navigation;

public class NodeGraphSpider
{
	public class StepInfo
	{
		public NodeGraph.NodeIndex node;

		public StepInfo previousStep;
	}

	[NotNull]
	private NodeGraph nodeGraph;

	private List<StepInfo> uncheckedSteps;

	private BitArray visitedNodes;

	public HullMask hullMask;

	[NotNull]
	public List<StepInfo> collectedSteps { get; private set; }

	public NodeGraphSpider([NotNull] NodeGraph nodeGraph, HullMask hullMask)
	{
		if ((object)nodeGraph == null)
		{
			throw new ArgumentNullException("nodeGraph", "'nodeGraph' must be valid.");
		}
		this.nodeGraph = nodeGraph;
		this.hullMask = hullMask;
		collectedSteps = new List<StepInfo>();
		uncheckedSteps = new List<StepInfo>();
		visitedNodes = new BitArray(nodeGraph.GetNodeCount());
	}

	public bool PerformStep()
	{
		List<StepInfo> list = uncheckedSteps;
		uncheckedSteps = new List<StepInfo>();
		List<NodeGraph.LinkIndex> list2 = CollectionPool<NodeGraph.LinkIndex, List<NodeGraph.LinkIndex>>.RentCollection();
		for (int i = 0; i < list.Count; i++)
		{
			StepInfo stepInfo = list[i];
			list2.Clear();
			nodeGraph.GetActiveNodeLinks(stepInfo.node, list2);
			for (int j = 0; j < list2.Count; j++)
			{
				NodeGraph.LinkIndex linkIndex = list2[j];
				if (nodeGraph.IsLinkSuitableForHull(linkIndex, hullMask))
				{
					NodeGraph.NodeIndex linkEndNode = nodeGraph.GetLinkEndNode(linkIndex);
					if (!visitedNodes[linkEndNode.nodeIndex])
					{
						uncheckedSteps.Add(new StepInfo
						{
							node = linkEndNode,
							previousStep = stepInfo
						});
						visitedNodes[linkEndNode.nodeIndex] = true;
					}
				}
			}
			collectedSteps.Add(stepInfo);
		}
		list2 = CollectionPool<NodeGraph.LinkIndex, List<NodeGraph.LinkIndex>>.ReturnCollection(list2);
		return list.Count > 0;
	}

	public void AddNodeForNextStep(NodeGraph.NodeIndex nodeIndex)
	{
		if (!(nodeIndex == NodeGraph.NodeIndex.invalid) && !visitedNodes[nodeIndex.nodeIndex])
		{
			uncheckedSteps.Add(new StepInfo
			{
				node = nodeIndex,
				previousStep = null
			});
			visitedNodes[nodeIndex.nodeIndex] = true;
		}
	}
}
