using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.MoonElevator;

public abstract class MoonElevatorBaseState : BaseState
{
	[SerializeField]
	public float duration;

	[SerializeField]
	public string enterSfxString;

	protected GenericInteraction genericInteraction;

	public virtual EntityState nextState => new Uninitialized();

	public virtual Interactability interactability => Interactability.Disabled;

	public virtual bool goToNextStateAutomatically => false;

	public virtual bool showBaseEffects => false;

	public override void OnEnter()
	{
		base.OnEnter();
		genericInteraction = GetComponent<GenericInteraction>();
		Util.PlaySound(enterSfxString, base.gameObject);
		if (NetworkServer.active)
		{
			genericInteraction.Networkinteractability = interactability;
			if (interactability == Interactability.Available)
			{
				genericInteraction.onActivation?.AddListener(OnInteractionBegin);
			}
		}
		FindModelChild("EffectBase").gameObject.SetActive(showBaseEffects);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge > duration && base.isAuthority && goToNextStateAutomatically)
		{
			outer.SetNextState(nextState);
		}
	}

	protected virtual void OnInteractionBegin(Interactor activator)
	{
	}
}
