using UnityEngine;

namespace EntityStates.Engi.SpiderMine;

public class ChaseTarget : BaseSpiderMineState
{
	private class OrientationHelper : MonoBehaviour
	{
		private Rigidbody rigidbody;

		private void Awake()
		{
			rigidbody = GetComponent<Rigidbody>();
		}

		private void OnCollisionStay(Collision collision)
		{
			int contactCount = collision.contactCount;
			if (contactCount == 0)
			{
				return;
			}
			Vector3 forward = collision.GetContact(0).normal;
			for (int i = 1; i < contactCount; i++)
			{
				Vector3 normal = collision.GetContact(i).normal;
				if (forward.y < normal.y)
				{
					forward = normal;
				}
			}
			rigidbody.MoveRotation(Quaternion.LookRotation(forward));
		}
	}

	public static float speed;

	public static float triggerRadius;

	private bool passedDetonationRadius;

	private float bestDistance;

	private OrientationHelper orientationHelper;

	private Transform target => base.projectileTargetComponent.target;

	protected override bool shouldStick => false;

	public override void OnEnter()
	{
		base.OnEnter();
		passedDetonationRadius = false;
		bestDistance = float.PositiveInfinity;
		PlayAnimation("Base", BaseSpiderMineState.ChaseStateHash);
		if (base.isAuthority)
		{
			orientationHelper = base.gameObject.AddComponent<OrientationHelper>();
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!base.isAuthority)
		{
			return;
		}
		if (!target)
		{
			base.rigidbody.AddForce(Vector3.up, ForceMode.VelocityChange);
			outer.SetNextState(new WaitForStick());
			return;
		}
		Vector3 position = target.position;
		Vector3 position2 = base.transform.position;
		Vector3 vector = position - position2;
		float magnitude = vector.magnitude;
		float y = base.rigidbody.velocity.y;
		Vector3 velocity = vector * (speed / magnitude);
		velocity.y = y;
		base.rigidbody.velocity = velocity;
		if (!passedDetonationRadius && magnitude <= triggerRadius)
		{
			passedDetonationRadius = true;
		}
		if (magnitude < bestDistance)
		{
			bestDistance = magnitude;
		}
		else if (passedDetonationRadius)
		{
			outer.SetNextState(new PreDetonate());
		}
	}

	public override void OnExit()
	{
		Transform transform = FindModelChild(childLocatorStringToEnable);
		if ((bool)transform)
		{
			transform.gameObject.SetActive(value: false);
		}
		if ((object)orientationHelper != null)
		{
			EntityState.Destroy(orientationHelper);
			orientationHelper = null;
		}
		base.OnExit();
	}
}
