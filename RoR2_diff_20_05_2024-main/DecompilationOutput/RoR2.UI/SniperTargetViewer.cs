using System;
using System.Collections.Generic;
using HG;
using UnityEngine;

namespace RoR2.UI;

[RequireComponent(typeof(PointViewer))]
public class SniperTargetViewer : MonoBehaviour
{
	public GameObject visualizerPrefab;

	private PointViewer pointViewer;

	private HUD hud;

	private Dictionary<UnityObjectWrapperKey<HurtBox>, GameObject> hurtBoxToVisualizer = new Dictionary<UnityObjectWrapperKey<HurtBox>, GameObject>();

	private List<HurtBox> displayedTargets = new List<HurtBox>();

	private List<HurtBox> previousDisplayedTargets = new List<HurtBox>();

	private void Awake()
	{
		pointViewer = GetComponent<PointViewer>();
		OnTransformParentChanged();
	}

	private void OnTransformParentChanged()
	{
		hud = GetComponentInParent<HUD>();
	}

	private void OnDisable()
	{
		SetDisplayedTargets(Array.Empty<HurtBox>());
		hurtBoxToVisualizer.Clear();
	}

	private void Update()
	{
		List<HurtBox> list = CollectionPool<HurtBox, List<HurtBox>>.RentCollection();
		if ((bool)hud && (bool)hud.targetMaster)
		{
			TeamIndex teamIndex = hud.targetMaster.teamIndex;
			IReadOnlyList<HurtBox> readOnlySniperTargetsList = HurtBox.readOnlySniperTargetsList;
			int i = 0;
			for (int count = readOnlySniperTargetsList.Count; i < count; i++)
			{
				HurtBox hurtBox = readOnlySniperTargetsList[i];
				if ((bool)hurtBox.healthComponent && hurtBox.healthComponent.alive && FriendlyFireManager.ShouldDirectHitProceed(hurtBox.healthComponent, teamIndex) && (object)hurtBox.healthComponent.body != hud.targetMaster.GetBody())
				{
					list.Add(hurtBox);
				}
			}
		}
		SetDisplayedTargets(list);
		list = CollectionPool<HurtBox, List<HurtBox>>.ReturnCollection(list);
	}

	private void OnTargetDiscovered(HurtBox hurtBox)
	{
		if (!hurtBoxToVisualizer.ContainsKey(hurtBox))
		{
			GameObject value = pointViewer.AddElement(new PointViewer.AddElementRequest
			{
				elementPrefab = visualizerPrefab,
				target = hurtBox.transform,
				targetWorldVerticalOffset = 0f,
				targetWorldRadius = HurtBox.sniperTargetRadius,
				scaleWithDistance = true
			});
			hurtBoxToVisualizer.Add(hurtBox, value);
		}
		else
		{
			Debug.LogWarning($"Already discovered hurtbox: {hurtBox}");
		}
	}

	private void OnTargetLost(HurtBox hurtBox)
	{
		if (hurtBoxToVisualizer.TryGetValue(hurtBox, out var value))
		{
			pointViewer.RemoveElement(value);
		}
	}

	private void SetDisplayedTargets(IReadOnlyList<HurtBox> newDisplayedTargets)
	{
		Util.Swap(ref displayedTargets, ref previousDisplayedTargets);
		displayedTargets.Clear();
		ListUtils.AddRange(displayedTargets, newDisplayedTargets);
		List<HurtBox> list = CollectionPool<HurtBox, List<HurtBox>>.RentCollection();
		List<HurtBox> list2 = CollectionPool<HurtBox, List<HurtBox>>.RentCollection();
		ListUtils.FindExclusiveEntriesByReference(displayedTargets, previousDisplayedTargets, list, list2);
		foreach (HurtBox item in list2)
		{
			OnTargetLost(item);
		}
		foreach (HurtBox item2 in list)
		{
			OnTargetDiscovered(item2);
		}
		list2 = CollectionPool<HurtBox, List<HurtBox>>.ReturnCollection(list2);
		list = CollectionPool<HurtBox, List<HurtBox>>.ReturnCollection(list);
	}
}
