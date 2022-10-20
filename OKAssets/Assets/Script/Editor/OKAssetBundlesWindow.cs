using UnityEngine;
using System.Collections;
using UnityEditor;
using System;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using System.IO;

namespace OKAssets.Editor
{
	class OKAssetBundlesWindow : EditorWindow
	{

		[NonSerialized] bool m_Initialized;
		[SerializeField] TreeViewState m_TreeViewState; // Serialized in the window layout file so it survives assembly reloading
		[SerializeField] MultiColumnHeaderState m_MultiColumnHeaderState;
		SearchField m_SearchField;
		OKAssetBundlesTreeView m_TreeView;
		OKTreeAsset mOkTreeAsset;

		[MenuItem("OKAssets/OpenConfigWindow")]
		public static OKAssetBundlesWindow GetWindow()
		{
			var window = GetWindow<OKAssetBundlesWindow>();
			window.titleContent = new GUIContent("OKAssetsResWindows");
			window.Focus();
			window.Repaint();
			return window;
		}

		Rect multiColumnTreeViewRect
		{
			get { return new Rect(20, 30, position.width - 40, position.height - 60); }
		}


		Rect toolbarRect
		{
			get { return new Rect(20f, 10f, position.width - 40f, 20f); }
		}

		Rect bottomToolbarRect
		{
			get { return new Rect(20f, position.height - 26f, position.width - 40f, 22f); }
		}
		public OKAssetBundlesTreeView treeView
		{
			get { return m_TreeView; }
		}
		void InitIfNeeded()
		{
			if (!m_Initialized)
			{
				if (m_TreeViewState == null)
					m_TreeViewState = new TreeViewState();

				bool firstInit = m_MultiColumnHeaderState == null;
				var headerState = OKAssetBundlesTreeView.CreateDefaultMultiColumnHeaderState(multiColumnTreeViewRect.width);
				if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_MultiColumnHeaderState, headerState))
					MultiColumnHeaderState.OverwriteSerializedFields(m_MultiColumnHeaderState, headerState);
				m_MultiColumnHeaderState = headerState;

				var multiColumnHeader = new MyMultiColumnHeader(headerState);
				if (firstInit)
					multiColumnHeader.ResizeToFit();

				var treeModel = new OKBaseTreeModel<OKBundlesTreeElement>(GetData());

				m_TreeView = new OKAssetBundlesTreeView(m_TreeViewState, multiColumnHeader, treeModel);

				m_SearchField = new SearchField();
				m_SearchField.downOrUpArrowKeyPressed += m_TreeView.SetFocusAndEnsureSelectedItem;
				//先自动保存一下 
				SaveData();

				m_Initialized = true;

			}
		}

		IList<OKBundlesTreeElement> GetData()
		{
			if (mOkTreeAsset == null)
			{
				OKBundlesInitScript.CreatOkAssetTreeData();
				mOkTreeAsset = AssetDatabase.LoadMainAssetAtPath(OKAssetsConst.OKAssetBundleData) as OKTreeAsset;
			}
			if (mOkTreeAsset != null)
			{
				mOkTreeAsset.UpdateData();
				return mOkTreeAsset.treeElements;
			}
			return null;
		}

		void SaveData()
		{
			OKTreeAsset dataScript = ScriptableObject.CreateInstance<OKTreeAsset>();
			dataScript.treeElements = mOkTreeAsset.treeElements;
			OKBundlesInitScript.CreatOkAssetTreeData(true,dataScript);
		}

		void OnGUI()
		{
			InitIfNeeded();

			SearchBar(toolbarRect);
			DoTreeView(multiColumnTreeViewRect);
			BottomToolBar(bottomToolbarRect);
		}

		void SearchBar(Rect rect)
		{
			treeView.searchString = m_SearchField.OnGUI(rect, treeView.searchString);
		}

		void DoTreeView(Rect rect)
		{
			m_TreeView.OnGUI(rect);
		}

		void BottomToolBar(Rect rect)
		{
			GUILayout.BeginArea(rect);

			using (new EditorGUILayout.HorizontalScope())
			{

				if (GUILayout.Button("Expand All"))
				{
					treeView.ExpandAll();
				}

				if (GUILayout.Button("Collapse All"))
				{
					treeView.CollapseAll();
				}

				GUILayout.FlexibleSpace();

				GUILayout.Label(mOkTreeAsset != null ? AssetDatabase.GetAssetPath(mOkTreeAsset) : string.Empty);

				GUILayout.FlexibleSpace();

				GUILayout.Space(10);

				if (GUILayout.Button("Save"))
				{
					SaveData();
				}
			}

			GUILayout.EndArea();
		}
	}

}


internal class MyMultiColumnHeader : MultiColumnHeader
{
	Mode m_Mode;

	public enum Mode
	{
		DefaultHeader,
		MinimumHeaderWithoutSorting
	}

	public MyMultiColumnHeader(MultiColumnHeaderState state)
		: base(state)
	{
		mode = Mode.MinimumHeaderWithoutSorting;
	}

	public Mode mode
	{
		get
		{
			return m_Mode;
		}
		set
		{
			m_Mode = value;
			switch (m_Mode)
			{
				case Mode.DefaultHeader:
					canSort = true;
					height = DefaultGUI.defaultHeight;
					break;
				case Mode.MinimumHeaderWithoutSorting:
					canSort = false;
					height = DefaultGUI.minimumHeight;
					break;
			}
		}
	}
}
