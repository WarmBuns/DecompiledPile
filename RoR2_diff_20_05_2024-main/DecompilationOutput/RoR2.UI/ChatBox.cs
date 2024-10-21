using System;
using System.Collections.ObjectModel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

[RequireComponent(typeof(MPEventSystemLocator))]
public class ChatBox : MonoBehaviour
{
	[Header("Cached Components")]
	public TMP_InputField messagesText;

	public TMP_InputField inputField;

	public Button sendButton;

	public Graphic[] gameplayHiddenGraphics;

	public RectTransform standardChatboxRect;

	public RectTransform expandedChatboxRect;

	public ScrollRect scrollRect;

	[Tooltip("The canvas group to use to fade this chat box. Leave empty for no fading behavior.")]
	public CanvasGroup fadeGroup;

	[Header("Parameters")]
	public bool allowExpandedChatbox;

	public bool deselectAfterSubmitChat;

	public bool deactivateInputFieldIfInactive;

	public float fadeWait = 5f;

	public float fadeDuration = 5f;

	private float fadeTimer;

	[Tooltip("If we're limiting the number of lines to display on this platform, only display this many lines")]
	public int lineCountLimit;

	private MPEventSystemLocator eventSystemLocator;

	private bool _showInput;

	private bool showKeybindTipOnStart => Chat.readOnlyLog.Count == 0;

	private bool showInput
	{
		get
		{
			return _showInput;
		}
		set
		{
			if (_showInput != value)
			{
				_showInput = value;
				RebuildChatRects();
				if ((bool)inputField && deactivateInputFieldIfInactive)
				{
					inputField.gameObject.SetActive(_showInput);
				}
				if ((bool)sendButton)
				{
					sendButton.gameObject.SetActive(_showInput);
				}
				for (int i = 0; i < gameplayHiddenGraphics.Length; i++)
				{
					gameplayHiddenGraphics[i].enabled = _showInput;
				}
				if (_showInput)
				{
					FocusInputField();
				}
				else
				{
					UnfocusInputField();
				}
			}
		}
	}

	private void UpdateFade(float deltaTime)
	{
		fadeTimer -= deltaTime;
		if ((bool)fadeGroup)
		{
			float alpha;
			if (!showInput)
			{
				alpha = ((fadeTimer < 0f) ? 0f : ((!(fadeTimer < fadeDuration)) ? 1f : Mathf.Sqrt(Util.Remap(fadeTimer, fadeDuration, 0f, 1f, 0f))));
			}
			else
			{
				alpha = 1f;
				ResetFadeTimer();
			}
			fadeGroup.alpha = alpha;
		}
	}

	private void ResetFadeTimer()
	{
		fadeTimer = fadeDuration + fadeWait;
	}

	public void SetShowInput(bool value)
	{
		showInput = value;
	}

	public void SubmitChat()
	{
		string text = inputField.text;
		if (text != "")
		{
			inputField.text = "";
			ReadOnlyCollection<NetworkUser> readOnlyLocalPlayersList = NetworkUser.readOnlyLocalPlayersList;
			if (readOnlyLocalPlayersList.Count > 0)
			{
				string text2 = text;
				text2 = text2.Replace("\\", "\\\\");
				text2 = text2.Replace("\"", "\\\"");
				Console.instance.SubmitCmd(readOnlyLocalPlayersList[0], "say \"" + text2 + "\"");
			}
		}
		Debug.LogFormat("SubmitChat() submittedText={0}", text);
		if (deselectAfterSubmitChat)
		{
			showInput = false;
		}
		else
		{
			FocusInputField();
		}
	}

	public void OnInputFieldEndEdit()
	{
	}

	private void Awake()
	{
		eventSystemLocator = GetComponent<MPEventSystemLocator>();
		showInput = true;
		showInput = false;
		Chat.onChatChanged += OnChatChangedHandler;
	}

	private void OnDestroy()
	{
		Chat.onChatChanged -= OnChatChangedHandler;
	}

	private void Start()
	{
		if (showKeybindTipOnStart && !RoR2Application.isInSinglePlayer)
		{
			Chat.AddMessage(Language.GetString("CHAT_KEYBIND_TIP"));
		}
		BuildChat();
		ScrollToBottom();
		inputField.resetOnDeActivation = true;
		Action callback = delegate
		{
			if (PlatformSystems.userManager.CurrentUserHasRestriction(UserManager.PlatformRestriction.TextChat))
			{
				inputField.gameObject.SetActive(value: false);
			}
		};
		if (PlatformSystems.userManager.CurrentUserHasRestriction(UserManager.PlatformRestriction.TextChat, callback))
		{
			inputField.gameObject.SetActive(value: false);
		}
	}

	private void OnEnable()
	{
		BuildChat();
		ScrollToBottom();
		Invoke("ScrollToBottom", 0f);
	}

	private void OnDisable()
	{
	}

	private void OnChatChangedHandler()
	{
		ResetFadeTimer();
		if (base.enabled)
		{
			BuildChat();
			ScrollToBottom();
		}
	}

	public void ScrollToBottom()
	{
		messagesText.verticalScrollbar.value = 0f;
		messagesText.verticalScrollbar.value = 1f;
	}

	private void BuildChat()
	{
		ReadOnlyCollection<string> readOnlyLog = Chat.readOnlyLog;
		int count = readOnlyLog.Count;
		string[] array = new string[count];
		readOnlyLog.CopyTo(array, 0);
		int num = count;
		messagesText.text = string.Join("\n", array, count - num, num);
		RebuildChatRects();
	}

	private void Update()
	{
		if (PlatformSystems.userManager.CurrentUserHasRestriction(UserManager.PlatformRestriction.TextChat))
		{
			return;
		}
		VirtualTextChatLookForInput();
		UpdateFade(Time.deltaTime);
		MPEventSystem eventSystem = eventSystemLocator.eventSystem;
		GameObject gameObject = (eventSystem ? eventSystem.currentSelectedGameObject : null);
		bool flag = false;
		flag = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
		if (!showInput && flag && !(ConsoleWindow.instance != null))
		{
			showInput = true;
		}
		else if (gameObject == inputField.gameObject)
		{
			if (flag)
			{
				if (showInput)
				{
					SubmitChat();
				}
				else if (!gameObject)
				{
					showInput = true;
				}
			}
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				showInput = false;
			}
		}
		else
		{
			showInput = false;
		}
	}

	private void RebuildChatRects()
	{
		RectTransform component = scrollRect.GetComponent<RectTransform>();
		component.SetParent((showInput && allowExpandedChatbox) ? expandedChatboxRect : standardChatboxRect);
		component.offsetMin = Vector2.zero;
		component.offsetMax = Vector2.zero;
		ScrollToBottom();
	}

	private void FocusInputField()
	{
		MPEventSystem eventSystem = eventSystemLocator.eventSystem;
		if ((bool)eventSystem)
		{
			eventSystem.SetSelectedGameObject(inputField.gameObject);
		}
		inputField.ActivateInputField();
		inputField.text = "";
	}

	private void UnfocusInputField()
	{
		MPEventSystem eventSystem = eventSystemLocator.eventSystem;
		if ((bool)eventSystem && eventSystem.currentSelectedGameObject == inputField.gameObject)
		{
			eventSystem.SetSelectedGameObject(null);
		}
		inputField.DeactivateInputField();
	}

	public void VritualTextChatStart()
	{
	}

	public void VirtualTextChatSubmit()
	{
	}

	private void VirtualTextChatLookForInput()
	{
	}
}
