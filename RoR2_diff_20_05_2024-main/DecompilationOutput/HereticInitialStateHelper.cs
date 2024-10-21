using EntityStates;
using EntityStates.Heretic;
using RoR2;
using UnityEngine;

public class HereticInitialStateHelper : MonoBehaviour
{
	[SerializeField]
	private EntityStateMachine entityStateMachine;

	public void PrepareTransformation()
	{
		entityStateMachine.initialStateType = new SerializableEntityStateType(typeof(SpawnState));
	}
}
