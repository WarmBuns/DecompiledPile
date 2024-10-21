using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace RoR2.Navigation;

public class MapNodeGroup : MonoBehaviour
{
	public enum GraphType
	{
		Ground,
		Air,
		Rail
	}

	public NodeGraph nodeGraph;

	public Transform testPointA;

	public Transform testPointB;

	public HullClassification debugHullDef;

	public GraphType graphType;

	public void Clear()
	{
		for (int num = base.transform.childCount - 1; num >= 0; num--)
		{
			Object.DestroyImmediate(base.transform.GetChild(num).gameObject);
		}
	}

	public GameObject AddNode(Vector3 position)
	{
		GameObject obj = new GameObject();
		obj.transform.position = position;
		obj.transform.parent = base.transform;
		obj.AddComponent<MapNode>();
		obj.name = "MapNode";
		return obj;
	}

	public List<MapNode> GetNodes()
	{
		List<MapNode> result = new List<MapNode>();
		GetComponentsInChildren(includeInactive: false, result);
		return result;
	}

	public void UpdateNoCeilingMasks()
	{
		int num = 0;
		foreach (MapNode node in GetNodes())
		{
			node.flags &= ~NodeFlags.NoCeiling;
			if (node.TestNoCeiling())
			{
				num++;
				node.flags |= NodeFlags.NoCeiling;
			}
		}
		Debug.LogFormat("{0} successful ceiling masks baked.", num);
	}

	public void UpdateTeleporterMasks()
	{
		int num = 0;
		foreach (MapNode node in GetNodes())
		{
			node.flags &= ~NodeFlags.TeleporterOK;
			if (node.TestTeleporterOK())
			{
				num++;
				node.flags |= NodeFlags.TeleporterOK;
			}
		}
		Debug.LogFormat("{0} successful teleporter masks baked.", num);
	}

	public void Bake(NodeGraph nodeGraph)
	{
		List<MapNode> nodes = GetNodes();
		ReadOnlyCollection<MapNode> readOnlyCollection = nodes.AsReadOnly();
		for (int i = 0; i < nodes.Count; i++)
		{
			nodes[i].BuildLinks(readOnlyCollection, graphType);
		}
		List<SerializableBitArray> list = new List<SerializableBitArray>();
		for (int j = 0; j < nodes.Count; j++)
		{
			MapNode mapNode = nodes[j];
			SerializableBitArray serializableBitArray = new SerializableBitArray(nodes.Count);
			for (int k = 0; k < nodes.Count; k++)
			{
				MapNode other = nodes[k];
				serializableBitArray[k] = mapNode.TestLineOfSight(other);
			}
			list.Add(serializableBitArray);
		}
		nodeGraph.SetNodes(readOnlyCollection, list.AsReadOnly());
	}
}
