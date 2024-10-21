using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace RoR2.Navigation;

[ExecuteInEditMode]
public class MapNode : MonoBehaviour
{
	public struct Link
	{
		public MapNode nodeB;

		public float distanceScore;

		public float minJumpHeight;

		public int hullMask;

		public int jumpHullMask;

		public string gateName;
	}

	public class MoveProbe
	{
		public CharacterController testCharacterController;

		private float testTimeStep = 1f / 15f;

		public void Init()
		{
			GameObject gameObject = new GameObject();
			gameObject.name = "NodeGraphProbe";
			_ = gameObject.transform;
			testCharacterController = gameObject.AddComponent<CharacterController>();
			testCharacterController.stepOffset = 0.5f;
			testCharacterController.slopeLimit = 60f;
		}

		public void SetHull(HullClassification hullClassification)
		{
			HullDef hullDef = HullDef.Find(hullClassification);
			testCharacterController.radius = hullDef.radius;
			testCharacterController.height = hullDef.height;
		}

		public void Destroy()
		{
			Object.DestroyImmediate(testCharacterController.gameObject);
		}

		private static float DistanceXZ(Vector3 a, Vector3 b)
		{
			a.y = 0f;
			b.y = 0f;
			return Vector3.Distance(a, b);
		}

		public static Vector3 GetGroundPosition(Vector3 footPosition, float height, float radius)
		{
			Vector3 vector = Vector3.up * (height * 0.5f - radius);
			Vector3 vector2 = Vector3.up * (height * 0.5f);
			Vector3 vector3 = footPosition + vector2;
			float num = radius * 0.5f + 0.005f;
			Vector3 vector4 = footPosition + Vector3.up * num;
			Vector3 vector5 = vector3 + Vector3.up * num;
			if (Physics.CapsuleCast(vector5 + vector, vector5 - vector, radius, Vector3.down, out var hitInfo, num * 2f + 0.005f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
			{
				Vector3 vector6 = hitInfo.distance * Vector3.down;
				return vector4 + vector6;
			}
			return footPosition;
		}

		public Vector3 GetGroundPosition(Vector3 footPosition)
		{
			return GetGroundPosition(footPosition, testCharacterController.height, testCharacterController.radius);
		}

		public bool CapsuleOverlapTest(Vector3 centerOfCapsule)
		{
			Vector3 vector = Vector3.up * (testCharacterController.height * 0.5f - testCharacterController.radius);
			_ = Vector3.up * (testCharacterController.height * 0.5f);
			return Physics.OverlapCapsule(centerOfCapsule + vector, centerOfCapsule - vector, testCharacterController.radius, (int)LayerIndex.world.mask | (int)LayerIndex.CommonMasks.characterBodiesOrDefault, QueryTriggerInteraction.Ignore).Length == 0;
		}

		public bool FlyTest(Vector3 startPos, Vector3 endPos, float flySpeed)
		{
			Vector3 vector = Vector3.up * (testCharacterController.height * 0.5f - testCharacterController.radius);
			return !Physics.CapsuleCast(startPos + vector, startPos - vector, testCharacterController.radius, (endPos - startPos).normalized, (endPos - startPos).magnitude, LayerIndex.world.mask);
		}

		private void MoveCapsule(Vector3 displacement)
		{
			testCharacterController.Move(displacement);
		}

		private void SetCapsulePosition(Vector3 position)
		{
			testCharacterController.transform.position = position;
			Physics.SyncTransforms();
		}

		public bool GroundTest(Vector3 startCenterOfCapsulePos, Vector3 endCenterOfCapsulePos, float hSpeed)
		{
			MoveCapsule(Vector3.zero);
			Vector3 zero = Vector3.zero;
			float num = DistanceXZ(startCenterOfCapsulePos, endCenterOfCapsulePos);
			SetCapsulePosition(startCenterOfCapsulePos + Vector3.up);
			int num2 = Mathf.CeilToInt(num * 1.5f / hSpeed / testTimeStep);
			Vector3 vector = testCharacterController.transform.position;
			for (int i = 0; i < num2; i++)
			{
				Vector3 vector2 = endCenterOfCapsulePos - testCharacterController.transform.position;
				if (vector2.sqrMagnitude <= 0.25f)
				{
					return true;
				}
				Vector3 vector3 = vector2;
				vector3.y = 0f;
				vector3.Normalize();
				zero.x = vector3.x * hSpeed;
				zero.z = vector3.z * hSpeed;
				zero += Physics.gravity * testTimeStep;
				MoveCapsule(zero * testTimeStep);
				Vector3 position = testCharacterController.transform.position;
				if (position == vector)
				{
					return false;
				}
				vector = position;
			}
			return false;
		}

		public float JumpTest(Vector3 startCenterOfCapsulePos, Vector3 endCenterOfCapsulePos, float hSpeed)
		{
			float y = Trajectory.CalculateInitialYSpeed(Trajectory.CalculateGroundTravelTime(hSpeed, DistanceXZ(startCenterOfCapsulePos, endCenterOfCapsulePos)), endCenterOfCapsulePos.y - startCenterOfCapsulePos.y);
			testCharacterController.Move(Vector3.zero);
			Vector3 vector = endCenterOfCapsulePos - startCenterOfCapsulePos;
			vector.y = 0f;
			vector.Normalize();
			vector *= hSpeed;
			vector.y = y;
			float num = DistanceXZ(startCenterOfCapsulePos, endCenterOfCapsulePos);
			SetCapsulePosition(startCenterOfCapsulePos);
			int num2 = Mathf.CeilToInt(num * 1.5f / hSpeed / testTimeStep);
			float num3 = float.NegativeInfinity;
			Vector3 vector2 = testCharacterController.transform.position;
			for (int i = 0; i < num2; i++)
			{
				Vector3 vector3 = endCenterOfCapsulePos - testCharacterController.transform.position;
				if (vector3.sqrMagnitude <= 4f)
				{
					return num3 - startCenterOfCapsulePos.y;
				}
				num3 = Mathf.Max(testCharacterController.transform.position.y, num3);
				Vector3 vector4 = vector3;
				vector4.y = 0f;
				vector4.Normalize();
				vector.x = vector4.x * hSpeed;
				vector.z = vector4.z * hSpeed;
				vector += Physics.gravity * testTimeStep;
				testCharacterController.Move(vector * testTimeStep);
				Vector3 position = testCharacterController.transform.position;
				if (position == vector2)
				{
					return 0f;
				}
				vector2 = position;
			}
			return 0f;
		}
	}

	private static List<MapNode> _instances = new List<MapNode>();

	private static ReadOnlyCollection<MapNode> instancesReadOnly = _instances.AsReadOnly();

	public static readonly float maxConnectionDistance = 15f;

	public List<Link> links = new List<Link>();

	[EnumMask(typeof(HullMask))]
	public HullMask forbiddenHulls;

	[EnumMask(typeof(NodeFlags))]
	public NodeFlags flags;

	[Tooltip("The name of the nodegraph gate associated with this node. If the named gate is closed this node will be treated as though it does not exist.")]
	public string gateName = "";

	public static ReadOnlyCollection<MapNode> instances => instancesReadOnly;

	public void OnEnable()
	{
		_instances.Add(this);
	}

	public void OnDisable()
	{
		_instances.Remove(this);
	}

	private void AddLink(MapNode nodeB, float distanceScore, float minJumpHeight, HullClassification hullClassification)
	{
		int num = links.FindIndex((Link item) => item.nodeB == nodeB);
		if (num == -1)
		{
			links.Add(new Link
			{
				nodeB = nodeB
			});
			num = links.Count - 1;
		}
		Link value = links[num];
		value.distanceScore = Mathf.Max(value.distanceScore, distanceScore);
		value.minJumpHeight = Mathf.Max(value.minJumpHeight, minJumpHeight);
		value.hullMask |= 1 << (int)hullClassification;
		if (minJumpHeight > 0f)
		{
			value.jumpHullMask |= 1 << (int)hullClassification;
		}
		if (string.IsNullOrEmpty(value.gateName))
		{
			value.gateName = nodeB.gateName;
		}
		links[num] = value;
	}

	private void BuildGroundLinks(ReadOnlyCollection<MapNode> nodes, MoveProbe moveProbe)
	{
		Vector3 position = base.transform.position;
		for (int i = 0; i < nodes.Count; i++)
		{
			MapNode mapNode = nodes[i];
			if (mapNode == this)
			{
				continue;
			}
			Vector3 position2 = mapNode.transform.position;
			float num = maxConnectionDistance;
			float num2 = num * num;
			float sqrMagnitude = (position2 - position).sqrMagnitude;
			if (!(sqrMagnitude < num2))
			{
				continue;
			}
			float distanceScore = Mathf.Sqrt(sqrMagnitude);
			for (int j = 0; j < 3; j++)
			{
				moveProbe.SetHull((HullClassification)j);
				if (((uint)forbiddenHulls & (uint)(1 << j)) != 0 || ((uint)mapNode.forbiddenHulls & (uint)(1 << j)) != 0)
				{
					continue;
				}
				Vector3 vector = Vector3.up * (moveProbe.testCharacterController.height * 0.5f);
				Vector3 vector2 = Vector3.up * 0.01f;
				Vector3 vector3 = moveProbe.GetGroundPosition(position) + vector2;
				Vector3 vector4 = moveProbe.GetGroundPosition(position2) + vector2;
				Vector3 vector5 = vector3 + vector;
				Vector3 vector6 = vector4 + vector;
				if (moveProbe.CapsuleOverlapTest(vector5) && moveProbe.CapsuleOverlapTest(vector6))
				{
					bool num3 = moveProbe.GroundTest(vector5, vector6, 6f);
					float num4 = ((!num3) ? moveProbe.JumpTest(vector5, vector6, 7.5f) : 0f);
					if (num3 || (num4 > 0f && num4 < 10f))
					{
						AddLink(mapNode, distanceScore, num4, (HullClassification)j);
					}
				}
			}
		}
	}

	private void BuildAirLinks(ReadOnlyCollection<MapNode> nodes, MoveProbe moveProbe)
	{
		Vector3 position = base.transform.position;
		for (int i = 0; i < nodes.Count; i++)
		{
			MapNode mapNode = nodes[i];
			if (mapNode == this)
			{
				continue;
			}
			Vector3 position2 = mapNode.transform.position;
			float num = maxConnectionDistance * 2f;
			float num2 = num * num;
			float sqrMagnitude = (position2 - position).sqrMagnitude;
			if (!(sqrMagnitude < num2))
			{
				continue;
			}
			float distanceScore = Mathf.Sqrt(sqrMagnitude);
			for (int j = 0; j < 3; j++)
			{
				if (((uint)forbiddenHulls & (uint)(1 << j)) == 0 && ((uint)mapNode.forbiddenHulls & (uint)(1 << j)) == 0)
				{
					moveProbe.SetHull((HullClassification)j);
					Vector3 vector = position;
					Vector3 vector2 = position2;
					if (moveProbe.CapsuleOverlapTest(vector) && moveProbe.CapsuleOverlapTest(vector2) && moveProbe.FlyTest(vector, vector2, 6f))
					{
						AddLink(mapNode, distanceScore, 0f, (HullClassification)j);
					}
				}
			}
		}
	}

	private void BuildRailLinks(ReadOnlyCollection<MapNode> nodes, MoveProbe moveProbe)
	{
		Vector3 position = base.transform.position;
		for (int i = 0; i < nodes.Count; i++)
		{
			MapNode mapNode = nodes[i];
			if (mapNode == this)
			{
				continue;
			}
			Vector3 position2 = mapNode.transform.position;
			float num = maxConnectionDistance * 2f;
			float num2 = num * num;
			float sqrMagnitude = (position2 - position).sqrMagnitude;
			if (!(sqrMagnitude < num2))
			{
				continue;
			}
			float distanceScore = Mathf.Sqrt(sqrMagnitude);
			for (int j = 0; j < 3; j++)
			{
				HullDef hullDef = HullDef.Find((HullClassification)j);
				if (((uint)forbiddenHulls & (uint)(1 << j)) != 0 || ((uint)mapNode.forbiddenHulls & (uint)(1 << j)) != 0)
				{
					continue;
				}
				moveProbe.SetHull((HullClassification)j);
				Vector3 vector = position;
				Vector3 vector2 = position2;
				if (!(Vector3.Angle(Vector3.up, vector2 - vector) <= 50f))
				{
					vector.y += hullDef.height;
					vector2.y += hullDef.height;
					if (moveProbe.CapsuleOverlapTest(vector) && moveProbe.CapsuleOverlapTest(vector2) && moveProbe.FlyTest(vector, vector2, 6f))
					{
						AddLink(mapNode, distanceScore, 0f, (HullClassification)j);
					}
				}
			}
		}
	}

	public void BuildLinks(ReadOnlyCollection<MapNode> nodes, MapNodeGroup.GraphType graphType)
	{
		links.Clear();
		Vector3 position = base.transform.position;
		MoveProbe moveProbe = new MoveProbe();
		moveProbe.Init();
		switch (graphType)
		{
		case MapNodeGroup.GraphType.Ground:
			BuildGroundLinks(nodes, moveProbe);
			break;
		case MapNodeGroup.GraphType.Air:
			BuildAirLinks(nodes, moveProbe);
			break;
		case MapNodeGroup.GraphType.Rail:
			BuildRailLinks(nodes, moveProbe);
			break;
		}
		MapNodeLink[] components = GetComponents<MapNodeLink>();
		foreach (MapNodeLink mapNodeLink in components)
		{
			if (!mapNodeLink.other)
			{
				continue;
			}
			Link link = default(Link);
			link.nodeB = mapNodeLink.other;
			link.distanceScore = Vector3.Distance(position, mapNodeLink.other.transform.position);
			link.minJumpHeight = mapNodeLink.minJumpHeight;
			link.gateName = mapNodeLink.gateName;
			link.hullMask = -1;
			Link link2 = link;
			bool flag = false;
			for (int j = 0; j < links.Count; j++)
			{
				if (links[j].nodeB == mapNodeLink.other)
				{
					links[j] = link2;
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				links.Add(link2);
			}
		}
		moveProbe.Destroy();
	}

	public bool TestLineOfSight(MapNode other)
	{
		return !Physics.Linecast(base.transform.position + Vector3.up, other.transform.position + Vector3.up, LayerIndex.world.mask);
	}

	public bool TestNoCeiling()
	{
		return !Physics.Raycast(new Ray(base.transform.position, Vector3.up), float.PositiveInfinity, LayerIndex.world.mask, QueryTriggerInteraction.Ignore);
	}

	public bool TestTeleporterOK()
	{
		float num = 15f;
		int num2 = 20;
		float num3 = 7f;
		float num4 = 3f;
		float num5 = 360f / (float)num2;
		for (int i = 0; i < num2; i++)
		{
			Vector3 vector = Quaternion.AngleAxis(num5 * (float)i, Vector3.up) * (Vector3.forward * num);
			Vector3 origin = base.transform.position + vector + Vector3.up * num3;
			if (!Physics.Raycast(new Ray(origin, Vector3.down), out var _, num4 + num3, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
			{
				return false;
			}
		}
		Debug.DrawRay(base.transform.position, base.transform.up * 20f, Color.green, 15f);
		return true;
	}
}
