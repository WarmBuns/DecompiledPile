using System;
using RoR2;
using UnityEngine;
using UnityEngine.Events;

public class SpeechBubbleController : MonoBehaviour
{
	[Serializable]
	public struct SpeechCategory
	{
		public string refName;

		public float chanceToTrigger;

		public SpeechInfo[] speechInfos;

		public WeightedSelection<SpeechInfo> speechSelection;
	}

	[Serializable]
	public struct SpeechInfo
	{
		public string nameToken;

		public float weight;
	}

	[Header("Cached Components")]
	public Transform idealBubbleTransform;

	public CharacterBody characterBody;

	public SfxLocator sfxLocator;

	[Header("Speech Info")]
	public string formatStringToken;

	public float minimumDurationBetweenSpeech;

	public bool removeRepeats;

	[SerializeField]
	public SpeechCategory[] speechCategories;

	[Header("Events")]
	public UnityEvent onBodyDamageDealt;

	public UnityEvent onStart;

	public UnityEvent onBodyKill;

	public UnityEvent onBodyDeath;

	private GameObject speechBubbleInstance;

	private float lastSpeechStopwatch;

	private void Start()
	{
		Initialize();
		lastSpeechStopwatch = minimumDurationBetweenSpeech;
		onStart?.Invoke();
	}

	private void OnEnable()
	{
		GlobalEventManager.onServerDamageDealt += OnServerDamageDealt;
		GlobalEventManager.onCharacterDeathGlobal += OnCharacterDeathGlobal;
	}

	private void OnDisable()
	{
		GlobalEventManager.onServerDamageDealt -= OnServerDamageDealt;
		GlobalEventManager.onCharacterDeathGlobal -= OnCharacterDeathGlobal;
	}

	private void FixedUpdate()
	{
		lastSpeechStopwatch += Time.fixedDeltaTime;
	}

	private void OnCharacterDeathGlobal(DamageReport damageReport)
	{
		if ((object)damageReport.victimBody == characterBody)
		{
			onBodyDeath?.Invoke();
		}
		else if ((object)damageReport.attackerBody == characterBody)
		{
			onBodyKill?.Invoke();
		}
	}

	private void OnServerDamageDealt(DamageReport damageReport)
	{
		if ((object)damageReport.attackerBody == characterBody)
		{
			onBodyDamageDealt?.Invoke();
		}
	}

	private void Initialize()
	{
		for (int i = 0; i < speechCategories.Length; i++)
		{
			WeightedSelection<SpeechInfo> weightedSelection = new WeightedSelection<SpeechInfo>();
			for (int j = 0; j < speechCategories[i].speechInfos.Length; j++)
			{
				SpeechInfo value = speechCategories[i].speechInfos[j];
				weightedSelection.AddChoice(value, value.weight);
			}
			speechCategories[i].speechSelection = weightedSelection;
		}
	}

	private void SubmitSpeech(string token)
	{
		lastSpeechStopwatch = 0f;
		Chat.SendBroadcastChat(new Chat.NpcChatMessage
		{
			baseToken = token,
			formatStringToken = formatStringToken,
			sender = base.gameObject,
			sound = sfxLocator.barkSound
		});
	}

	public void TriggerSpeech(string refName)
	{
		if (lastSpeechStopwatch < minimumDurationBetweenSpeech)
		{
			return;
		}
		for (int i = 0; i < speechCategories.Length; i++)
		{
			if (speechCategories[i].refName == refName)
			{
				if (speechCategories[i].speechSelection.Count <= 0)
				{
					Initialize();
				}
				int num = speechCategories[i].speechSelection.EvaluateToChoiceIndex(UnityEngine.Random.value);
				SpeechInfo value = speechCategories[i].speechSelection.GetChoice(num).value;
				if (removeRepeats)
				{
					SpeechCategory speechCategory = speechCategories[i];
					speechCategory.speechSelection.RemoveChoice(num);
					speechCategories[i] = speechCategory;
				}
				if (UnityEngine.Random.value < speechCategories[i].chanceToTrigger)
				{
					SubmitSpeech(value.nameToken);
				}
			}
		}
	}
}
