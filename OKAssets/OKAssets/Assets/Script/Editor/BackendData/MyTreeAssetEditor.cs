using System.Collections.Generic;
using UnityEngine;
using UnityEditor.IMGUI.Controls;
using UnityEditor;

namespace OKAssets.Editor
{

	[CustomEditor(typeof(MyTreeAsset))]
	public class MyTreeAssetEditor : UnityEditor.Editor
	{
		MyTreeView m_TreeView;
		SearchField m_SearchField;
		const string kSessionStateKeyPrefix = "TVS";

		MyTreeAsset asset
		{
			get { return (MyTreeAsset)target; }
		}

		void OnEnable()
		{
			Undo.undoRedoPerformed += OnUndoRedoPerformed;

			var treeViewState = new TreeViewState();
			var jsonState = SessionState.GetString(kSessionStateKeyPrefix + asset.GetInstanceID(), "");
			if (!string.IsNullOrEmpty(jsonState))
				JsonUtility.FromJsonOverwrite(jsonState, treeViewState);
			var treeModel = new BaseTreeModel<HybridBundlesTreeElement>(asset.treeElements);
			if (!treeModel.HasData())
			{
				return;
			}
			m_TreeView = new MyTreeView(treeViewState, treeModel);
			m_TreeView.beforeDroppingDraggedItems += OnBeforeDroppingDraggedItems;
			m_TreeView.Reload();

			m_SearchField = new SearchField();

			m_SearchField.downOrUpArrowKeyPressed += m_TreeView.SetFocusAndEnsureSelectedItem;
		}


		void OnDisable()
		{
			Undo.undoRedoPerformed -= OnUndoRedoPerformed;
			if (m_TreeView == null)
				return;
			SessionState.SetString(kSessionStateKeyPrefix + asset.GetInstanceID(), JsonUtility.ToJson(m_TreeView.state));
		}

		void OnUndoRedoPerformed()
		{
			if (m_TreeView != null)
			{
				m_TreeView.treeModel.SetData(asset.treeElements);
				m_TreeView.Reload();
			}
		}

		void OnBeforeDroppingDraggedItems(IList<TreeViewItem> draggedRows)
		{
			Undo.RecordObject(asset, string.Format("Moving {0} Item{1}", draggedRows.Count, draggedRows.Count > 1 ? "s" : ""));
		}

		public override void OnInspectorGUI()
		{
			if (m_TreeView == null)
				return;
			GUILayout.Space(5f);
			ToolBar();
			GUILayout.Space(3f);

			const float topToolbarHeight = 20f;
			const float spacing = 2f;
			float totalHeight = m_TreeView.totalHeight + topToolbarHeight + 2 * spacing;
			Rect rect = GUILayoutUtility.GetRect(0, 10000, 0, totalHeight);
			Rect toolbarRect = new Rect(rect.x, rect.y, rect.width, topToolbarHeight);
			Rect multiColumnTreeViewRect = new Rect(rect.x, rect.y + topToolbarHeight + spacing, rect.width, rect.height - topToolbarHeight - 2 * spacing);
			SearchBar(toolbarRect);
			DoTreeView(multiColumnTreeViewRect);
		}

		void SearchBar(Rect rect)
		{
			m_TreeView.searchString = m_SearchField.OnGUI(rect, m_TreeView.searchString);
		}

		void DoTreeView(Rect rect)
		{
			m_TreeView.OnGUI(rect);
		}

		void ToolBar()
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				var style = "miniButton";
				if (GUILayout.Button("Expand All", style))
				{
					m_TreeView.ExpandAll();
				}

				if (GUILayout.Button("Collapse All", style))
				{
					m_TreeView.CollapseAll();
				}

				GUILayout.FlexibleSpace();
			}
		}


		class MyTreeView : TreeViewWithTreeModel<HybridBundlesTreeElement>
		{
			public MyTreeView(TreeViewState state, BaseTreeModel<HybridBundlesTreeElement> model)
				: base(state, model)
			{
				showBorder = true;
				showAlternatingRowBackgrounds = true;
			}
		}
	}
}
