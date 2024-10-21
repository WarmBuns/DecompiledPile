using System;
using System.Collections.Generic;
using HG;
using UnityEngine;
using UnityEngine.Events;

namespace RoR2;

public class MultiBodyTrigger : MonoBehaviour
{
	[Header("Parameters")]
	public bool playerControlledOnly = true;

	[Header("Events")]
	public CharacterBody.CharacterBodyUnityEvent onFirstQualifyingBodyEnter;

	public CharacterBody.CharacterBodyUnityEvent onLastQualifyingBodyExit;

	public CharacterBody.CharacterBodyUnityEvent onAnyQualifyingBodyEnter;

	public CharacterBody.CharacterBodyUnityEvent onAnyQualifyingBodyExit;

	public UnityEvent onAllQualifyingBodiesEnter;

	public UnityEvent onAllQualifyingBodiesExit;

	private readonly Queue<Collider> collisionsQueue = new Queue<Collider>();

	private readonly List<CharacterBody> encounteredBodies = new List<CharacterBody>();

	private bool allCandidatesPreviouslyTriggering;

	private bool cachedEnabled;

	private void OnEnable()
	{
		cachedEnabled = true;
	}

	private void OnDisable()
	{
		cachedEnabled = false;
		collisionsQueue.Clear();
		List<CharacterBody> list = CollectionPool<CharacterBody, List<CharacterBody>>.RentCollection();
		SetEncounteredBodies(list, 0);
		list = CollectionPool<CharacterBody, List<CharacterBody>>.ReturnCollection(list);
	}

	private void OnTriggerStay(Collider collider)
	{
		if (cachedEnabled)
		{
			collisionsQueue.Enqueue(collider);
		}
	}

	private void FixedUpdate()
	{
		if (collisionsQueue.Count == 0 && encounteredBodies.Count == 0)
		{
			return;
		}
		List<CharacterBody> list = CollectionPool<CharacterBody, List<CharacterBody>>.RentCollection();
		while (collisionsQueue.Count > 0)
		{
			Collider collider = collisionsQueue.Dequeue();
			if ((bool)collider)
			{
				CharacterBody value = collider.GetComponent<CharacterBody>();
				if ((bool)value && (!playerControlledOnly || value.isPlayerControlled) && ListUtils.FirstOccurrenceByReference(list, in value) == -1)
				{
					list.Add(value);
				}
			}
		}
		SetEncounteredBodies(list, playerControlledOnly ? (Run.instance?.livingPlayerCount ?? 0) : 0);
		list = CollectionPool<CharacterBody, List<CharacterBody>>.ReturnCollection(list);
	}

	private void SetEncounteredBodies(List<CharacterBody> newEncounteredBodies, int candidateCount)
	{
		List<CharacterBody> list = CollectionPool<CharacterBody, List<CharacterBody>>.RentCollection();
		List<CharacterBody> list2 = CollectionPool<CharacterBody, List<CharacterBody>>.RentCollection();
		ListUtils.FindExclusiveEntriesByReference(encounteredBodies, newEncounteredBodies, list2, list);
		bool flag = encounteredBodies.Count == 0;
		bool flag2 = newEncounteredBodies.Count == 0;
		ListUtils.CloneTo(newEncounteredBodies, encounteredBodies);
		foreach (CharacterBody item in list2)
		{
			try
			{
				onAnyQualifyingBodyExit?.Invoke(item);
			}
			catch (Exception message)
			{
				Debug.LogError(message);
			}
		}
		if (flag != flag2)
		{
			if (flag2)
			{
				try
				{
					onLastQualifyingBodyExit?.Invoke(list2[list2.Count - 1]);
				}
				catch (Exception message2)
				{
					Debug.LogError(message2);
				}
			}
			else
			{
				try
				{
					onFirstQualifyingBodyEnter?.Invoke(list[0]);
				}
				catch (Exception message3)
				{
					Debug.LogError(message3);
				}
			}
		}
		foreach (CharacterBody item2 in list)
		{
			try
			{
				onAnyQualifyingBodyEnter?.Invoke(item2);
			}
			catch (Exception message4)
			{
				Debug.LogError(message4);
			}
		}
		list2 = CollectionPool<CharacterBody, List<CharacterBody>>.ReturnCollection(list2);
		list = CollectionPool<CharacterBody, List<CharacterBody>>.ReturnCollection(list);
		bool flag3 = encounteredBodies.Count >= candidateCount && candidateCount > 0;
		if (allCandidatesPreviouslyTriggering == flag3)
		{
			return;
		}
		try
		{
			if (flag3)
			{
				onAllQualifyingBodiesEnter?.Invoke();
			}
			else
			{
				onAllQualifyingBodiesExit?.Invoke();
			}
		}
		catch (Exception message5)
		{
			Debug.LogError(message5);
		}
		allCandidatesPreviouslyTriggering = flag3;
	}

	public void KillAllOutsideWithVoidDeath()
	{
		List<CharacterBody> list = CollectionPool<CharacterBody, List<CharacterBody>>.RentCollection();
		ListUtils.AddRange(list, CharacterBody.readOnlyInstancesList);
		foreach (CharacterBody item in list)
		{
			if (encounteredBodies.Contains(item))
			{
				continue;
			}
			CharacterMaster master = item.master;
			if ((bool)master)
			{
				try
				{
					master.TrueKill(BodyCatalog.FindBodyPrefab("BrotherBody"), null, DamageType.VoidDeath);
				}
				catch (Exception message)
				{
					Debug.LogError(message);
				}
			}
		}
		list = CollectionPool<CharacterBody, List<CharacterBody>>.ReturnCollection(list);
	}
}
