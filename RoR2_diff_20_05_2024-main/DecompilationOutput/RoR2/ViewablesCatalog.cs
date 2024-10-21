using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace RoR2;

public static class ViewablesCatalog
{
	public class Node
	{
		public readonly string name;

		public readonly bool isFolder;

		private readonly List<Node> _children = new List<Node>();

		public ReadOnlyCollection<Node> children;

		private string _fullName;

		private bool fullNameDirty = true;

		public Func<UserProfile, bool> shouldShowUnviewed;

		public Node parent { get; private set; }

		public string fullName
		{
			get
			{
				if (fullNameDirty)
				{
					GenerateFullName();
				}
				return _fullName;
			}
		}

		public Node(string name, bool isFolder, Node parent = null)
		{
			this.name = name;
			this.isFolder = isFolder;
			shouldShowUnviewed = DefaultShouldShowUnviewedTest;
			children = _children.AsReadOnly();
			SetParent(parent);
		}

		public void SetParent(Node newParent)
		{
			if (parent != newParent)
			{
				parent?._children.Remove(this);
				parent = newParent;
				parent?._children.Add(this);
				fullNameDirty = true;
			}
		}

		private void GenerateFullName()
		{
			string text = name;
			if (parent != null)
			{
				text = parent.fullName + text;
			}
			if (isFolder)
			{
				text += "/";
			}
			_fullName = text;
			fullNameDirty = false;
		}

		public bool DefaultShouldShowUnviewedTest(UserProfile userProfile)
		{
			if (!isFolder && userProfile.HasViewedViewable(fullName))
			{
				return false;
			}
			foreach (Node child in children)
			{
				if (child.shouldShowUnviewed(userProfile))
				{
					return true;
				}
			}
			return false;
		}

		public IEnumerable<Node> Descendants()
		{
			yield return this;
			foreach (Node child in _children)
			{
				foreach (Node item in child.Descendants())
				{
					yield return item;
				}
			}
		}
	}

	private static readonly Node rootNode = new Node("", isFolder: true);

	private static readonly Dictionary<string, Node> fullNameToNodeMap = new Dictionary<string, Node>();

	public static void AddNodeToRoot(Node node)
	{
		node.SetParent(rootNode);
		foreach (Node item in node.Descendants())
		{
			if (fullNameToNodeMap.ContainsKey(item.fullName))
			{
				Debug.LogFormat("Tried to add duplicate node {0}", item.fullName);
			}
			else
			{
				fullNameToNodeMap.Add(item.fullName, item);
			}
		}
	}

	public static Node FindNode(string fullName)
	{
		if (fullNameToNodeMap.TryGetValue(fullName, out var value))
		{
			return value;
		}
		return null;
	}

	[ConCommand(commandName = "viewables_list", flags = ConVarFlags.None, helpText = "Displays the full names of all viewables.")]
	private static void CCViewablesList(ConCommandArgs args)
	{
	}

	[ConCommand(commandName = "viewables_list_unviewed", flags = ConVarFlags.None, helpText = "Displays the full names of all unviewed viewables.")]
	private static void CCViewablesListUnviewed(ConCommandArgs args)
	{
		_ = args.GetSenderLocalUser().userProfile;
	}

	[ConCommand(commandName = "viewables_clear_viewed", flags = ConVarFlags.None, helpText = "Clears all viewed viewables")]
	private static void CCViewablesClearViewed(ConCommandArgs args)
	{
		args.GetSenderLocalUser().userProfile.ClearAllViewablesAsViewed();
	}
}
