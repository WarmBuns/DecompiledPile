using System;
using UnityEngine;

namespace RoR2;

public class ProxyInteraction : MonoBehaviour, IInteractable
{
	public Func<ProxyInteraction, Interactor, string> getContextString;

	public Func<ProxyInteraction, Interactor, Interactability> getInteractability;

	public Action<ProxyInteraction, Interactor> onInteractionBegin;

	public Func<ProxyInteraction, Interactor, bool> shouldIgnoreSpherecastForInteractability;

	public Func<ProxyInteraction, bool> shouldShowOnScanner;

	public string GetContextString(Interactor activator)
	{
		return getContextString?.Invoke(this, activator);
	}

	public Interactability GetInteractability(Interactor activator)
	{
		return getInteractability?.Invoke(this, activator) ?? Interactability.Disabled;
	}

	public void OnInteractionBegin(Interactor activator)
	{
		onInteractionBegin?.Invoke(this, activator);
	}

	public bool ShouldIgnoreSpherecastForInteractibility(Interactor activator)
	{
		return shouldIgnoreSpherecastForInteractability?.Invoke(this, activator) ?? true;
	}

	public bool ShouldShowOnScanner()
	{
		return shouldShowOnScanner?.Invoke(this) ?? false;
	}

	public bool ShouldProximityHighlight()
	{
		return false;
	}

	private void OnEnable()
	{
		InstanceTracker.Add(this);
	}

	private void OnDisable()
	{
		InstanceTracker.Remove(this);
	}
}
