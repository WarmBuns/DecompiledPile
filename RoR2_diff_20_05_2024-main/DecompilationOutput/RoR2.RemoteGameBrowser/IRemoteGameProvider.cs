using System;

namespace RoR2.RemoteGameBrowser;

public interface IRemoteGameProvider
{
	event Action onNewInfoAvailable;

	bool RequestRefresh();

	RemoteGameInfo[] GetKnownGames();

	bool IsBusy();
}
