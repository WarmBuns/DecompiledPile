using System;
using HG;

namespace RoR2.ContentManagement;

public class LoadStaticContentAsyncArgs
{
	private readonly IProgress<float> progressReceiver;

	public readonly ReadOnlyArray<ContentPackLoadInfo> peerLoadInfos;

	public LoadStaticContentAsyncArgs(IProgress<float> progressReceiver, ReadOnlyArray<ContentPackLoadInfo> peerLoadInfos)
	{
		this.progressReceiver = progressReceiver;
		this.peerLoadInfos = peerLoadInfos;
	}

	public void ReportProgress(float progress)
	{
		progressReceiver.Report(progress);
	}
}
