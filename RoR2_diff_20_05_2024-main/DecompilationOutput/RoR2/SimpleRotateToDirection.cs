using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[DefaultExecutionOrder(99999)]
public class SimpleRotateToDirection : MonoBehaviour
{
	public float smoothTime;

	public float maxRotationSpeed;

	private new Transform transform;

	[CanBeNull]
	private NetworkIdentity networkIdentity;

	private float velocity;

	private bool isNetworkControlled;

	public Quaternion targetRotation { get; set; }

	private void Awake()
	{
		transform = base.transform;
		networkIdentity = GetComponent<NetworkIdentity>();
	}

	private void OnEnable()
	{
		targetRotation = transform.rotation;
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
		transform.rotation = Util.SmoothDampQuaternion(transform.rotation, targetRotation, ref velocity, smoothTime, maxRotationSpeed, deltaTime);
	}
}
