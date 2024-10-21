using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoR2;

public class RoachController : MonoBehaviour
{
	[Serializable]
	public struct KeyFrame
	{
		public float time;

		public Vector3 position;

		public Quaternion rotation;
	}

	[Serializable]
	public class Roach
	{
		public KeyFrame[] keyFrames;

		private int frameIndex = 1;

		public KeyFrame lowFrame => keyFrames[frameIndex - 1];

		public KeyFrame highFrame => keyFrames[frameIndex];

		public bool CanBeUpdated(float timeSince)
		{
			return frameIndex < keyFrames.Length - 1;
		}

		public void UpdateRoach(RoachController controller, int roachIndex, float timeSince)
		{
			float num = Mathf.Min(timeSince, keyFrames[keyFrames.Length - 1].time);
			if (highFrame.time <= num)
			{
				Iterate();
			}
			float t = Mathf.InverseLerp(lowFrame.time, highFrame.time, num);
			controller.SetRoachPosition(roachIndex, Vector3.Lerp(lowFrame.position, highFrame.position, t), Quaternion.Slerp(lowFrame.rotation, highFrame.rotation, t));
		}

		public void Iterate()
		{
			if (frameIndex < keyFrames.Length - 1)
			{
				frameIndex++;
			}
		}
	}

	private class SimulatedRoach : IDisposable
	{
		private struct RaycastResult
		{
			public bool didHit;

			public Vector3 point;

			public Vector3 normal;

			public float distance;
		}

		private Vector3 initialFleeNormal;

		private Vector3 desiredMovement;

		private RoachParams roachParams;

		private float reorientTimer;

		private float backupTimer;

		private Vector3 velocity = Vector3.zero;

		private float currentSpeed;

		private float turnVelocity;

		private Vector3 groundNormal;

		private float simulationDuration;

		public Transform transform { get; private set; }

		public float age { get; private set; }

		public bool finished { get; private set; }

		private bool onGround => groundNormal != Vector3.zero;

		public SimulatedRoach(Vector3 position, Vector3 groundNormal, Vector3 initialFleeNormal, RoachParams roachParams)
		{
			this.roachParams = roachParams;
			GameObject gameObject = new GameObject("SimulatedRoach");
			transform = gameObject.transform;
			transform.position = position;
			transform.up = groundNormal;
			transform.Rotate(transform.up, UnityEngine.Random.Range(0f, 360f));
			transform.forward = UnityEngine.Random.onUnitSphere;
			this.groundNormal = groundNormal;
			this.initialFleeNormal = initialFleeNormal;
			desiredMovement = UnityEngine.Random.onUnitSphere;
			age = UnityEngine.Random.Range(roachParams.minReactionTime, roachParams.maxReactionTime);
			simulationDuration = age + UnityEngine.Random.Range(roachParams.minSimulationDuration, roachParams.maxSimulationDuration);
		}

		private void SetUpVector(Vector3 desiredUp)
		{
			Vector3 right = transform.right;
			Vector3 up = transform.up;
			transform.Rotate(right, Vector3.SignedAngle(up, desiredUp, right), Space.World);
		}

		public void Simulate(float deltaTime)
		{
			age += deltaTime;
			if (onGround)
			{
				SetUpVector(groundNormal);
				turnVelocity += UnityEngine.Random.Range(0f - roachParams.wiggle, roachParams.wiggle) * deltaTime;
				TurnDesiredMovement(turnVelocity * deltaTime);
				Vector3 up = transform.up;
				Vector3 normalized = Vector3.ProjectOnPlane(desiredMovement, up).normalized;
				float value = Vector3.SignedAngle(transform.forward, normalized, up);
				TurnBody(Mathf.Clamp(value, (0f - turnVelocity) * deltaTime, turnVelocity * deltaTime));
				currentSpeed = Mathf.MoveTowards(currentSpeed, roachParams.maxSpeed, deltaTime * roachParams.acceleration);
				StepGround(currentSpeed * deltaTime);
			}
			else
			{
				velocity += Physics.gravity * deltaTime;
				StepAir(velocity);
			}
			reorientTimer -= deltaTime;
			if (reorientTimer <= 0f)
			{
				desiredMovement = initialFleeNormal;
				reorientTimer = UnityEngine.Random.Range(roachParams.reorientTimerMin, roachParams.reorientTimerMax);
			}
			if (age >= simulationDuration)
			{
				finished = true;
			}
		}

		private void OnBump()
		{
			TurnDesiredMovement(UnityEngine.Random.Range(-90f, 90f));
			currentSpeed *= -0.5f;
			if (roachParams.chanceToFinishOnBump < UnityEngine.Random.value)
			{
				finished = true;
			}
		}

