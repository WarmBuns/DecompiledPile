using RoR2;
using UnityEngine;
using UnityEngine.UI;

namespace EntityStates.CaptainSupplyDrop;

public class BaseCaptainSupplyDropState : BaseState
{
	private ProxyInteraction interactionComponent;

	protected GenericEnergyComponent energyComponent;

	protected TeamFilter teamFilter;

	private Image energyIndicator;

	private GameObject energyIndicatorContainer;

	protected virtual bool shouldShowModel => true;

	protected virtual bool shouldShowEnergy => false;

	protected virtual string GetContextString(Interactor activator)
	{
		return null;
	}

	protected virtual Interactability GetInteractability(Interactor activator)
	{
		return Interactability.Disabled;
	}

	protected virtual void OnInteractionBegin(Interactor activator)
	{
	}

	protected virtual bool ShouldShowOnScanner()
	{
		return false;
	}

	protected virtual bool ShouldIgnoreSpherecastForInteractability(Interactor activator)
	{
		return false;
	}

	private string GetContextStringInternal(ProxyInteraction proxyInteraction, Interactor activator)
	{
		return GetContextString(activator);
	}

	private Interactability GetInteractabilityInternal(ProxyInteraction proxyInteraction, Interactor activator)
	{
		return GetInteractability(activator);
	}

	private void OnInteractionBeginInternal(ProxyInteraction proxyInteraction, Interactor activator)
	{
		OnInteractionBegin(activator);
	}

	private bool ShouldIgnoreSpherecastForInteractabilityInternal(ProxyInteraction proxyInteraction, Interactor activator)
	{
		return ShouldIgnoreSpherecastForInteractability(activator);
	}

	private bool ShouldShowOnScannerInternal(ProxyInteraction proxyInteraction)
	{
		return ShouldShowOnScanner();
	}

	public override void OnEnter()
	{
		base.OnEnter();
		energyComponent = GetComponent<GenericEnergyComponent>();
		teamFilter = GetComponent<TeamFilter>();
		interactionComponent = GetComponent<ProxyInteraction>();
		interactionComponent.getContextString = GetContextStringInternal;
		interactionComponent.getInteractability = GetInteractabilityInternal;
		interactionComponent.onInteractionBegin = OnInteractionBeginInternal;
		interactionComponent.shouldShowOnScanner = ShouldShowOnScannerInternal;
		interactionComponent.shouldIgnoreSpherecastForInteractability = ShouldIgnoreSpherecastForInteractabilityInternal;
		GetModelTransform().gameObject.SetActive(shouldShowModel);
		energyIndicatorContainer = FindModelChild("EnergyIndicatorContainer").gameObject;
		energyIndicator = FindModelChild("EnergyIndicator").GetComponent<Image>();
	}

	public override void OnExit()
	{
		interactionComponent.getContextString = null;
		interactionComponent.getInteractability = null;
		interactionComponent.onInteractionBegin = null;
		interactionComponent.shouldShowOnScanner = null;
		interactionComponent.shouldIgnoreSpherecastForInteractability = null;
		base.OnExit();
	}

	public override void Update()
	{
		base.Update();
		UpdateEnergyIndicator();
	}

	private void UpdateEnergyIndicator()
	{
		energyIndicatorContainer.SetActive(shouldShowEnergy);
		energyIndicator.fillAmount = energyComponent.normalizedEnergy;
	}
}
