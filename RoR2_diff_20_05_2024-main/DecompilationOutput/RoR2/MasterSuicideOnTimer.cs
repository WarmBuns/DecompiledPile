using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class MasterSuicideOnTimer : MonoBehaviour
{
	public float lifeTimer;

	[Tooltip("Reset the timer if we're within this radius of the owner")]
	public float timerResetDistanceToOwner;

	public MinionOwnership minionOwnership;

	private CharacterBody body;

	private CharacterBody ownerBody;

	private float timer;

	private bool hasDied;

	private CharacterMaster characterMaster;

	private void Start()
	{
		characterMaster = GetComponent<CharacterMaster>();
		if ((bool)minionOwnership && (bool)minionOwnership.ownerMaster)
		{
			ownerBody = minionOwnership.ownerMaster.GetBody();
		}
	}

	private void FixedUpdate()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		if ((bool)ownerBody)
		{
			if (!body)
			{
				body = characterMaster.GetBody();
			}
			if ((bool)body && (ownerBody.transform.position - body.transform.position).sqrMagnitude < timerResetDistanceToOwner * timerResetDistanceToOwner)
			{
				timer = 0f;
			}
		}
		timer += Time.fixedDeltaTime;
		if (!(timer >= lifeTimer) || hasDied)
		{
			return;
		}
		GameObject bodyObject = characterMaster.GetBodyObject();
		if ((bool)bodyObject)
		{
			HealthComponent component = bodyObject.GetComponent<HealthComponent>();
			if ((bool)component)
			{
				component.Suicide();
				hasDied = true;
			}
		}
	}
}
