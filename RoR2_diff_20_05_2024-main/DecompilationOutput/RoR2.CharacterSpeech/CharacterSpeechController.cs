using System;
using System.Collections.Generic;
using RoR2.ConVar;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.CharacterSpeech;

public class CharacterSpeechController : MonoBehaviour
{
	[Serializable]
	public struct SpeechInfo
	{
		public string token;

		public float duration;

		public float maxWait;

		public float priority;

		public bool mustPlay;

		public override string ToString()
		{
			return $"{{ token=\"{token}\" duration={duration} maxWait={maxWait} priority={priority} mustPlay={mustPlay} }}";
		}
	}

	[Serializable]
	private struct SpeechRequest
	{
		public SpeechInfo speechInfo;

		public float submitTime;

		public override string ToString()
		{
			return $"{{ speechInfo={speechInfo} submitTime={submitTime} }}";
		}
	}

	public CharacterMaster initialCharacterMaster;

	public string chatFormatToken;

	private float localTime;

	private float nextSpeakTime;

	private List<SpeechRequest> speechRequestQueue;

	private CharacterMaster _characterMaster;

	private CharacterBody _currentCharacterBody;

	private static readonly BoolConVar cvEnableLogging = new BoolConVar("character_speech_debug", ConVarFlags.None, "0", "Enables/disables debug logging for CharacterSpeechController");

	public CharacterMaster characterMaster
	{
		get
		{
			return _characterMaster;
		}
		set
		{
			if ((object)_characterMaster != value)
			{
				if ((object)_characterMaster != null)
				{
					OnCharacterMasterLost(_characterMaster);
				}
				_characterMaster = value;
				if ((object)_characterMaster != null)
				{
					OnCharacterMasterDiscovered(_characterMaster);
				}
			}
		}
	}

	public CharacterBody currentCharacterBody
	{
		get
		{
			return _currentCharacterBody;
		}
		private set
		{
			if ((object)_currentCharacterBody != value)
			{
				if ((object)_currentCharacterBody != null)
				{
					OnCharacterBodyLost(_currentCharacterBody);
				}
				_currentCharacterBody = value;
				if ((object)_currentCharacterBody != null)
				{
					OnCharacterBodyDiscovered(_currentCharacterBody);
				}
			}
		}
	}

	public event Action<CharacterMaster> onCharacterMasterDiscovered;

	public event Action<CharacterMaster> onCharacterMasterLost;

	public event Action<CharacterBody> onCharacterBodyDiscovered;

	public event Action<CharacterBody> onCharacterBodyLost;

	private void Awake()
	{
		if (!NetworkServer.active)
		{
			base.enabled = false;
		}
		else
		{
			speechRequestQueue = new List<SpeechRequest>();
		}
	}

	private void Start()
	{
		if ((object)characterMaster == null)
		{
			characterMaster = initialCharacterMaster;
		}
	}

	private void FixedUpdate()
	{
		Process(Time.fixedDeltaTime);
	}

	private void OnDestroy()
	{
		if (NetworkServer.active)
		{
			characterMaster = null;
		}
	}

	public void SpeakNow(in SpeechInfo speechInfo)
	{
		if (!NetworkServer.active)
		{
			return;
		}
		if (cvEnableLogging.value)
		{
			Log($"Playing speech: {speechInfo}");
		}
		nextSpeakTime = localTime + speechInfo.duration;
		CharacterBody characterBody = null;
		GameObject sender = null;
		SfxLocator sfxLocator = null;
		if ((bool)characterMaster)
		{
			characterBody = characterMaster.GetBody();
			if ((bool)characterBody)
			{
				sfxLocator = characterBody.GetComponent<SfxLocator>();
			}
		}
		Chat.SendBroadcastChat(new Chat.NpcChatMessage
		{
			baseToken = speechInfo.token,
			formatStringToken = chatFormatToken,
			sender = sender,
			sound = sfxLocator?.barkSound
		});
	}

