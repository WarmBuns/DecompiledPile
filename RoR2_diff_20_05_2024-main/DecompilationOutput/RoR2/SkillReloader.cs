using EntityStates;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(NetworkIdentity))]
public class SkillReloader : MonoBehaviour
{
	private NetworkIdentity networkIdentity;

	public GenericSkill skill;

	public EntityStateMachine stateMachine;

	public SerializableEntityStateType reloadState;

	public float reloadDelay = 0.2f;

	private float timer;

	private void Awake()
	{
		networkIdentity = GetComponent<NetworkIdentity>();
	}

	private void Start()
	{
		timer = 0f;
	}

	private void FixedUpdate()
	{
		if (Util.HasEffectiveAuthority(networkIdentity))
		{
			bool flag = stateMachine.state.GetType() == typeof(Idle) && !stateMachine.HasPendingState();
			if (skill.stock < skill.maxStock && flag)
			{
				timer += Time.fixedDeltaTime;
			}
			else
			{
				timer = 0f;
			}
			if (timer >= reloadDelay || (skill.stock == 0 && flag))
			{
				stateMachine.SetNextState(EntityStateCatalog.InstantiateState(ref reloadState));
			}
		}
	}
}
