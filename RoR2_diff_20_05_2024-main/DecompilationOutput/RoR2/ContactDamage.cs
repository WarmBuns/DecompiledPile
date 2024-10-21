using RoR2.ConVar;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(CharacterBody))]
public class ContactDamage : MonoBehaviour
{
	protected static FloatConVar cvContactDamageUpdateInterval = new FloatConVar("contact_damage_update_interval", ConVarFlags.Cheat, "0.1", "Frequency that the contact damage fires.");

	public float damagePerSecondCoefficient = 2f;

	[Tooltip("The frequency that this can hurt the same target. Is NOT the frequency that it checks for targets!")]
	public float damageInterval = 0.25f;

	public float pushForcePerSecond = 4000f;

	public HitBoxGroup hitBoxGroup;

	public DamageTypeCombo damageType;

	public bool doBlastAttackInstead;

	private OverlapAttack overlapAttack;

	private CharacterBody characterBody;

	private TeamComponent teamComponent;

	private float refreshTimer;

	private float fireTimer;

	private bool hasFiredThisUpdate;

	private void Start()
	{
		characterBody = GetComponent<CharacterBody>();
		teamComponent = GetComponent<TeamComponent>();
		InitOverlapAttack();
	}

	private void FixedUpdate()
	{
		if (NetworkServer.active)
		{
			hasFiredThisUpdate = false;
			refreshTimer -= Time.fixedDeltaTime;
			fireTimer -= Time.fixedDeltaTime;
			if (refreshTimer <= 0f)
			{
				refreshTimer = damageInterval;
				overlapAttack.ResetIgnoredHealthComponents();
				overlapAttack.teamIndex = teamComponent.teamIndex;
				FireOverlaps();
			}
			if (fireTimer <= 0f)
			{
				FireOverlaps();
			}
		}
	}

	private void InitOverlapAttack()
	{
		overlapAttack = new OverlapAttack
		{
			attacker = base.gameObject,
			inflictor = base.gameObject,
			hitBoxGroup = hitBoxGroup,
			teamIndex = teamComponent.teamIndex
		};
	}

	private void FireOverlaps()
	{
		if (!hasFiredThisUpdate)
		{
			if (overlapAttack == null)
			{
				InitOverlapAttack();
			}
			hasFiredThisUpdate = true;
			fireTimer = cvContactDamageUpdateInterval.value;
			overlapAttack.damage = characterBody.damage * damagePerSecondCoefficient * damageInterval;
			overlapAttack.pushAwayForce = pushForcePerSecond * damageInterval;
			overlapAttack.damageType = damageType;
			overlapAttack.Fire();
		}
	}
}
