using EntityStates;
using UnityEngine;

namespace RoR2;

public class CharacterDeathBehavior : MonoBehaviour
{
	[Tooltip("The state machine to set the state of when this character is killed.")]
	public EntityStateMachine deathStateMachine;

	[Tooltip("The state to enter when this character is killed.")]
	public SerializableEntityStateType deathState;

	[Tooltip("The state machine(s) to set to idle when this character is killed.")]
	public EntityStateMachine[] idleStateMachine;

	[SerializeField]
	[Tooltip("Be default, dead characters are set to the debris layer. Check this to bypass that.")]
	private bool bypassDeathLayerChange;

	public void OnDeath()
	{
		if (Util.HasEffectiveAuthority(base.gameObject))
		{
			if ((bool)deathStateMachine)
			{
				deathStateMachine.SetNextState(EntityStateCatalog.InstantiateState(ref deathState));
			}
			EntityStateMachine[] array = idleStateMachine;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetNextState(new Idle());
			}
		}
		if (!bypassDeathLayerChange)
		{
			base.gameObject.layer = LayerIndex.debris.intVal;
			CharacterMotor component = GetComponent<CharacterMotor>();
			if ((bool)component)
			{
				component.Motor.RebuildCollidableLayers();
			}
		}
		ILifeBehavior[] components = GetComponents<ILifeBehavior>();
		for (int i = 0; i < components.Length; i++)
		{
			components[i].OnDeathStart();
		}
		ModelLocator component2 = GetComponent<ModelLocator>();
		if (!component2)
		{
			return;
		}
		Transform modelTransform = component2.modelTransform;
		if ((bool)modelTransform)
		{
			components = modelTransform.GetComponents<ILifeBehavior>();
			for (int i = 0; i < components.Length; i++)
			{
				components[i].OnDeathStart();
			}
		}
	}
}