		private void TurnDesiredMovement(float degrees)
		{
			Quaternion quaternion = Quaternion.AngleAxis(degrees, transform.up);
			desiredMovement = quaternion * desiredMovement;
		}

		private void TurnBody(float degrees)
		{
			transform.Rotate(Vector3.up, degrees, Space.Self);
		}

		private void StepAir(Vector3 movement)
		{
			RaycastResult raycastResult = SimpleRaycast(new Ray(transform.position, movement), movement.magnitude);
			Debug.DrawLine(transform.position, raycastResult.point, Color.magenta, 10f, depthTest: false);
			if (raycastResult.didHit)
			{
				groundNormal = raycastResult.normal;
				velocity = Vector3.zero;
			}
			transform.position = raycastResult.point;
		}

		private void StepGround(float distance)
		{
			groundNormal = Vector3.zero;
			Vector3 up = transform.up;
			Vector3 forward = transform.forward;
			float stepSize = roachParams.stepSize;
			Vector3 vector = up * stepSize;
			Vector3 position = transform.position;
			position += vector;
			Debug.DrawLine(transform.position, position, Color.red, 10f, depthTest: false);
			RaycastResult raycastResult = SimpleRaycast(new Ray(position, forward), distance);
			Debug.DrawLine(position, raycastResult.point, Color.green, 10f, depthTest: false);
			position = raycastResult.point;
			if (raycastResult.didHit)
			{
				if (Vector3.Dot(raycastResult.normal, forward) < -0.5f)
				{
					OnBump();
				}
				groundNormal = raycastResult.normal;
			}
			else
			{
				RaycastResult raycastResult2 = SimpleRaycast(new Ray(position, -vector), stepSize * 2f);
				if (raycastResult2.didHit)
				{
					Debug.DrawLine(position, raycastResult2.point, Color.blue, 10f, depthTest: false);
					position = raycastResult2.point;
					groundNormal = raycastResult2.normal;
				}
				else
				{
					Debug.DrawLine(position, position - vector, Color.white, 10f);
					position -= vector;
				}
			}
			if (groundNormal == Vector3.zero)
			{
				currentSpeed = 0f;
			}
			transform.position = position;
		}

		private static RaycastResult SimpleRaycast(Ray ray, float maxDistance)
		{
			RaycastHit hitInfo;
			bool flag = Physics.Raycast(ray, out hitInfo, maxDistance, LayerIndex.world.mask, QueryTriggerInteraction.Ignore);
			RaycastResult result = default(RaycastResult);
			result.didHit = flag;
			result.point = (flag ? hitInfo.point : ray.GetPoint(maxDistance));
			result.normal = (flag ? hitInfo.normal : Vector3.zero);
			result.distance = (flag ? hitInfo.distance : maxDistance);
			return result;
		}

		public void Dispose()
		{
			UnityEngine.Object.DestroyImmediate(transform.gameObject);
			transform = null;
		}
	}

	public class RoachPathEditorComponent : MonoBehaviour
	{
		public RoachController roachController;

		public int nodeCount => base.transform.childCount;

		public RoachNodeEditorComponent AddNode()
		{
			GameObject obj = new GameObject("Roach Path Node (Temporary)");
			obj.hideFlags = HideFlags.DontSave;
			obj.transform.SetParent(base.transform);
			RoachNodeEditorComponent roachNodeEditorComponent = obj.AddComponent<RoachNodeEditorComponent>();
			roachNodeEditorComponent.path = this;
			return roachNodeEditorComponent;
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.white;
			for (int i = 0; i + 1 < nodeCount; i++)
			{
				Vector3 position = base.transform.GetChild(i).transform.position;
				Vector3 position2 = base.transform.GetChild(i + 1).transform.position;
				Gizmos.DrawLine(position, position2);
			}
		}
	}

	public class RoachNodeEditorComponent : MonoBehaviour
	{
		public RoachPathEditorComponent path;

		public void FacePosition(Vector3 position)
		{
			Vector3 position2 = base.transform.position;
			Vector3 up = base.transform.up;
			Quaternion rotation = Quaternion.LookRotation(position - position2, up);
			base.transform.rotation = rotation;
			base.transform.up = up;
		}
	}

	public RoachParams roachParams;

	public int roachCount;

	public float placementSpreadMin = 1f;

	public float placementSpreadMax = 25f;

	public float placementMaxDistance = 10f;

	public Roach[] roaches;

	private Transform[] roachTransforms;

	private bool scattered;

	private Run.TimeStamp scatterStartTime = Run.TimeStamp.positiveInfinity;

	private const string roachScatterSoundString = "Play_env_roach_scatter";

