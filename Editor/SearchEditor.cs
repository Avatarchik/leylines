﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Exodrifter.NodeGraph
{
	[SerializeField]
	public class SearchEditor
	{
		[SerializeField]
		private bool isOpen = false;
		[SerializeField]
		private Vector2 position;
		[SerializeField]
		private Vector2 spawnPosition;
		[SerializeField]
		private Vector2 size;
		[SerializeField]
		private string searchStr = "";
		[SerializeField]
		private int selected = 0;
		[SerializeField]
		private NodeGraphPolicy policy;

		private Texture2D highlight;

		#region Properties

		public bool IsOpen
		{
			get { return isOpen; }
			set { isOpen = value; }
		}

		public Vector2 Position
		{
			get { return position; }
			set { position = value; }
		}

		public Vector2 Size
		{
			get { return size; }
			set { size = value; }
		}

		#endregion

		private const int SEARCH_PADDING = 4;

		public SearchEditor(Vector2 size)
		{
			this.size = size;
		}

		public void Open
			(Vector2 position, Vector2 spawnPosition, NodeGraphPolicy policy)
		{
			isOpen = true;
			this.position = position;
			this.spawnPosition = spawnPosition;
			this.policy = policy;
		}

		public void Close()
		{
			isOpen = false;
		}

		public void OnGUI(GraphEditor editor)
		{
			if (editor.Target != this) {
				Close();
				return;
			}
			if (!isOpen) {
				return;
			}

			var rect = new Rect();
			rect.size = size;
			rect.center = position + new Vector2(0, rect.size.y / 2);

			GUI.Box(rect, GUIContent.none);

			rect.position += Vector2.one * SEARCH_PADDING;
			rect.size -= Vector2.one * SEARCH_PADDING * 2;
			GUILayout.BeginArea(rect);
			GUILayout.BeginVertical();

			// Detect key events before the text field
			switch(Event.current.type)
			{
				case EventType.KeyDown:
					switch(Event.current.keyCode)
					{
						case KeyCode.UpArrow:
							selected--;
							Event.current.Use();
							break;

						case KeyCode.DownArrow:
							selected++;
							Event.current.Use();
							break;
					}

					break;
			}

			GUILayout.BeginHorizontal();
			GUILayout.Label("Search:", GUILayout.ExpandWidth(false));
			GUI.SetNextControlName("search_field");
			searchStr = GUILayout.TextField(searchStr);
			GUI.FocusControl("search_field");

			var results = (
					from result in policy.SearchItems
					select new
					{
						S = FuzzySearch(searchStr, result.Label),
						R = result
					})
					.Where(x => x.S != int.MinValue)
					.OrderByDescending(x => x.S)
					.Select(x => x.R)
					.ToList();

			selected = Mathf.Clamp(selected, 0, results.Count - 1);

			GUILayout.Label("" + results.Count, GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();
			
			var oldRichText = GUI.skin.label.richText;
			GUI.skin.label.richText = true;

			GUILayout.BeginVertical();
			// Show results
			for (int i = 0; i < results.Count; ++i)
			{
				var result = results[i];

				var oldColor = GUI.skin.label.normal.textColor;
				if (i == selected)
				{
					GUIStyle style = new GUIStyle(GUI.skin.label);
					style.normal.background = GetHighlightTex();

					GUILayout.BeginHorizontal(style);

					GUI.skin.label.normal.textColor = Color.white;
				}
				else
				{
					GUILayout.BeginHorizontal();
				}

				GUILayout.Label("" + FuzzySearch(searchStr, result.Label),
					GUILayout.ExpandWidth(false));
				GUILayout.Label(FuzzyHighlight(searchStr, result.Label),
					GUILayout.ExpandWidth(true));
				GUILayout.EndHorizontal();

				GUI.skin.label.normal.textColor = oldColor;

				if (i == 20) {
					break;
				}
			}
			// No results
			if (results.Count == 0) {
				var oldAlignment = GUI.skin.label.alignment;
				GUI.skin.label.alignment = TextAnchor.LowerCenter;
				GUI.enabled = false;
				GUILayout.Label("<i>No results</i>", GUILayout.MaxHeight(30));
				GUI.enabled = true;
				GUI.skin.label.alignment = oldAlignment;
			}
			GUILayout.EndVertical();

			GUI.skin.label.richText = oldRichText;

			GUILayout.EndVertical();
			GUILayout.EndArea();

			switch (Event.current.type)
			{
				case EventType.MouseDown:
					if (rect.Contains(Event.current.mousePosition))
					{
						editor.Target = this;
						Event.current.Use();
					}
					break;

				case EventType.MouseUp:
					if (rect.Contains(Event.current.mousePosition))
					{
						editor.Target = this;
						Event.current.Use();
					}
					break;

				// Detect key events in the search field
				case EventType.Used:
					switch(Event.current.keyCode)
					{
						case KeyCode.KeypadEnter:
						case KeyCode.Return:
							var node = results[selected].MakeNode();
							editor.AddNode(node, spawnPosition);
							Close();
							break;

						case KeyCode.Escape:
							Close();
							break;
					}
					break;
			}
		}

		private Texture2D GetHighlightTex()
		{
			if (!Util.IsNull(highlight)) {
				return highlight;
			}

			Color[] pixels = new Color[1];
			pixels[0] = new Color32(61, 128, 223, 255);
 
			highlight = new Texture2D(1, 1);
			highlight.SetPixels(pixels);
			highlight.Apply();
 
			return highlight;
		}

		private string FuzzyHighlight(string pattern, string str)
		{
			string ret = "";
			int pi = 0, si = 0;
			while (pi < pattern.Length && si < str.Length)
			{
				if (char.ToLower(pattern[pi]) == char.ToLower(str[si]))
				{
					ret += "<b>" + str[si] + "</b>";
					pi++;
				}
				else
				{
					ret += str[si];
				}
				si++;
			}

			if (si != str.Length)
			{
				ret += str.Substring(si);
			}

			return ret;
		}

		private int FuzzySearch(string pattern, string str,
			int patternStart = 0, int stringStart = 0)
		{
			bool prev = false;
			int bestScore = int.MinValue, score = 0, unmatched = 0;
			int pi = patternStart, si = stringStart;
			while (pi < pattern.Length && si < str.Length)
			{
				// Increase score
				if (char.ToLower(pattern[pi]) == char.ToLower(str[si]))
				{
					// Exhaustive search
					var skipScore = FuzzySearch(pattern, str, pi, si + 1);
					bestScore = Mathf.Max(bestScore, skipScore);

					// Uppercase letters
					if (char.IsUpper(str[si])) {
						score += 20;
					}
					// Seperator bonus
					if ('.' == pattern[pi] || '/' == pattern[pi]) {
						score += 20;
					}
					// Consecutive letters
					if (prev) {
						score += 5;
					}

					prev = true;
					pi++;
				}
				// Decrease score
				else
				{
					// Unmatched letter
					score -= 1;
					// Leading character
					if (pi < 3 && unmatched < 9) {
						unmatched -= 3;
					}

					prev = false;
				}
				si++;
			}

			// Check if the pattern matched
			if (pi != pattern.Length)
			{
				return int.MinValue;
			}

			// Score remaining unmatched letters
			score -= (str.Length - si) * 1;
			// Add the unmatched leading character penalty
			score += unmatched;

			return Mathf.Max(score, bestScore);
		}
	}
}
