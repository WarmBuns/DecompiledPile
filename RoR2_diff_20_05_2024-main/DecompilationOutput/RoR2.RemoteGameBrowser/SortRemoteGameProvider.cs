using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace RoR2.RemoteGameBrowser;

public class SortRemoteGameProvider : BaseAsyncRemoteGameProvider
{
	public delegate int RemoteGameProviderComparison(in RemoteGameInfo a, in RemoteGameInfo b);

	public struct Parameters : IEquatable<Parameters>
	{
		public int sorterIndex;

		public bool ascending;

		public bool Equals(Parameters other)
		{
			if (sorterIndex == other.sorterIndex)
			{
				return ascending == other.ascending;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (obj is Parameters other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (sorterIndex * 397) ^ ascending.GetHashCode();
		}
	}

	public class Sorter
	{
		public string nameToken;

		public RemoteGameProviderComparison comparer;
	}

	private readonly IRemoteGameProvider source;

	private Parameters currentParameters;

	public static Sorter[] sorters = new Sorter[5]
	{
		new Sorter
		{
			nameToken = "GAME_BROWSER_SORTER_PING",
			comparer = ComparePing
		},
		new Sorter
		{
			nameToken = "GAME_BROWSER_SORTER_NAME",
			comparer = CompareName
		},
		new Sorter
		{
			nameToken = "GAME_BROWSER_SORTER_PLAYER_COUNT",
			comparer = ComparePlayerCount
		},
		new Sorter
		{
			nameToken = "GAME_BROWSER_SORTER_MAX_PLAYER_COUNT",
			comparer = CompareMaxPlayerCount
		},
		new Sorter
		{
			nameToken = "GAME_BROWSER_SORTER_AVAILABLE_SLOTS",
			comparer = CompareAvailableSlots
		}
	};

	public SortRemoteGameProvider([NotNull] IRemoteGameProvider source)
	{
		this.source = source;
		source.onNewInfoAvailable += OnSourceNewInfoAvailable;
		maxTasks = 1;
	}

	public override void Dispose()
	{
		source.onNewInfoAvailable -= OnSourceNewInfoAvailable;
		base.Dispose();
	}

	private void OnSourceNewInfoAvailable()
	{
		SetDirty();
	}

	protected override Task<RemoteGameInfo[]> CreateTask(CancellationToken cancellationToken)
	{
		Parameters parameters = GetParameters();
		RemoteGameInfo[] input = source.GetKnownGames();
		RemoteGameInfo[] output = new RemoteGameInfo[input.Length];
		Sorter sorter = sorters[parameters.sorterIndex];
		bool ascending = parameters.ascending;
		return new Task<RemoteGameInfo[]>(delegate
		{
			Sort(input, output, sorter, ascending, cancellationToken);
			return output;
		});
	}

	public override bool RequestRefresh()
	{
		return source.RequestRefresh();
	}

	public override bool IsBusy()
	{
		if (!base.IsBusy())
		{
			return source.IsBusy();
		}
		return true;
	}

	private static void Sort(RemoteGameInfo[] input, RemoteGameInfo[] output, Sorter sorter, bool ascending, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		int[] array = new int[input.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = i;
		}
		cancellationToken.ThrowIfCancellationRequested();
		RemoteGameProviderComparison comparer = sorter.comparer;
		cancellationToken.ThrowIfCancellationRequested();
		Array.Sort(array, Compare);
		cancellationToken.ThrowIfCancellationRequested();
		if (ascending)
		{
			for (int j = 0; j < array.Length; j++)
			{
				output[j] = input[array[j]];
			}
		}
		else
		{
			for (int k = 0; k < array.Length; k++)
			{
				output[k] = input[array[array.Length - 1 - k]];
			}
		}
		int Compare(int indexA, int indexB)
		{
			return comparer(in input[indexA], in input[indexB]);
		}
	}

	public Parameters GetParameters()
	{
		lock (this)
		{
			return currentParameters;
		}
	}

	public void SetParameters(Parameters newParameters)
	{
		if (!currentParameters.Equals(newParameters))
		{
			SetParametersInternal(newParameters);
		}
	}

	private void SetParametersInternal(Parameters newParameters)
	{
		lock (this)
		{
			currentParameters = newParameters;
			SetDirty();
		}
	}

	private static int ComparePing(in RemoteGameInfo a, in RemoteGameInfo b)
	{
		int num = a.ping ?? int.MinValue;
		int value = b.ping ?? int.MinValue;
		return num.CompareTo(value);
	}

	private static int ComparePlayerCount(in RemoteGameInfo a, in RemoteGameInfo b)
	{
		return a.lesserPlayerCount.CompareTo(b.lesserPlayerCount);
	}

	private static int CompareMaxPlayerCount(in RemoteGameInfo a, in RemoteGameInfo b)
	{
		return a.lesserPlayerCount.CompareTo(b.lesserPlayerCount);
	}

	private static int CompareName(in RemoteGameInfo a, in RemoteGameInfo b)
	{
		return a.name.CompareTo(b.name);
	}

	public static int CompareAvailableSlots(in RemoteGameInfo a, in RemoteGameInfo b)
	{
		return a.availableSlots.CompareTo(b.availableSlots);
	}
}
