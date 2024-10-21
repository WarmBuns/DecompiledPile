using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(NetworkIdentity))]
public sealed class DummyPingableInteraction : MonoBehaviour, IInteractable, IDisplayNameProvider
{
	public string displayNameToken = "DUMMYINTERACTION_NAME";

	public string contextToken = "DUMMYINTERACTION_CONTEXT";

	public Interactability interactability = Interactability.ConditionsNotMet;

	public string GetContextString(Interactor activator)
	{
		return Language.GetString(contextToken);
	}

	public Interactability GetInteractability(Interactor activator)
	{
		return interactability;
	}

	public void OnInteractionBegin(Interactor activator)
	{
	}

	public string GetDisplayName()
	{
		return Language.GetString(displayNameToken);
	}

	public bool ShouldIgnoreSpherecastForInteractibility(Interactor activator)
	{
		return true;
	}

	public void OnEnable()
	{
		InstanceTracker.Add(this);
	}

	public void OnDisable()
	{
		InstanceTracker.Remove(this);
	}

	public bool ShouldShowOnScanner()
	{
		return interactability != Interactability.Disabled;
	}

	public bool ShouldProximityHighlight()
	{
		return false;
	}
}
