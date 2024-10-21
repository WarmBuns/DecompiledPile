using System.Collections.Generic;
using System.Text;
using Rewired;
using RoR2.ConVar;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RoR2.UI;

[RequireComponent(typeof(MPEventSystemProvider))]
public class ConsoleWindow : MonoBehaviour
{
	private enum InputFieldState
	{
		Neutral,
		History,
		AutoComplete
	}

	public TMP_InputField inputField;

	public TMP_InputField outputField;

	private TMP_Dropdown autoCompleteDropdown;

	private Console.AutoComplete autoComplete;

	private bool preventAutoCompleteUpdate;

	private bool preventHistoryReset;

	private int historyIndex = -1;

	private readonly StringBuilder stringBuilder = new StringBuilder();

	private const string consoleEnabledDefaultValue = "0";

	private static BoolConVar cvConsoleEnabled = new BoolConVar("console_enabled", ConVarFlags.None, "0", "Enables/Disables the console.");

	public static ConsoleWindow instance { get; private set; }

	private bool autoCompleteInUse
	{
		get
		{
			if ((bool)autoCompleteDropdown)
			{
				return autoCompleteDropdown.IsExpanded;
			}
			return false;
		}
	}

	public void Start()
	{
		GetComponent<MPEventSystemProvider>().eventSystem = MPEventSystemManager.kbmEventSystem;
		if ((bool)outputField.verticalScrollbar)
		{
			outputField.verticalScrollbar.value = 1f;
		}
		outputField.textComponent.gameObject.AddComponent<RectTransformDimensionsChangeEvent>().onRectTransformDimensionsChange += OnOutputFieldRectTransformDimensionsChange;
	}

	private void OnOutputFieldRectTransformDimensionsChange()
	{
		if ((bool)outputField.verticalScrollbar)
		{
			outputField.verticalScrollbar.value = 0f;
			outputField.verticalScrollbar.value = 1f;
		}
	}

	public void OnEnable()
	{
		Console.onLogReceived += OnLogReceived;
		Console.onClear += OnClear;
		RebuildOutput();
		inputField.onSubmit.AddListener(Submit);
		inputField.onValueChanged.AddListener(OnInputFieldValueChanged);
		instance = this;
	}

	public void SubmitInputField()
	{
		inputField.onSubmit.Invoke(inputField.text);
	}

	public void Submit(string text)
	{
		if (!(inputField.text == ""))
		{
			if ((bool)autoCompleteDropdown)
			{
				autoCompleteDropdown.Hide();
			}
			inputField.text = "";
			Console.instance.SubmitCmd(LocalUserManager.GetFirstLocalUser(), text, recordSubmit: true);
			if ((bool)inputField && inputField.IsActive())
			{
				inputField.ActivateInputField();
			}
		}
	}

	private void OnInputFieldValueChanged(string text)
	{
		if (!preventHistoryReset)
		{
			historyIndex = -1;
		}
		if (preventAutoCompleteUpdate)
		{
			return;
		}
		if (text.Length > 0 != (autoComplete != null))
		{
			if (autoComplete != null)
			{
				Object.Destroy(autoCompleteDropdown.gameObject);
				autoComplete = null;
			}
			else
			{
				GameObject gameObject = Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/UI/ConsoleAutoCompleteDropdown"), inputField.transform);
				autoCompleteDropdown = gameObject.GetComponent<TMP_Dropdown>();
				autoComplete = new Console.AutoComplete(Console.instance);
				autoCompleteDropdown.onValueChanged.AddListener(ApplyAutoComplete);
			}
		}
		if (autoComplete != null && autoComplete.SetSearchString(text))
		{
			List<TMP_Dropdown.OptionData> list = new List<TMP_Dropdown.OptionData>();
			List<string> resultsList = autoComplete.resultsList;
			for (int i = 0; i < resultsList.Count; i++)
			{
				list.Add(new TMP_Dropdown.OptionData(resultsList[i]));
			}
			autoCompleteDropdown.options = list;
		}
	}