	private void Awake()
	{
		roachTransforms = new Transform[roaches.Length];
		for (int i = 0; i < roachTransforms.Length; i++)
		{
			roachTransforms[i] = UnityEngine.Object.Instantiate(roachParams.roachPrefab, roaches[i].keyFrames[0].position, roaches[i].keyFrames[0].rotation).transform;
		}
	}

	private void OnDestroy()
	{
		for (int i = 0; i < roachTransforms.Length; i++)
		{
			if ((bool)roachTransforms[i])
			{
				UnityEngine.Object.Destroy(roachTransforms[i].gameObject);
			}
		}
	}

	public void BakeRoaches2()
	{
		List<Roach> list = new List<Roach>();
		for (int i = 0; i < roachCount; i++)
		{
			Ray ray = new Ray(base.transform.position, Util.ApplySpread(base.transform.forward, placementSpreadMin, placementSpreadMax, 1f, 1f));
			if (Physics.Raycast(ray, out var hitInfo, placementMaxDistance, LayerIndex.world.mask))
			{
				SimulatedRoach simulatedRoach = new SimulatedRoach(hitInfo.point + hitInfo.normal * 0.01f, hitInfo.normal, ray.direction, roachParams);
				float keyframeInterval = roachParams.keyframeInterval;
				List<KeyFrame> list2 = new List<KeyFrame>();
				while (!simulatedRoach.finished)
				{
					simulatedRoach.Simulate(keyframeInterval);
					list2.Add(new KeyFrame
					{
						position = simulatedRoach.transform.position,
						rotation = simulatedRoach.transform.rotation,
						time = simulatedRoach.age
					});
				}
				KeyFrame value = list2[list2.Count - 1];
				value.position += value.rotation * (Vector3.down * 0.25f);
				list2[list2.Count - 1] = value;
				simulatedRoach.Dispose();
				list.Add(new Roach
				{
					keyFrames = list2.ToArray()
				});
			}
		}
		roaches = list.ToArray();
	}

	public void BakeRoaches()
	{
		BakeRoaches2();
	}

	private void ClearRoachPathEditors()
	{
		for (int num = base.transform.childCount - 1; num > 0; num--)
		{
			UnityEngine.Object.DestroyImmediate(base.transform.GetChild(num).gameObject);
		}
	}

	public void DebakeRoaches()
	{
		ClearRoachPathEditors();
		for (int i = 0; i < roaches.Length; i++)
		{
			Roach roach = roaches[i];
			RoachPathEditorComponent roachPathEditorComponent = AddPathEditorObject();
			for (int j = 0; j < roach.keyFrames.Length; j++)
			{
				KeyFrame keyFrame = roach.keyFrames[j];
				RoachNodeEditorComponent roachNodeEditorComponent = roachPathEditorComponent.AddNode();
				roachNodeEditorComponent.transform.position = keyFrame.position;
				roachNodeEditorComponent.transform.rotation = keyFrame.rotation;
			}
		}
	}

	public RoachPathEditorComponent AddPathEditorObject()
	{
		GameObject obj = new GameObject("Roach Path (Temporary)");
		obj.hideFlags = HideFlags.DontSave;
		obj.transform.SetParent(base.transform, worldPositionStays: false);
		RoachPathEditorComponent roachPathEditorComponent = obj.AddComponent<RoachPathEditorComponent>();
		roachPathEditorComponent.roachController = this;
		return roachPathEditorComponent;
	}

	private void SetRoachPosition(int i, Vector3 position, Quaternion rotation)
	{
		roachTransforms[i].SetPositionAndRotation(position, rotation);
	}

	private void Update()
	{
		if (!scattered)
		{
			return;
		}
		int num = 0;
		for (int i = 0; i < roaches.Length; i++)
		{
			if (roaches[i].CanBeUpdated(scatterStartTime.timeSince))
			{
				roaches[i].UpdateRoach(this, i, scatterStartTime.timeSince);
				num++;
			}
		}
		if (num == 0)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private void Scatter()
	{
		if (!scattered)
		{
			Util.PlaySound("Play_env_roach_scatter", base.gameObject);
			scattered = true;
			scatterStartTime = Run.TimeStamp.now;
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		CharacterBody component = other.GetComponent<CharacterBody>();
		if ((object)component != null && component.isPlayerControlled)
		{
			Scatter();
		}
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.yellow;
		Gizmos.matrix = Matrix4x4.TRS(base.transform.position, base.transform.rotation, Vector3.one);
		Gizmos.DrawFrustum(Vector3.zero, placementSpreadMax * 0.5f, placementMaxDistance, 0f, 1f);
	}
}
