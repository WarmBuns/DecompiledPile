using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[DisallowMultipleComponent]
public class TeamComponent : NetworkBehaviour, ILifeBehavior
{
	public struct Team
	{
		public List<TeamComponent> members;

		public ReadOnlyCollection<TeamComponent> readOnlyMembers;

		public TeamIndex teamIndex;

		private int importanceCollection_;

		public int averageImportance;

		public int importanceCollection
		{
			get
			{
				return importanceCollection_;
			}
			set
			{
				importanceCollection_ = value;
				averageImportance = importanceCollection_ / ((members.Count == 0) ? 1 : members.Count);
			}
		}

		public void Init(TeamIndex index)
		{
			members = new List<TeamComponent>();
			readOnlyMembers = members.AsReadOnly();
			teamIndex = index;
		}
	}

	public bool hideAllyCardDisplay;

	[SerializeField]
	private TeamIndex _teamIndex = TeamIndex.None;

	private TeamIndex oldTeamIndex = TeamIndex.None;

	private GameObject defaultIndicatorPrefab;

	private GameObject indicator;

	private static readonly Queue<TeamComponent> indicatorSetupQueue;

	private static Team[] teamsList;

	private static ReadOnlyCollection<TeamComponent> emptyTeamMembers;

	public CharacterBody body { get; private set; }

	public TeamIndex teamIndex
	{
		get
		{
			return _teamIndex;
		}
		set
		{
			if (_teamIndex != value)
			{
				_teamIndex = value;
				if (Application.isPlaying)
				{
					SetDirtyBit(1u);
					OnChangeTeam(value);
				}
			}
		}
	}

	public static event Action<TeamComponent, TeamIndex> onJoinTeamGlobal;

	public static event Action<TeamComponent, TeamIndex> onLeaveTeamGlobal;

	private static bool TeamIsValid(TeamIndex teamIndex)
	{
		if (teamIndex >= TeamIndex.Neutral)
		{
			return teamIndex < TeamIndex.Count;
		}
		return false;
	}

	private void OnChangeTeam(TeamIndex newTeamIndex)
	{
		OnLeaveTeam(oldTeamIndex);
		OnJoinTeam(newTeamIndex);
	}

	private void OnLeaveTeam(TeamIndex oldTeamIndex)
	{
		if (TeamIsValid(oldTeamIndex))
		{
			teamsList[(int)oldTeamIndex].members.Remove(this);
		}
		TeamComponent.onLeaveTeamGlobal?.Invoke(this, oldTeamIndex);
	}

	private void OnJoinTeam(TeamIndex newTeamIndex)
	{
		if (TeamIsValid(newTeamIndex))
		{
			teamsList[(int)newTeamIndex].members.Add(this);
		}
		indicatorSetupQueue.Enqueue(this);
		HurtBox[] array = ((!body) ? null : body.hurtBoxGroup?.hurtBoxes);
		if (array != null)
		{
			HurtBox[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i].teamIndex = newTeamIndex;
			}
		}
		oldTeamIndex = newTeamIndex;
		TeamComponent.onJoinTeamGlobal?.Invoke(this, newTeamIndex);
	}

	private static void ProcessIndicatorSetupRequests()
	{
		while (indicatorSetupQueue.Count > 0)
		{
			TeamComponent teamComponent = indicatorSetupQueue.Dequeue();
			if ((bool)teamComponent)
			{
				try
				{
					teamComponent.SetupIndicator();
				}
				catch (Exception message)
				{
					Debug.LogError(message);
				}
			}
		}
	}

	[SystemInitializer(new Type[] { })]
	private static void Init()
	{
		RoR2Application.onFixedUpdate += ProcessIndicatorSetupRequests;
	}

	public void RequestDefaultIndicator(GameObject newIndicatorPrefab)
	{
		defaultIndicatorPrefab = newIndicatorPrefab;
		indicatorSetupQueue.Enqueue(this);
	}

	private void SetupIndicator()
	{
		if ((bool)indicator || !body)
		{
			return;
		}
		CharacterMaster master = body.master;
		bool flag = (bool)master && master.isBoss;
		GameObject gameObject = defaultIndicatorPrefab;
		if ((bool)master && teamIndex == TeamIndex.Player)
		{
			gameObject = LegacyResourcesAPI.Load<GameObject>(body.isPlayerControlled ? "Prefabs/PositionIndicators/PlayerPositionIndicator" : "Prefabs/PositionIndicators/NPCPositionIndicator");
		}
		else if (flag)
		{
			gameObject = LegacyResourcesAPI.Load<GameObject>("Prefabs/PositionIndicators/BossPositionIndicator");
		}
		if ((bool)indicator)
		{
			UnityEngine.Object.Destroy(indicator);
			indicator = null;
		}
		if ((bool)gameObject)
		{
			indicator = UnityEngine.Object.Instantiate(gameObject, base.transform);
			indicator.GetComponent<PositionIndicator>().targetTransform = body.coreTransform;
			Nameplate component = indicator.GetComponent<Nameplate>();
			if ((bool)component)
			{
				component.SetBody(body);
			}
		}
	}

	static TeamComponent()
	{
		indicatorSetupQueue = new Queue<TeamComponent>();
		emptyTeamMembers = new List<TeamComponent>().AsReadOnly();
		teamsList = new Team[5];
		for (int i = 0; i < teamsList.Length; i++)
		{
			teamsList[i].Init((TeamIndex)i);
		}
	}

	private void Awake()
	{
		body = GetComponent<CharacterBody>();
	}

	public void Start()
	{
		SetupIndicator();
		if (oldTeamIndex != teamIndex)
		{
			OnChangeTeam(teamIndex);
		}
	}

	private void OnDestroy()
	{
		teamIndex = TeamIndex.None;
	}

	public void OnDeathStart()
	{
		base.enabled = false;
	}

	public override bool OnSerialize(NetworkWriter writer, bool initialState)
	{
		writer.Write(teamIndex);
		if (!initialState)
		{
			return base.syncVarDirtyBits != 0;
		}
		return true;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		teamIndex = reader.ReadTeamIndex();
	}

	public static ReadOnlyCollection<TeamComponent> GetTeamMembers(TeamIndex teamIndex)
	{
		if (!TeamIsValid(teamIndex))
		{
			return emptyTeamMembers;
		}
		return teamsList[(int)teamIndex].readOnlyMembers;
	}

	public static TeamIndex GetObjectTeam(GameObject gameObject)
	{
		if ((bool)gameObject)
		{
			TeamComponent component = gameObject.GetComponent<TeamComponent>();
			if ((bool)component)
			{
				return component.teamIndex;
			}
		}
		return TeamIndex.None;
	}

	public static TeamIndex GetObjectTeam(TeamComponent teamComponent)
	{
		if ((bool)teamComponent)
		{
			return teamComponent.teamIndex;
		}
		return TeamIndex.None;
	}

	internal static int GetTeamAverageImportance(TeamComponent teamComponent)
	{
		if ((bool)teamComponent)
		{
			return teamsList[(int)teamComponent.teamIndex].averageImportance;
		}
		Debug.LogError("Error: Tried to get average team importance using a null component reference");
		return -1;
	}

	private void UNetVersion()
	{
	}

	public override void PreStartClient()
	{
	}
}
