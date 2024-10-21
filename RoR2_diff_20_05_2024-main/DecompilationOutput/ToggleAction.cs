using System;

public class ToggleAction : IDisposable
{
	private readonly Action activationAction;

	private readonly Action deactivationAction;

	public bool active { get; private set; }

	public ToggleAction(Action activationAction, Action deactivationAction)
	{
		active = false;
		this.activationAction = activationAction;
		this.deactivationAction = deactivationAction;
	}

	public void SetActive(bool newActive)
	{
		if (active != newActive)
		{
			active = newActive;
			if (active)
			{
				activationAction?.Invoke();
			}
			else
			{
				deactivationAction?.Invoke();
			}
		}
	}

	public void Dispose()
	{
		SetActive(newActive: false);
	}
}
