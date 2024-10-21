using System.Collections.ObjectModel;
using UnityEngine;

namespace RoR2.Projectile;

[RequireComponent(typeof(Rigidbody))]
public class DaggerController : MonoBehaviour
{
	private new Transform transform;

	private Rigidbody rigidbody;

	public Transform target;

	public float acceleration;

	public float delayTimer;

	public float giveupTimer = 8f;

	public float deathTimer = 10f;

	private float timer;

	public float turbulence;

	private bool hasPlayedSound;

	private void Awake()
	{
		transform = base.transform;
		rigidbody = GetComponent<Rigidbody>();
		rigidbody.AddRelativeForce(Random.insideUnitSphere * 50f);
	}

	private void FixedUpdate()
	{
		timer += Time.fixedDeltaTime;
		if (timer < giveupTimer)
		{
			if ((bool)target)
			{
				Vector3 vector = target.transform.position - transform.position;
				if (vector != Vector3.zero)
				{
					transform.rotation = Util.QuaternionSafeLookRotation(vector);
				}
				if (timer >= delayTimer)
				{
					rigidbody.AddForce(transform.forward * acceleration);
					if (!hasPlayedSound)
					{
						Util.PlaySound("Play_item_proc_dagger_fly", base.gameObject);
						hasPlayedSound = true;
					}
				}
			}
		}
		else
		{
			rigidbody.useGravity = true;
		}
		if (!target)
		{
			target = FindTarget();
		}
		else
		{
			HealthComponent component = target.GetComponent<HealthComponent>();
			if ((bool)component && !component.alive)
			{
				target = FindTarget();
			}
		}
		if (timer > deathTimer)
		{
			Object.Destroy(base.gameObject);
		}
	}

	private Transform FindTarget()
	{
		ReadOnlyCollection<TeamComponent> teamMembers = TeamComponent.GetTeamMembers(TeamIndex.Monster);
		float num = 99999f;
		Transform result = null;
		for (int i = 0; i < teamMembers.Count; i++)
		{
			float num2 = Vector3.SqrMagnitude(teamMembers[i].transform.position - transform.position);
			if (num2 < num)
			{
				num = num2;
				result = teamMembers[i].transform;
			}
		}
		return result;
	}
}