	public void EnqueueSpeech(in SpeechInfo speechInfo)
	{
		if (NetworkServer.active)
		{
			SpeechRequest speechRequest = default(SpeechRequest);
			speechRequest.speechInfo = speechInfo;
			speechRequest.submitTime = localTime;
			SpeechRequest speechRequest2 = speechRequest;
			speechRequestQueue.Add(speechRequest2);
			if (cvEnableLogging.value)
			{
				Log($"Enqueued speech: {speechRequest2}");
			}
		}
	}

	private void Process(float deltaTime)
	{
		localTime += deltaTime;
		if (!(nextSpeakTime <= localTime))
		{
			return;
		}
		int bestNextRequestIndex = GetBestNextRequestIndex();
		if (bestNextRequestIndex == -1)
		{
			return;
		}
		SpeechRequest speechRequest = speechRequestQueue[bestNextRequestIndex];
		if (cvEnableLogging.value)
		{
			Log($"Found best request: bestNextRequestIndex={bestNextRequestIndex} bestNextRequest={speechRequest}");
			for (int i = 0; i < bestNextRequestIndex; i++)
			{
				Log($"Dropping request: i={i} request={speechRequestQueue[i]}");
			}
		}
		speechRequestQueue.RemoveRange(0, bestNextRequestIndex + 1);
		SpeakNow(in speechRequest.speechInfo);
	}

	private int GetBestNextRequestIndex()
	{
		if (speechRequestQueue.Count == 0)
		{
			return -1;
		}
		for (int i = 0; i < speechRequestQueue.Count; i++)
		{
			SpeechRequest speechRequest = speechRequestQueue[i];
			SpeechInfo speechInfo = speechRequest.speechInfo;
			if (speechInfo.mustPlay)
			{
				return i;
			}
			float num = speechRequest.submitTime + speechInfo.maxWait;
			if (localTime > num)
			{
				continue;
			}
			float num2 = localTime + speechInfo.duration;
			bool flag = false;
			for (int j = i + 1; j < speechRequestQueue.Count; j++)
			{
				SpeechRequest speechRequest2 = speechRequestQueue[j];
				SpeechInfo speechInfo2 = speechRequest2.speechInfo;
				if (!(speechInfo.priority >= speechInfo2.priority) || speechInfo2.mustPlay)
				{
					if (speechRequest2.submitTime + speechInfo2.maxWait < num2)
					{
						flag = true;
						break;
					}
					num2 += speechInfo2.duration;
				}
			}
			if (!flag)
			{
				return i;
			}
		}
		return -1;
	}

	private void OnCharacterMasterDiscovered(CharacterMaster characterMaster)
	{
		characterMaster.onBodyStart += OnCharacterMasterBodyStart;
		this.onCharacterMasterDiscovered?.Invoke(characterMaster);
		currentCharacterBody = characterMaster.GetBody();
	}

	private void OnCharacterMasterLost(CharacterMaster characterMaster)
	{
		currentCharacterBody = null;
		this.onCharacterMasterLost?.Invoke(characterMaster);
		characterMaster.onBodyDestroyed -= OnCharacterMasterBodyDestroyed;
	}

	private void OnCharacterMasterBodyStart(CharacterBody characterBody)
	{
		currentCharacterBody = characterBody;
	}

	private void OnCharacterMasterBodyDestroyed(CharacterBody characterBody)
	{
		if ((object)characterBody == currentCharacterBody)
		{
			currentCharacterBody = null;
		}
	}

	private void OnCharacterBodyDiscovered(CharacterBody characterBody)
	{
		this.onCharacterBodyDiscovered?.Invoke(characterBody);
	}

	private void OnCharacterBodyLost(CharacterBody characterBody)
	{
		this.onCharacterBodyLost?.Invoke(characterBody);
	}

	private void Log(string str)
	{
	}
}
