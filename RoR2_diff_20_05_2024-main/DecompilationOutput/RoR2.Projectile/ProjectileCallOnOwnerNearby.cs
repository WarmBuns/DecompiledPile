using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace RoR2.Projectile;

[RequireComponent(typeof(ProjectileController))]
public class ProjectileCallOnOwnerNearby : MonoBehaviour
{
	[Flags]
	public enum Filter
	{
		None = 0,
		Server = 1,
		Client = 2
	}

	private enum State
	{
		Outside,
		Inside
	}

	public Filter filter;

	public float radius;

	public UnityEvent onOwnerEnter;

	public UnityEvent onOwnerExit;

	private State state;

	private bool ownerInRadius;

	private ProjectileController projectileController;

	private void Awake()
	{
		projectileController = GetComponent<ProjectileController>();
		Filter filter = Filter.None;
		if (NetworkServer.active)
		{
			filter |= Filter.Server;
		}
		if (NetworkClient.active)
		{
			filter |= Filter.Client;
		}
		if ((this.filter & filter) == 0)
		{
			base.enabled = false;
		}
	}

	private void OnDisable()
	{
		SetState(State.Outside);
	}

	private void SetState(State newState)
	{
		if (state != newState)
		{
			state = newState;
			if (state == State.Inside)
			{
				onOwnerEnter?.Invoke();
			}
			else
			{
				onOwnerExit?.Invoke();
			}
		}
	}

	private void FixedUpdate()
	{
		State state = State.Outside;
		if ((bool)projectileController.owner)
		{
			float num = radius * radius;
			if ((base.transform.position - projectileController.owner.transform.position).sqrMagnitude < num)
			{
				state = State.Inside;
			}
		}
		SetState(state);
	}
}
