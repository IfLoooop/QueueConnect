﻿using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System;

/// <summary>
/// Remove empty folders automatically.
/// </summary>
public class RemoveEmptyFolders : UnityEditor.AssetModificationProcessor
{
	private const string kMenuText = "Assets/Remove Empty Folders";
	private static readonly StringBuilder s_Log = new StringBuilder();
	private static readonly List<DirectoryInfo> s_Results = new List<DirectoryInfo>();

	/// <summary>
	/// Raises the initialize on load method event.
	/// </summary>
	[InitializeOnLoadMethod]
	private static void OnInitializeOnLoadMethod()
	{
		EditorApplication.delayCall += () => Valid();
	}

	/// <summary>
	/// Raises the will save assets event.
	/// </summary>
	private static string[] OnWillSaveAssets(string[] paths)
	{
		// If menu is unchecked, do nothing.
		if (!EditorPrefs.GetBool(kMenuText, false))
			return paths;
	
		// Get empty directories in Assets directory
		s_Results.Clear();
		var assetsDir = Application.dataPath + Path.DirectorySeparatorChar;
		GetEmptyDirectories(new DirectoryInfo(assetsDir), s_Results);

		// When empty directories has detected, remove the directory.
		if (0 < s_Results.Count)
		{
			s_Log.Length = 0;
			s_Log.AppendFormat("Remove {0} empty directories as following:\n", s_Results.Count);
			foreach (var d in s_Results)
			{
				s_Log.AppendFormat("- {0}\n", d.FullName.Replace(assetsDir, ""));
				FileUtil.DeleteFileOrDirectory(d.FullName);
				FileUtil.DeleteFileOrDirectory($"{d.FullName}.meta");
			}

			// UNITY BUG: Debug.Log can not set about more than 15000 characters.
			s_Log.Length = Mathf.Min(s_Log.Length, 15000);
			Debug.Log(s_Log.ToString());
			s_Log.Length = 0;

			AssetDatabase.Refresh();
		}
		return paths;
	}

	/// <summary>
	/// Toggles the menu.
	/// </summary>
	[MenuItem(kMenuText)]
	private static void OnClickMenu()
	{
		// Check/Uncheck menu.
		var isChecked = !Menu.GetChecked(kMenuText);
		Menu.SetChecked(kMenuText, isChecked);

		// Save to EditorPrefs.
		EditorPrefs.SetBool(kMenuText, isChecked);

		OnWillSaveAssets(null);
	}
	
	[MenuItem(kMenuText, true)]
	private static bool Valid()
	{
		// Check/Uncheck menu from EditorPrefs.
		Menu.SetChecked(kMenuText, EditorPrefs.GetBool(kMenuText, false));
		return true;
	}



	/// <summary>
	/// Get empty directories.
	/// </summary>
	private static bool GetEmptyDirectories(DirectoryInfo dir, ICollection<DirectoryInfo> results)
	{
		var isEmpty = true;
		try
		{
			bool all = true;
			foreach (var x in dir.GetFiles("*.*"))
			{
				if (x.Extension != ".meta")
				{
					all = false;
					break;
				}
			}

			int count = 0;
			foreach (var x in dir.GetDirectories())
			{
				if (!GetEmptyDirectories(x, results)) count++;
			}

			isEmpty = count == 0	// Are sub directories empty?
			          && all;	// No file exist?
		}
		catch
		{
			// ignored
		}

		// Store empty directory to results.
		if (isEmpty)
			results.Add(dir);
		return isEmpty;
	}
}