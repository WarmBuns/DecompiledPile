using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.CharacterSpeech;

[RequireComponent(typeof(CharacterSpeechController))]
public class BaseCharacterSpeechDriver : MonoBehaviour
{
	protected CharacterSpeechController characterSpeechController { get; private set; }

	protected CharacterBody currentCharacterBody { get; private set; }

	protected void Awake()
	{
		if (!NetworkServer.active)
		{
			base.enabled = false;
		}
		else
		{
			characterSpeechController = GetComponent<CharacterSpeechController>();
		}
	}

	protected void OnEnable()
	{
		if (NetworkServer.active)
		{
			characterSpeechController.onCharacterBodyDiscovered += OnCharacterBodyDiscovered;
			characterSpeechController.onCharacterBodyLost += OnCharacterBodyLost;
			if ((object)characterSpeechController.currentCharacterBody != null)
			{
				OnCharacterBodyDiscovered(characterSpeechController.currentCharacterBody);
			}
		}
	}

	protected void OnDisable()
	{
		if (NetworkServer.active)
		{
			if ((object)currentCharacterBody != null)
			{
				OnCharacterBodyLost(currentCharacterBody);
			}
			characterSpeechController.onCharacterBodyLost -= OnCharacterBodyLost;
			characterSpeechController.onCharacterBodyDiscovered -= OnCharacterBodyDiscovered;
		}
	}

	protected void OnDestroy()
	{
		_ = NetworkServer.active;
	}

	protected virtual void OnCharacterBodyDiscovered(CharacterBody characterBody)
	{
		currentCharacterBody = characterBody;
	}

	protected virtual void OnCharacterBodyLost(CharacterBody characterBody)
	{
		currentCharacterBody = null;
	}
}
