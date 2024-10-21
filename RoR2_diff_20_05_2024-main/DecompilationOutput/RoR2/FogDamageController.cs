using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class FogDamageController : NetworkBehaviour
{
	[SerializeField]
	[Tooltip("Used to control which teams to damage.  If it's null, it damages ALL teams")]
	private TeamFilter teamFilter;

	[SerializeField]
	[Tooltip("If true, it damages all OTHER teams than the one specified.  If false, it damages the specified team.")]
	private bool invertTeamFilter;

	[SerializeField]
	[Tooltip("The period in seconds in between each tick")]
	private float tickPeriodSeconds;

	[Range(0f, 1f)]
	[SerializeField]
	[Tooltip("The fraction of combined health to deduct per second.  Note that damage is actually applied per tick, not per second.")]
	private float healthFractionPerSecond;

	[SerializeField]
	[Tooltip("The coefficient to increase the damage by, for every tick they take outside the zone.")]
	private float healthFractionRampCoefficientPerSecond;

	[SerializeField]
	[Tooltip("The buff to apply when in danger, i.e not in the safe zone.")]
	private BuffDef dangerBuffDef;

	[SerializeField]
	private float dangerBuffDuration;

	[SerializeField]
	[Tooltip("An initial list of safe zones behaviors which protect bodies from the fog")]
	private BaseZoneBehavior[] initialSafeZones;

	private float dictionaryValidationTimer;

	private float damageTimer;

	private List<IZone> safeZones = new List<IZone>();

	private Dictionary<CharacterBody, int> characterBodyToStacks = new Dictionary<CharacterBody, int>();

	private void Start()
	{
		BaseZoneBehavior[] array = initialSafeZones;
		foreach (BaseZoneBehavior zone in array)
		{
			AddSafeZone(zone);
		}
	}

	public void AddSafeZone(IZone zone)
	{
		safeZones.Add(zone);
	}

	public void RemoveSafeZone(IZone zone)
	{
		safeZones.Remove(zone);
	}

	private void FixedUpdate()
	{
		MyFixedUpdate(Time.fixedDeltaTime);
	}

	private void MyFixedUpdate(float deltaTime)
	{
		if (!NetworkServer.active)
		{
			return;
		}
		damageTimer += deltaTime;
		dictionaryValidationTimer += deltaTime;
		if (dictionaryValidationTimer > 60f)
		{
			dictionaryValidationTimer = 0f;
			CharacterBody[] array = new CharacterBody[characterBodyToStacks.Keys.Count];
			characterBodyToStacks.Keys.CopyTo(array, 0);
			for (int i = 0; i < array.Length; i++)
			{
				if (!array[i])
				{
					characterBodyToStacks.Remove(array[i]);
				}
			}
		}
		while (damageTimer > tickPeriodSeconds)
		{
			damageTimer -= tickPeriodSeconds;
			if ((bool)teamFilter)
			{
				if (invertTeamFilter)
				{
					for (TeamIndex teamIndex = TeamIndex.Neutral; teamIndex < TeamIndex.Count; teamIndex++)
					{
						if (teamIndex != teamFilter.teamIndex && teamIndex != TeamIndex.None && teamIndex != 0)
						{
							EvaluateTeam(teamIndex);
						}
					}
				}
				else
				{
					EvaluateTeam(teamFilter.teamIndex);
				}
			}
			else
			{
				for (TeamIndex teamIndex2 = TeamIndex.Neutral; teamIndex2 < TeamIndex.Count; teamIndex2++)
				{
					EvaluateTeam(teamIndex2);
				}
			}
			foreach (KeyValuePair<CharacterBody, int> characterBodyToStack in characterBodyToStacks)
			{
				CharacterBody key = characterBodyToStack.Key;
				if (!key || !key.transform || !key.healthComponent)
				{
					continue;
				}
				int num = characterBodyToStack.Value - 1;
				float num2 = healthFractionPerSecond * (1f + (float)num * healthFractionRampCoefficientPerSecond * tickPeriodSeconds) * tickPeriodSeconds * key.healthComponent.fullCombinedHealth;
				if (num2 > 0f)
				{
					if (key.master != null && (bool)key.master.GetComponent<DevotedLemurianController>())
					{
						num2 *= 0.5f;
					}
					key.healthComponent.TakeDamage(new DamageInfo
					{
						damage = num2,
						position = key.corePosition,
						damageType = (DamageType.BypassArmor | DamageType.BypassBlock),
						damageColorIndex = DamageColorIndex.Void
					});
				}
				if ((bool)dangerBuffDef)
				{
					key.AddTimedBuff(dangerBuffDef, dangerBuffDuration);
				}
			}
		}
	}

	public IEnumerable<CharacterBody> GetAffectedBodies()
	{
		TeamIndex currentTeam;
		if ((bool)teamFilter)
		{
			if (invertTeamFilter)
			{
				currentTeam = TeamIndex.Neutral;
				while (currentTeam < TeamIndex.Count)
				{
					IEnumerable<CharacterBody> affectedBodiesOnTeam = GetAffectedBodiesOnTeam(currentTeam);
					foreach (CharacterBody item in affectedBodiesOnTeam)
					{
						yield return item;
					}
					TeamIndex teamIndex = currentTeam + 1;
					currentTeam = teamIndex;
				}
				yield break;
			}
			IEnumerable<CharacterBody> affectedBodiesOnTeam2 = GetAffectedBodiesOnTeam(teamFilter.teamIndex);
			foreach (CharacterBody item2 in affectedBodiesOnTeam2)
			{
				yield return item2;
			}
			yield break;
		}
		currentTeam = TeamIndex.Neutral;
		while (currentTeam < TeamIndex.Count)
		{
			IEnumerable<CharacterBody> affectedBodiesOnTeam3 = GetAffectedBodiesOnTeam(currentTeam);
			foreach (CharacterBody item3 in affectedBodiesOnTeam3)
			{
				yield return item3;
			}
			TeamIndex teamIndex = currentTeam + 1;
			currentTeam = teamIndex;
		}
	}

	public IEnumerable<CharacterBody> GetAffectedBodiesOnTeam(TeamIndex teamIndex)
	{
		foreach (TeamComponent teamMember in TeamComponent.GetTeamMembers(teamIndex))
		{
			CharacterBody body = teamMember.body;
			bool flag = false;
			foreach (IZone safeZone in safeZones)
			{
				if (safeZone.IsInBounds(teamMember.transform.position))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				yield return body;
			}
		}
	}

	private void EvaluateTeam(TeamIndex teamIndex)
	{
		foreach (TeamComponent teamMember in TeamComponent.GetTeamMembers(teamIndex))
		{
			CharacterBody body = teamMember.body;
			bool flag = characterBodyToStacks.ContainsKey(body);
			bool flag2 = false;
			foreach (IZone safeZone in safeZones)
			{
				if (safeZone.IsInBounds(teamMember.transform.position))
				{
					flag2 = true;
					break;
				}
			}
			if (flag2)
			{
				if (flag)
				{
					characterBodyToStacks.Remove(body);
				}
			}
			else if (!flag)
			{
				characterBodyToStacks.Add(body, 1);
			}
			else
			{
				characterBodyToStacks[body]++;
			}
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		bool result = default(bool);
		return result;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
	}

	public override void PreStartClient()
	{
	}
}
