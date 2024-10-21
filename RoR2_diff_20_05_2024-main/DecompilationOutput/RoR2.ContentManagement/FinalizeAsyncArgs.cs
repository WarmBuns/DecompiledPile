using System;
using HG;

namespace RoR2.ContentManagement;

public class FinalizeAsyncArgs
{
	private readonly IProgress<float> progressReceiver;

	public readonly ReadOnlyArray<ContentPackLoadInfo> peerLoadInfos;

	public readonly ReadOnlyContentPack finalContentPack;

	public FinalizeAsyncArgs(IProgress<float> progressReceiver, ReadOnlyArray<ContentPackLoadInfo> peerLoadInfos, ReadOnlyContentPack finalContentPack)
	{
		this.progressReceiver = progressReceiver;
	}

	public void ReportProgress(float progress)
	{
		progressReceiver.Report(progress);
	}
}
