using System.Collections.Generic;
using HG;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(Rigidbody))]
public class AllPlayersTrigger : MonoBehaviour
{
	public UnityEvent onTriggerStart;

	public UnityEvent onTriggerEnd;

	private Queue<Collider> collisionQueueServer;

	private bool triggerActiveServer;

	private void Awake()
	{
		if (NetworkServer.active)
		{
			collisionQueueServer = new Queue<Collider>();
			triggerActiveServer = false;
		}
	}

	private void OnEnable()
	{
		if (!NetworkServer.active)
		{
			base.enabled = false;
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (base.enabled)
		{
			collisionQueueServer.Enqueue(other);
		}
	}

	private void FixedUpdate()
	{
		if (!Run.instance)
		{
			return;
		}
		int num = 0;
		List<CharacterBody> list = CollectionPool<CharacterBody, List<CharacterBody>>.RentCollection();
		while (collisionQueueServer.Count > 0)
		{
			Collider collider = collisionQueueServer.Dequeue();
			if ((bool)collider)
			{
				CharacterBody component = collider.GetComponent<CharacterBody>();
				if ((bool)component && component.isPlayerControlled && !list.Contains(component))
				{
					list.Add(component);
					num++;
				}
			}
		}
		CollectionPool<CharacterBody, List<CharacterBody>>.ReturnCollection(list);
		bool flag = num == Run.instance.livingPlayerCount && num != 0;
		if (triggerActiveServer != flag)
		{
			triggerActiveServer = flag;
			(triggerActiveServer ? onTriggerStart : onTriggerEnd)?.Invoke();
		}
	}
}
