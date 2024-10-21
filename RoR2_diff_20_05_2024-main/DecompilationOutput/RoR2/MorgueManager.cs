using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using HG;
using RoR2.ConVar;
using UnityEngine;
using Zio;

namespace RoR2;

public static class MorgueManager
{
	private struct HistoryFileInfo
	{
		public UPath path;

		public DateTime lastModified;

		public FileEntry fileEntry;

		public RunReport LoadRunReport()
		{
			RunReport value = new RunReport();
			HGXml.FromXml(XDocument.Parse(fileEntry.ReadAllText()).Root, ref value);
			return value;
		}

		public void Delete()
		{
			fileEntry.Delete();
		}
	}

	private static readonly IntConVar morgueHistoryLimit = new IntConVar("morgue_history_limit", ConVarFlags.Archive, "30", "How many non-favorited entries we can store in the morgue before old ones are deleted.");

	private static readonly UPath historyDirectory = new UPath("/RunReports/History/");

	private static IFileSystem storage => RoR2Application.fileSystem;

	[SystemInitializer(new Type[] { })]
	private static void Init()
	{
		Run.onClientGameOverGlobal += OnClientGameOverGlobal;
	}

	private static void OnClientGameOverGlobal(Run run, RunReport runReport)
	{
		AddRunReportToHistory(runReport);
	}

	private static int CompareHistoryFileLastModified(HistoryFileInfo a, HistoryFileInfo b)
	{
		return b.lastModified.CompareTo(a.lastModified);
	}

	private static void AddRunReportToHistory(RunReport runReport)
	{
		EnforceHistoryLimit();
		StringBuilder stringBuilder = HG.StringBuilderPool.RentStringBuilder();
		string fileName = stringBuilder.Append(runReport.runGuid).Append(".xml").ToString();
		HG.StringBuilderPool.ReturnStringBuilder(stringBuilder);
		foreach (LocalUser readOnlyLocalUsers in LocalUserManager.readOnlyLocalUsersList)
		{
			if (!runReport.HasUserParticipated(readOnlyLocalUsers))
			{
				continue;
			}
			using Stream stream = GetHistoryFile(fileName, readOnlyLocalUsers.userProfile.saveID);
			if (stream != null)
			{
				XDocument xDocument = new XDocument();
				xDocument.Add(HGXml.ToXml("RunReport", RunReport.GetLocalUserRunReport(runReport, readOnlyLocalUsers)));
				xDocument.Save(stream);
				stream.Flush();
				stream.Dispose();
			}
		}
	}

	private static void EnforceHistoryLimit()
	{
		List<HistoryFileInfo> list = CollectionPool<HistoryFileInfo, List<HistoryFileInfo>>.RentCollection();
		GetHistoryFiles(list);
		list.Sort(CompareHistoryFileLastModified);
		int num = list.Count - 1;
		int num2 = Math.Max(morgueHistoryLimit.value, 0);
		while (num >= num2)
		{
			list[num].Delete();
			num--;
		}
		CollectionPool<HistoryFileInfo, List<HistoryFileInfo>>.ReturnCollection(list);
	}

	public static void LoadHistoryRunReports(List<RunReport> dest)
	{
		List<HistoryFileInfo> list = CollectionPool<HistoryFileInfo, List<HistoryFileInfo>>.RentCollection();
		GetHistoryFiles(list);
		list.Sort(CompareHistoryFileLastModified);
		for (int i = 0; i < list.Count; i++)
		{
			HistoryFileInfo historyFileInfo = list[i];
			try
			{
				Debug.LogFormat("loading RunReport {0}", historyFileInfo.path);
				RunReport item = historyFileInfo.LoadRunReport();
				dest.Add(item);
			}
			catch (Exception ex)
			{
				Debug.LogFormat("Could not load RunReport \"{0}\": {1}", historyFileInfo, ex);
			}
		}
		CollectionPool<HistoryFileInfo, List<HistoryFileInfo>>.ReturnCollection(list);
	}

	private static Stream GetHistoryFile(string fileName)
	{
		UPath path = historyDirectory / fileName;
		storage.CreateDirectory(historyDirectory);
		return storage.OpenFile(path, FileMode.Create, FileAccess.Write);
	}

	private static Stream GetHistoryFile(string fileName, ulong inSaveID)
	{
		return GetHistoryFile(fileName);
	}

	private static void GetHistoryFiles(List<HistoryFileInfo> dest)
	{
		foreach (UPath item in storage.DirectoryExists(historyDirectory) ? storage.EnumeratePaths(historyDirectory) : Enumerable.Empty<UPath>())
		{
			if (storage.FileExists(item) && item.GetExtensionWithDot().Equals(".xml", StringComparison.OrdinalIgnoreCase))
			{
				dest.Add(new HistoryFileInfo
				{
					path = item,
					lastModified = storage.GetLastWriteTime(item),
					fileEntry = storage.GetFileEntry(item)
				});
			}
		}
	}
}
