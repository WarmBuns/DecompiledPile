using System.Collections.Generic;
using System.Collections.ObjectModel;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class EscapeSequenceExtractionZone : MonoBehaviour
{
	private class EscapeSequenceExtractionZoneObjectiveTracker : ObjectivePanelController.ObjectiveTracker
	{
		private int cachedLivingPlayersCount;

		private int cachedPlayersInRadiusCount;

		private EscapeSequenceExtractionZone extractionZone => (EscapeSequenceExtractionZone)sourceDescriptor.source;

		public override string ToString()
		{
			cachedLivingPlayersCount = extractionZone.livingPlayerCount;
			cachedPlayersInRadiusCount = extractionZone.playersInRadius.Count;
			string text = Language.GetString(extractionZone.objectiveToken);
			if (cachedLivingPlayersCount >= 1)
			{
				text = Language.GetStringFormatted("OBJECTIVE_FRACTION_PROGRESS_FORMAT", text, cachedPlayersInRadiusCount, cachedLivingPlayersCount);
			}
			return text;
		}

		protected override bool IsDirty()
		{
			if (cachedLivingPlayersCount != extractionZone.livingPlayerCount)
			{
				return cachedPlayersInRadiusCount != extractionZone.playersInRadius.Count;
			}
			return false;
		}
	}

	public float radius;

	public string objectiveToken;

	public GameEndingDef successEnding;

	private int livingPlayerCount;

	private List<CharacterBody> playersInRadius;

	private TeamIndex teamIndex = TeamIndex.Player;

	private void Awake()
	{
		playersInRadius = new List<CharacterBody>();
	}

	private void FixedUpdate()
	{
		livingPlayerCount = CountLivingPlayers(teamIndex);
		playersInRadius.Clear();
		GetPlayersInRadius(base.transform.position, radius * radius, teamIndex, playersInRadius);
		if (NetworkServer.active && livingPlayerCount > 0 && playersInRadius.Count >= livingPlayerCount)
		{
			HandleEndingServer();
		}
	}

	private void OnEnable()
	{
		if (InstanceTracker.GetInstancesList<EscapeSequenceExtractionZone>().Count == 0)
		{
			ObjectivePanelController.collectObjectiveSources += ReportObjectives;
		}
		InstanceTracker.Add(this);
	}

	private void OnDisable()
	{
		InstanceTracker.Remove(this);
		if (InstanceTracker.GetInstancesList<EscapeSequenceExtractionZone>().Count == 0)
		{
			ObjectivePanelController.collectObjectiveSources -= ReportObjectives;
		}
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawSphere(base.transform.position, radius);
	}

	public void KillAllStragglers()
	{
		List<CharacterMaster> list = new List<CharacterMaster>(CharacterMaster.readOnlyInstancesList);
		for (int i = 0; i < list.Count; i++)
		{
			CharacterMaster characterMaster = list[i];
			if ((bool)characterMaster && !playersInRadius.Contains(characterMaster.GetBody()))
			{
				characterMaster.TrueKill(null, null, DamageType.Silent | DamageType.VoidDeath);
			}
		}
	}

	public void HandleEndingServer()
	{
		if (!Run.instance.isGameOverServer)
		{
			KillAllStragglers();
			if (playersInRadius.Count > 0)
			{
				Run.instance.BeginGameOver(successEnding);
			}
		}
	}

	private static void ReportObjectives(CharacterMaster characterMaster, List<ObjectivePanelController.ObjectiveSourceDescriptor> dest)
	{
		List<EscapeSequenceExtractionZone> instancesList = InstanceTracker.GetInstancesList<EscapeSequenceExtractionZone>();
		for (int i = 0; i < instancesList.Count; i++)
		{
			EscapeSequenceExtractionZone escapeSequenceExtractionZone = instancesList[i];
			if (characterMaster.teamIndex == escapeSequenceExtractionZone.teamIndex)
			{
				ObjectivePanelController.ObjectiveSourceDescriptor objectiveSourceDescriptor = default(ObjectivePanelController.ObjectiveSourceDescriptor);
				objectiveSourceDescriptor.master = characterMaster;
				objectiveSourceDescriptor.objectiveType = typeof(EscapeSequenceExtractionZoneObjectiveTracker);
				objectiveSourceDescriptor.source = escapeSequenceExtractionZone;
			}
		}
	}

	private static bool IsPointInRadius(Vector3 origin, float chargingRadiusSqr, Vector3 point)
	{
		if ((point - origin).sqrMagnitude <= chargingRadiusSqr)
		{
			return true;
		}
		return false;
	}

	private static bool IsBodyInRadius(Vector3 origin, float chargingRadiusSqr, CharacterBody characterBody)
	{
		return IsPointInRadius(origin, chargingRadiusSqr, characterBody.corePosition);
	}

	private static int CountLivingPlayers(TeamIndex teamIndex)
	{
		int num = 0;
		ReadOnlyCollection<TeamComponent> teamMembers = TeamComponent.GetTeamMembers(teamIndex);
		for (int i = 0; i < teamMembers.Count; i++)
		{
			if (teamMembers[i].body.isPlayerControlled)
			{
				num++;
			}
		}
		return num;
	}

	public int CountPlayersInRadius(Vector3 origin, float chargingRadiusSqr, TeamIndex teamIndex)
	{
		int num = 0;
		ReadOnlyCollection<TeamComponent> teamMembers = TeamComponent.GetTeamMembers(teamIndex);
		for (int i = 0; i < teamMembers.Count; i++)
		{
			TeamComponent teamComponent = teamMembers[i];
			if (teamComponent.body.isPlayerControlled && IsBodyInRadius(origin, chargingRadiusSqr, teamComponent.body))
			{
				num++;
			}
		}
		return num;
	}

	private int GetPlayersInRadius(Vector3 origin, float chargingRadiusSqr, TeamIndex teamIndex, List<CharacterBody> dest)
	{
		int result = 0;
		ReadOnlyCollection<TeamComponent> teamMembers = TeamComponent.GetTeamMembers(teamIndex);
		for (int i = 0; i < teamMembers.Count; i++)
		{
			TeamComponent teamComponent = teamMembers[i];
			if (teamComponent.body.isPlayerControlled && IsBodyInRadius(origin, chargingRadiusSqr, teamComponent.body))
			{
				dest.Add(teamComponent.body);
			}
		}
		return result;
	}

	public bool IsBodyInRadius(CharacterBody body)
	{
		if (!body)
		{
			return false;
		}
		return IsBodyInRadius(base.transform.position, radius * radius, body);
	}
}
