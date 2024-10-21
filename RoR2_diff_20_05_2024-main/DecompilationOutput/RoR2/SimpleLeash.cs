using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[DefaultExecutionOrder(99999)]
public class SimpleLeash : MonoBehaviour
{
	public float minLeashRadius = 1f;

	public float maxLeashRadius = 20f;

	public float maxFollowSpeed = 40f;

	public float smoothTime = 0.15f;

	private new Transform transform;

	[CanBeNull]
	private NetworkIdentity networkIdentity;

	private Vector3 velocity = Vector3.zero;

	private bool isNetworkControlled;

	public Vector3 leashOrigin { get; set; }

	private void Awake()
	{
		transform = base.transform;
		networkIdentity = GetComponent<NetworkIdentity>();
	}

	private void OnEnable()
	{
		leashOrigin = transform.position;
	}

	private void LateUpdate()
	{
		isNetworkControlled = (object)networkIdentity != null && !Util.HasEffectiveAuthority(networkIdentity);
		if (!isNetworkControlled)
		{
			Simulate(Time.deltaTime);
		}
	}

	private void Simulate(float deltaTime)
	{
		Vector3 position = transform.position;
		Vector3 vector = leashOrigin;
		Vector3 vector2 = position - vector;
		float sqrMagnitude = vector2.sqrMagnitude;
		if (sqrMagnitude > minLeashRadius * minLeashRadius)
		{
			float num = Mathf.Sqrt(sqrMagnitude);
			Vector3 vector3 = vector2 / num;
			Vector3 target = vector + vector3 * minLeashRadius;
			Vector3 current = position;
			if (num > maxLeashRadius)
			{
				current = vector + vector3 * maxLeashRadius;
			}
			current = Vector3.SmoothDamp(current, target, ref velocity, smoothTime, maxFollowSpeed, deltaTime);
			if (current != position)
			{
				transform.position = current;
			}
		}
	}
}
