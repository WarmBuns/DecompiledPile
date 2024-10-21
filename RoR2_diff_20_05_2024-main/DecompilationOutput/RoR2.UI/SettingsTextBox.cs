using TMPro;

namespace RoR2.UI;

public class SettingsTextBox : BaseSettingsControl
{
	public TMP_InputField textbox;

	protected new void OnEnable()
	{
		base.OnEnable();
		textbox?.onValueChanged?.AddListener(OnTextBoxValueChanged);
	}

	protected void OnDisable()
	{
		textbox?.onValueChanged?.RemoveListener(OnTextBoxValueChanged);
	}

	private void OnTextBoxValueChanged(string newValue)
	{
		SubmitSetting(newValue);
	}

	protected override void OnUpdateControls()
	{
		string currentValue = GetCurrentValue();
		textbox?.SetTextWithoutNotify(currentValue);
	}
}
