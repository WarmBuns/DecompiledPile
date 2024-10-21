using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HG;

namespace RoR2.RemoteGameBrowser;

public class AggregateRemoteGameProvider : BaseAsyncRemoteGameProvider
{
	private IRemoteGameProvider[] providers = Array.Empty<IRemoteGameProvider>();

	public override void Dispose()
	{
		for (int num = providers.Length - 1; num >= 0; num--)
		{
			OnProviderLost(providers[num]);
		}
		providers = Array.Empty<IRemoteGameProvider>();
		base.Dispose();
	}

	public override bool RequestRefresh()
	{
		IRemoteGameProvider[] array = providers;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].RequestRefresh();
		}
		return true;
	}

	public void AddProvider(IRemoteGameProvider provider)
	{
		ArrayUtils.ArrayAppend(ref providers, in provider);
		OnProviderDiscovered(provider);
		SetDirty();
	}

	public void RemoveProvider(IRemoteGameProvider provider)
	{
		int num = Array.IndexOf(providers, provider);
		if (num != -1)
		{
			RemoveProviderAt(num);
		}
	}

	public void SetProviderAdded(IRemoteGameProvider provider, bool shouldUse)
	{
		int num = Array.IndexOf(providers, provider);
		if (num == -1)
		{
			if (shouldUse)
			{
				AddProvider(provider);
			}
		}
		else if (!shouldUse)
		{
			RemoveProviderAt(num);
		}
	}

	private void RemoveProviderAt(int index)
	{
		OnProviderLost(providers[index]);
		ArrayUtils.ArrayRemoveAtAndResize(ref providers, index);
		SetDirty();
	}

	private void OnProviderDiscovered(IRemoteGameProvider provider)
	{
		provider.onNewInfoAvailable += OnProviderNewInfoAvailable;
	}

	private void OnProviderLost(IRemoteGameProvider provider)
	{
		provider.onNewInfoAvailable -= OnProviderNewInfoAvailable;
	}

	private void OnProviderNewInfoAvailable()
	{
		SetDirty();
	}

	protected override Task<RemoteGameInfo[]> CreateTask(CancellationToken cancellationToken)
	{
		IList<IRemoteGameProvider> providers = this.providers;
		return new Task<RemoteGameInfo[]>(delegate
		{
			cancellationToken.ThrowIfCancellationRequested();
			IEnumerable<RemoteGameInfo>[] array = new IEnumerable<RemoteGameInfo>[providers.Count];
			for (int i = 0; i < providers.Count; i++)
			{
				array[i] = providers[i].GetKnownGames();
			}
			cancellationToken.ThrowIfCancellationRequested();
			int num = 0;
			for (int j = 0; j < providers.Count; j++)
			{
				num += array[j].Count();
			}
			cancellationToken.ThrowIfCancellationRequested();
			RemoteGameInfo[] array2 = new RemoteGameInfo[num];
			int k = 0;
			int num2 = 0;
			for (; k < providers.Count; k++)
			{
				cancellationToken.ThrowIfCancellationRequested();
				RemoteGameInfo[] knownGames = providers[k].GetKnownGames();
				foreach (RemoteGameInfo remoteGameInfo in knownGames)
				{
					array2[num2++] = remoteGameInfo;
				}
			}
			return array2;
		});
	}

	public override bool IsBusy()
	{
		if (!base.IsBusy())
		{
			return IsAnyProviderBusy();
		}
		return true;
		bool IsAnyProviderBusy()
		{
			for (int i = 0; i < providers.Length; i++)
			{
				if (providers[i].IsBusy())
				{
					return true;
				}
			}
			return false;
		}
	}
}