	public void Update()
	{
		EventSystem eventSystem = MPEventSystemManager.FindEventSystem(ReInput.players.GetPlayer(0));
		if (!eventSystem || !(eventSystem.currentSelectedGameObject == inputField.gameObject))
		{
			return;
		}
		InputFieldState inputFieldState = InputFieldState.Neutral;
		if ((bool)autoCompleteDropdown && autoCompleteInUse)
		{
			inputFieldState = InputFieldState.AutoComplete;
		}
		else if (historyIndex != -1)
		{
			inputFieldState = InputFieldState.History;
		}
		bool keyDown = Input.GetKeyDown(KeyCode.UpArrow);
		bool keyDown2 = Input.GetKeyDown(KeyCode.DownArrow);
		switch (inputFieldState)
		{
		case InputFieldState.Neutral:
			if (keyDown)
			{
				if (Console.userCmdHistory.Count > 0)
				{
					historyIndex = Console.userCmdHistory.Count - 1;
					preventHistoryReset = true;
					inputField.text = Console.userCmdHistory[historyIndex];
					inputField.MoveToEndOfLine(shift: false, ctrl: false);
					preventHistoryReset = false;
				}
			}
			else if (keyDown2 && (bool)autoCompleteDropdown)
			{
				autoCompleteDropdown.Show();
				autoCompleteDropdown.value = 0;
				autoCompleteDropdown.onValueChanged.Invoke(autoCompleteDropdown.value);
			}
			break;
		case InputFieldState.History:
		{
			int num = 0;
			if (keyDown)
			{
				num--;
			}
			if (keyDown2)
			{
				num++;
			}
			if (num != 0)
			{
				historyIndex += num;
				if (historyIndex < 0)
				{
					historyIndex = 0;
				}
				if (historyIndex >= Console.userCmdHistory.Count)
				{
					historyIndex = -1;
					break;
				}
				preventHistoryReset = true;
				inputField.text = Console.userCmdHistory[historyIndex];
				inputField.MoveToEndOfLine(shift: false, ctrl: false);
				preventHistoryReset = false;
			}
			break;
		}
		case InputFieldState.AutoComplete:
			if (keyDown2)
			{
				TMP_Dropdown tMP_Dropdown = autoCompleteDropdown;
				int value = tMP_Dropdown.value + 1;
				tMP_Dropdown.value = value;
			}
			if (keyDown)
			{
				if (autoCompleteDropdown.value > 0)
				{
					TMP_Dropdown tMP_Dropdown2 = autoCompleteDropdown;
					int value = tMP_Dropdown2.value - 1;
					tMP_Dropdown2.value = value;
				}
				else
				{
					autoCompleteDropdown.Hide();
				}
			}
			break;
		}
		eventSystem.SetSelectedGameObject(inputField.gameObject);
	}

	private void ApplyAutoComplete(int optionIndex)
	{
		if ((bool)autoCompleteDropdown && autoCompleteDropdown.options.Count > optionIndex)
		{
			preventAutoCompleteUpdate = true;
			inputField.text = autoCompleteDropdown.options[optionIndex].text;
			inputField.MoveToEndOfLine(shift: false, ctrl: false);
			preventAutoCompleteUpdate = false;
		}
	}

	public void OnDisable()
	{
		Console.onLogReceived -= OnLogReceived;
		Console.onClear -= OnClear;
		inputField.onSubmit.RemoveListener(Submit);
		inputField.onValueChanged.RemoveListener(OnInputFieldValueChanged);
		if (instance == this)
		{
			instance = null;
		}
	}

	private void OnLogReceived(Console.Log log)
	{
		RebuildOutput();
	}

	private void OnClear()
	{
		RebuildOutput();
	}

	private void RebuildOutput()
	{
		float value = 0f;
		if ((bool)outputField.verticalScrollbar)
		{
			value = outputField.verticalScrollbar.value;
		}
		string[] array = new string[Console.logs.Count];
		stringBuilder.Clear();
		for (int i = 0; i < array.Length; i++)
		{
			stringBuilder.AppendLine(Console.logs[i].message);
		}
		outputField.text = stringBuilder.ToString();
		if ((bool)outputField.verticalScrollbar)
		{
			outputField.verticalScrollbar.value = 0f;
			outputField.verticalScrollbar.value = 1f;
			outputField.verticalScrollbar.value = value;
		}
	}

	private static void CheckConsoleKey()
	{
		bool keyDown = Input.GetKeyDown(KeyCode.BackQuote);
		if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftAlt) && keyDown)
		{
			cvConsoleEnabled.SetBool(!cvConsoleEnabled.value);
		}
		if (cvConsoleEnabled.value && keyDown)
		{
			if ((bool)instance)
			{
				Object.Destroy(instance.gameObject);
			}
			else
			{
				Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/UI/ConsoleWindow")).GetComponent<ConsoleWindow>().inputField.ActivateInputField();
			}
		}
		if (Input.GetKeyDown(KeyCode.Escape) && (bool)instance)
		{
			Object.Destroy(instance.gameObject);
		}
	}

	[RuntimeInitializeOnLoadMethod]
	public static void Init()
	{
		RoR2Application.onUpdate += CheckConsoleKey;
	}
}
