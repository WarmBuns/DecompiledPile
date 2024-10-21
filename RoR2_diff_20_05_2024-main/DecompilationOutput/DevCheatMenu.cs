using System;
using System.Linq;
using System.Text;
using RoR2;
using RoR2.ContentManagement;
using RoR2.UI;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(MPEventSystemLocator))]
public class DevCheatMenu : MonoBehaviour
{
	public TextMeshProUGUI enemyName;

	public TextMeshProUGUI teamText;

	public TextMeshProUGUI affixText;

	public TextMeshProUGUI spawnEnemyText;

	public TextMeshProUGUI toggleSpawningText;

	public TextMeshProUGUI miscCommandLabel;

	public TMP_Dropdown masterNameDropdown;

	public TMP_Dropdown enemiesAmountDropdown;

	public TMP_Dropdown itemsNamesDropdown;

	public TMP_Dropdown itemsAmountDropdown;

	public TMP_Dropdown stageListDropdown;

	private string enemyMasterName;

	private string teamIndex;

	private string equipmentIndex;

	private string enemyMasterNameTeamIndexEquipmentIndex;

	private bool spawnEnemyArgsDirty;

	private readonly StringBuilder miscCommand = new StringBuilder();

	private NetworkUser localUser;

	private MPEventSystem eventSystem;

	private bool isUpper;

	private void Awake()
	{
		Close();
	}

	private void SetMonsterTabText()
	{
		spawnEnemyText.SetText(enemyMasterNameTeamIndexEquipmentIndex);
		enemyName.SetText(enemyMasterName);
		teamText.SetText(teamIndex);
		affixText.SetText(equipmentIndex);
		toggleSpawningText.SetText(CombatDirector.cvDirectorCombatDisable.value ? "Spawning DISABLED" : "Spawning ENABLED");
	}

	private void InitializeMasterDropdown()
	{
		masterNameDropdown.options = MasterCatalog.allMasters.Select((CharacterMaster master) => new TMP_Dropdown.OptionData(master.gameObject.name)).ToList();
	}

	private void InitializeStageListDropdown()
	{
		stageListDropdown.options = SceneCatalog.allSceneDefs.Select((SceneDef scene) => new TMP_Dropdown.OptionData(scene.baseSceneName)).ToList();
	}

	public void EnableSpawning()
	{
	}

	public void DisableSpawning()
	{
	}

	public void SpawnEnemy()
	{
		if (!CombatDirector.cvDirectorCombatDisable.value && !string.IsNullOrEmpty(enemyMasterName))
		{
			if (spawnEnemyArgsDirty)
			{
				enemyMasterNameTeamIndexEquipmentIndex = string.Join(" ", "create_master", enemyMasterName, teamIndex, equipmentIndex);
				spawnEnemyArgsDirty = false;
			}
			int num = int.Parse(enemiesAmountDropdown.options[Mathf.Min(enemiesAmountDropdown.options.Count - 1, enemiesAmountDropdown.value)].text);
			for (int i = 0; i < num; i++)
			{
				RoR2.Console.instance.SubmitCmd(NetworkUser.readOnlyLocalPlayersList[0], enemyMasterNameTeamIndexEquipmentIndex, recordSubmit: true);
				spawnEnemyText.SetText(enemyMasterNameTeamIndexEquipmentIndex);
			}
		}
	}

	public void SetEnemyMasterName()
	{
		string text = masterNameDropdown.options[Mathf.Min(masterNameDropdown.options.Count - 1, masterNameDropdown.value)].text;
		if (string.IsNullOrEmpty(text) || enemyMasterName == text || MasterCatalog.FindMasterIndex(text) == MasterCatalog.MasterIndex.none)
		{
			Debug.LogWarning("Invalid newEnemyMaster: " + text);
			return;
		}
		enemyMasterName = text;
		spawnEnemyArgsDirty = true;
		enemyName.SetText(enemyMasterName);
	}

	public void SetSceneTravelName()
	{
		string text = stageListDropdown.options[Mathf.Min(stageListDropdown.options.Count - 1, stageListDropdown.value)].text;
		if (!string.IsNullOrEmpty(text))
		{
			RoR2.Console.instance.SubmitCmd(null, "set_scene " + text);
		}
	}

