using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using HG;
using RoR2.CharacterAI;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class OutsideInteractableLocker : MonoBehaviour
{
	private struct Candidate
	{
		public PurchaseInteraction purchaseInteraction;

		public float distanceSqr;
	}

	private class LockInfo
	{
		public GameObject lockObj;

		public float distSqr;

		public bool IsLocked()
		{
			return lockObj != null;
		}
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private struct CandidateDistanceCompararer : IComparer<Candidate>
	{
		public int Compare(Candidate a, Candidate b)
		{
			return a.distanceSqr.CompareTo(b.distanceSqr);
		}
	}

	[Tooltip("The networked object which will be instantiated to lock purchasables.")]
	public GameObject lockPrefab;

	[Tooltip("How long to wait between steps.")]
	public float updateInterval = 0.1f;

	[Tooltip("Whether or not to invert the requirements.")]
	public bool lockInside;

	private Dictionary<PurchaseInteraction, GameObject> lockObjectMap;

	private Dictionary<LemurianEggController, LockInfo> eggLockInfoMap;

	private float updateTimer;

	private IEnumerator currentCoroutine;

	public float radius { get; set; }

	private void Awake()
	{
		if (NetworkServer.active)
		{
			lockObjectMap = new Dictionary<PurchaseInteraction, GameObject>();
			if (DevotionInventoryController.isDevotionEnable)
			{
				eggLockInfoMap = new Dictionary<LemurianEggController, LockInfo>();
			}
		}
		else
		{
			base.enabled = false;
		}
	}

	private void OnEnable()
	{
		if (NetworkServer.active)
		{
			currentCoroutine = ChestLockCoroutine();
			updateTimer = updateInterval;
		}
	}

	private void OnDisable()
	{
		if (NetworkServer.active)
		{
			currentCoroutine = null;
			UnlockAll();
		}
	}

	private void FixedUpdate()
	{
		updateTimer -= Time.fixedDeltaTime;
		if (updateTimer <= 0f)
		{
			updateTimer = updateInterval;
			currentCoroutine?.MoveNext();
		}
	}

	private void LockPurchasable(PurchaseInteraction purchaseInteraction)
	{
		if (!purchaseInteraction.lockGameObject)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(lockPrefab, purchaseInteraction.transform.position, Quaternion.identity);
			NetworkServer.Spawn(gameObject);
			purchaseInteraction.NetworklockGameObject = gameObject;
			lockObjectMap.Add(purchaseInteraction, gameObject);
		}
	}

	private void UnlockPurchasable(PurchaseInteraction purchaseInteraction)
	{
		if (lockObjectMap.TryGetValue(purchaseInteraction, out var value) && !(value != purchaseInteraction.lockGameObject))
		{
			UnityEngine.Object.Destroy(value);
			lockObjectMap.Remove(purchaseInteraction);
		}
	}

	private void UnlockAll()
	{
		foreach (GameObject value in lockObjectMap.Values)
		{
			UnityEngine.Object.Destroy(value);
		}
		lockObjectMap.Clear();
		if (eggLockInfoMap == null)
		{
			return;
		}
		foreach (KeyValuePair<LemurianEggController, LockInfo> item in eggLockInfoMap)
		{
			UnlockLemurianEgg(item.Key);
		}
		eggLockInfoMap.Clear();
	}

	private void LockLemurianEgg(LemurianEggController egg)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(lockPrefab, egg.transform.position, Quaternion.identity);
		NetworkServer.Spawn(gameObject);
		eggLockInfoMap[egg].lockObj = gameObject;
		egg.SetInteractability(interactable: false);
	}

	private void UnlockLemurianEgg(LemurianEggController egg)
	{
		LockInfo lockInfo = eggLockInfoMap[egg];
		if ((bool)lockInfo.lockObj)
		{
			NetworkServer.Destroy(lockInfo.lockObj);
			lockInfo.lockObj = null;
		}
		egg.SetInteractability(interactable: true);
	}

	private IEnumerator ChestLockCoroutine()
	{
		Candidate[] candidates = new Candidate[64];
		int candidatesCount = 0;
		if (DevotionInventoryController.isDevotionEnable)
		{
			foreach (LemurianEggController instances in InstanceTracker.GetInstancesList<LemurianEggController>())
			{
				LockInfo lockInfo = new LockInfo();
				lockInfo.lockObj = null;
				lockInfo.distSqr = (instances.transform.position - base.transform.position).sqrMagnitude;
				eggLockInfoMap[instances] = lockInfo;
			}
		}
		while (true)
		{
			Vector3 position = base.transform.position;
			if (DevotionInventoryController.isDevotionEnable)
			{
				foreach (KeyValuePair<LemurianEggController, LockInfo> item in eggLockInfoMap)
				{
					float num = radius * radius;
					LockInfo value = item.Value;
					bool flag = value.distSqr <= num;
					value.IsLocked();
					if (flag != lockInside)
					{
						UnlockLemurianEgg(item.Key);
					}
					else
					{
						LockLemurianEgg(item.Key);
					}
				}
			}
			List<PurchaseInteraction> instancesList = InstanceTracker.GetInstancesList<PurchaseInteraction>();
			int num2 = candidatesCount;
			candidatesCount = instancesList.Count;
			ArrayUtils.EnsureCapacity(ref candidates, candidatesCount);
			for (int i = num2; i < candidatesCount; i++)
			{
				candidates[i] = default(Candidate);
			}
			for (int j = 0; j < candidatesCount; j++)
			{
				PurchaseInteraction purchaseInteraction = instancesList[j];
				candidates[j] = new Candidate
				{
					purchaseInteraction = purchaseInteraction,
					distanceSqr = (purchaseInteraction.transform.position - position).sqrMagnitude
				};
			}
			yield return null;
			Array.Sort(candidates, 0, candidatesCount, default(CandidateDistanceCompararer));
			yield return null;
			int i2 = 0;
			while (i2 < candidatesCount)
			{
				PurchaseInteraction purchaseInteraction2 = candidates[i2].purchaseInteraction;
				if ((bool)purchaseInteraction2)
				{
					float num3 = radius * radius;
					if (candidates[i2].distanceSqr <= num3 != lockInside || !purchaseInteraction2.available)
					{
						UnlockPurchasable(purchaseInteraction2);
					}
					else
					{
						LockPurchasable(purchaseInteraction2);
					}
					yield return null;
				}
				int num4 = i2 + 1;
				i2 = num4;
			}
			yield return null;
		}
	}
}
