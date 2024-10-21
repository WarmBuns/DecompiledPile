using System.Collections.Generic;
using HG;
using UnityEngine;

namespace RoR2.HudOverlay;

public static class HudOverlayManager
{
	private static Dictionary<GameObject, TargetTracker> targetToTargetTracker = new Dictionary<GameObject, TargetTracker>();

	private static List<OverlayController> globalOverlays = new List<OverlayController>();

	public static OverlayController AddOverlay(GameObject target, OverlayCreationParams overlayCreationParams)
	{
		if (target != null)
		{
			TargetTracker andIncrementTargetTracker = GetAndIncrementTargetTracker(target);
			OverlayController overlayController = new OverlayController(andIncrementTargetTracker, overlayCreationParams);
			andIncrementTargetTracker.AddOverlay(overlayController);
			return overlayController;
		}
		Debug.LogError("AddOverlay can't be called with no target--did you mean to use AddGlobalOverlay?");
		return null;
	}

	public static void RemoveOverlay(OverlayController overlayController)
	{
		TargetTracker owner = overlayController.owner;
		if (owner != null)
		{
			owner.RemoveOverlay(overlayController);
			DecrementTargetTracker(owner);
		}
		else
		{
			Debug.LogError("RemoveOverlay can't be called on an OverlayController with no target--did you mean to use RemoveGlobalOverlay?");
		}
	}

	public static OverlayController AddGlobalOverlay(OverlayCreationParams overlayCreationParams)
	{
		OverlayController overlayController = new OverlayController(null, overlayCreationParams);
		globalOverlays.Add(overlayController);
		return overlayController;
	}

	public static void RemoveGlobalOverlay(OverlayController overlayController)
	{
		if (overlayController.owner == null)
		{
			globalOverlays.Remove(overlayController);
		}
		else
		{
			Debug.LogError("RemoveGlobalOverlay can't be called on an OverlayController with a target--did you mean to use RemoveOverlay?");
		}
	}

	public static void GetGlobalOverlayControllers(List<OverlayController> dest)
	{
		ListUtils.AddRange(dest, globalOverlays);
	}

	private static TargetTracker GetAndIncrementTargetTracker(GameObject target)
	{
		if (!targetToTargetTracker.TryGetValue(target, out var value))
		{
			value = CreateTargetTracker(target);
		}
		value.refCount++;
		return value;
	}

	private static void DecrementTargetTracker(TargetTracker targetTracker)
	{
		targetTracker.refCount--;
		if (targetTracker.refCount <= 0)
		{
			targetToTargetTracker.Remove(targetTracker.target);
			targetTracker.Dispose();
		}
	}

	private static TargetTracker CreateTargetTracker(GameObject target)
	{
		TargetTracker targetTracker = new TargetTracker
		{
			target = target
		};
		targetToTargetTracker.Add(target, targetTracker);
		return targetTracker;
	}

	public static TargetTracker GetTargetTracker(GameObject target)
	{
		if ((object)target != null && targetToTargetTracker.TryGetValue(target, out var value))
		{
			return value;
		}
		return null;
	}
}