	public void SetTeamIndex(string newTeamIndex)
	{
		if (string.IsNullOrEmpty(newTeamIndex) || teamIndex == newTeamIndex || !Enum.TryParse<TeamIndex>(newTeamIndex, ignoreCase: true, out var _))
		{
			Debug.LogWarning("Invalid newTeamIndex: " + newTeamIndex);
			return;
		}
		teamIndex = newTeamIndex;
		spawnEnemyArgsDirty = true;
		teamText.SetText(teamIndex);
	}

	public void SetCurrentEquipment(string newEquipment)
	{
		if (string.IsNullOrEmpty(newEquipment) || equipmentIndex == newEquipment || EquipmentCatalog.FindEquipmentIndex(newEquipment) == EquipmentIndex.None)
		{
			Debug.LogWarning("Invalid newEquipment: " + newEquipment);
			return;
		}
		equipmentIndex = newEquipment;
		spawnEnemyArgsDirty = true;
		affixText.SetText(equipmentIndex);
	}

	public void SetUppercase()
	{
		isUpper = true;
	}

	public void SetLowercase()
	{
		isUpper = false;
	}

	public void BackspaceMiscCommand()
	{
		miscCommand.Remove(miscCommand.Length - 1, 1);
		miscCommandLabel.SetText(miscCommand);
	}

	public void AppendMiscCommand(string stringToAdd)
	{
		miscCommand.Append(isUpper ? stringToAdd.ToUpper() : stringToAdd.ToLower());
		miscCommandLabel.SetText(miscCommand);
	}

	public void SubmitMiscCommand()
	{
		string text = miscCommand.ToString();
		if (string.IsNullOrEmpty(text))
		{
			string text2 = "Invalid command sequence: " + text + ". Enter a valid command sequence before submitting a command.";
			Debug.LogWarning(text2);
			miscCommandLabel.SetText(text2);
			return;
		}
		if (localUser == null && NetworkUser.readOnlyLocalPlayersList != null && NetworkUser.readOnlyLocalPlayersList.Count > 0)
		{
			localUser = NetworkUser.readOnlyLocalPlayersList[0];
		}
		RoR2.Console.instance.SubmitCmd(localUser, text, recordSubmit: true);
	}

	public void ClearMiscCommand()
	{
		miscCommand.Clear();
		miscCommandLabel.SetText(miscCommand);
	}

	public void AttemptAlternateClick()
	{
		if ((bool)eventSystem.currentSelectedButton)
		{
			AlternateButtonEvents component = eventSystem.currentSelectedButton.GetComponent<AlternateButtonEvents>();
			if ((bool)component)
			{
				component.InvokeAltClick();
			}
		}
	}

	public void AttemptTertiaryClick()
	{
		if ((bool)eventSystem.currentSelectedButton)
		{
			AlternateButtonEvents component = eventSystem.currentSelectedButton.GetComponent<AlternateButtonEvents>();
			if ((bool)component)
			{
				component.InvokeTertiaryClick();
			}
		}
	}

	private void InitializeItemsDropdown()
	{
		itemsNamesDropdown.options = ContentManager._itemDefs.Select((ItemDef itemDef) => new TMP_Dropdown.OptionData(itemDef.name)).ToList();
	}

	public void GiveItem()
	{
		string text = itemsNamesDropdown.options[Mathf.Min(itemsNamesDropdown.options.Count - 1, itemsNamesDropdown.value)].text;
		int num = int.Parse(itemsAmountDropdown.options[Mathf.Min(itemsAmountDropdown.options.Count - 1, itemsAmountDropdown.value)].text);
		RoR2.Console.instance.SubmitCmd(NetworkUser.readOnlyLocalPlayersList[0], $"give_item {text} {num}", recordSubmit: true);
	}

	[ConCommand(commandName = "enemies_are_gods", flags = ConVarFlags.None, helpText = "EnemiesAreGods")]
	private static void EnemiesAreGods(ConCommandArgs args)
	{
		foreach (TeamComponent teamMember in TeamComponent.GetTeamMembers(TeamIndex.Monster))
		{
			HealthComponent component = teamMember.GetComponent<HealthComponent>();
			component.godMode = !component.godMode;
		}
	}

	public void StartLog()
	{
	}

	public void StopLog()
	{
	}

	public void Close()
	{
		UnityEngine.Object.Destroy(base.gameObject);
	}
}
