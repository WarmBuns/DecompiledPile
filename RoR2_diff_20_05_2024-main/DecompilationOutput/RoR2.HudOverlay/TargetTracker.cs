using System;
using System.Collections.Generic;
using HG;
using UnityEngine;

namespace RoR2.HudOverlay;

public class TargetTracker : IDisposable
{
	public GameObject target;

	public int refCount;

	private List<OverlayController> overlayControllers;

	private bool disposed;

	public TargetTracker()
	{
		overlayControllers = CollectionPool<OverlayController, List<OverlayController>>.RentCollection();
	}

	public void Dispose()
	{
		if (!disposed)
		{
			disposed = true;
			while (overlayControllers.Count > 0)
			{
				RemoveOverlayAt(overlayControllers.Count - 1);
			}
			overlayControllers = CollectionPool<OverlayController, List<OverlayController>>.ReturnCollection(overlayControllers);
		}
	}

	public void AddOverlay(OverlayController overlayController)
	{
		overlayControllers.Add(overlayController);
	}

	public void RemoveOverlay(OverlayController overlayController)
	{
		int i = overlayControllers.IndexOf(overlayController);
		RemoveOverlayAt(i);
	}

	private void RemoveOverlayAt(int i)
	{
		overlayControllers.RemoveAt(i);
	}

	public void GetOverlayControllers(List<OverlayController> dest)
	{
		ListUtils.AddRange(dest, overlayControllers);
	}
}
