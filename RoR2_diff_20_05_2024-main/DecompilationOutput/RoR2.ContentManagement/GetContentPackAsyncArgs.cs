using System;
using HG;

namespace RoR2.ContentManagement;

public class GetContentPackAsyncArgs
{
	private readonly IProgress<float> progressReceiver;

	public readonly ReadOnlyArray<ContentPackLoadInfo> peerLoadInfos;

	public readonly ContentPack output;

	public readonly int retriesRemaining;

	public GetContentPackAsyncArgs(IProgress<float> progressReceiver, ContentPack output, ReadOnlyArray<ContentPackLoadInfo> peerLoadInfos, int retriesRemaining)
	{
		this.progressReceiver = progressReceiver;
		this.output = output;
		this.peerLoadInfos = peerLoadInfos;
		this.retriesRemaining = retriesRemaining;
	}

	public void ReportProgress(float progress)
	{
		progressReceiver.Report(progress);
	}
}
