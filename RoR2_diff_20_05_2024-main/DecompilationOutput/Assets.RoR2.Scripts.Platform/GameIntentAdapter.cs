using System;

namespace Assets.RoR2.Scripts.Platform;

public class GameIntentAdapter
{
	public class EventResponse : EventArgs
	{
		public bool wasEventHandledByAnySystem { get; private set; }

		public void EventHasBeenHandled(bool eventHandled)
		{
			wasEventHandledByAnySystem |= eventHandled;
		}
	}

	public virtual bool hasPendingGameIntent => false;

	public event Action<EventResponse> OnGameIntentReceivedEvent;

	public virtual void Initialize()
	{
	}

	public virtual void ClearPendingGameIntent()
	{
	}

	protected void FireGameIntentReceived()
	{
		EventResponse eventResponse = new EventResponse();
		this.OnGameIntentReceivedEvent?.Invoke(eventResponse);
		if (eventResponse.wasEventHandledByAnySystem)
		{
			ClearPendingGameIntent();
		}
	}
}
