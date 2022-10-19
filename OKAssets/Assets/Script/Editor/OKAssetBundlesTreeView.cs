using UnityEngine;
using System.Collections;
using UnityEditor.IMGUI.Controls;
using UnityEngine.Assertions;
using System;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace OKAssets.Editor
{
    internal class OKAssetBundlesTreeView : TreeViewWithTreeModel<OKBundlesTreeElement>
    {
        const float kRowHeights = 20f;
        const float kToggleWidth = 18f;

        private HybridBundlesBuildTag tags
        {
            get
            {
                return AssetDatabase.LoadAssetAtPath<HybridBundlesBuildTag>(
                    $"Assets/{OKAssetsConst.OKAssetBundleTagData}");
            }
        }


        static Texture2D[] s_TestIcons =
        {
            EditorGUIUtility.FindTexture("Folder Icon"),
            EditorGUIUtility.FindTexture("AudioSource Icon"),
            EditorGUIUtility.FindTexture("Camera Icon"),
            EditorGUIUtility.FindTexture("Windzone Icon"),
            EditorGUIUtility.FindTexture("GameObject Icon")
        };

        // All columns
        enum MyColumns
        {
            Icon,
            Name,
            Path,
            FolderBundleType,
            FileBundleName,
            TAG,
            Location,
        }

        public enum SortOption
        {
            None,
            Name,
        }

        // Sort options per column
        SortOption[] m_SortOptions =
        {
            SortOption.None,
            SortOption.Name,
            SortOption.None,
            SortOption.None,
            SortOption.None,
            SortOption.None,
            SortOption.None,
        };


        public static void TreeToList(TreeViewItem root, IList<TreeViewItem> result)
        {
            if (root == null)
                throw new NullReferenceException("root");
            if (result == null)
                throw new NullReferenceException("result");

            result.Clear();

            if (root.children == null)
                return;

            Stack<TreeViewItem> stack = new Stack<TreeViewItem>();
            for (int i = root.children.Count - 1; i >= 0; i--)
                stack.Push(root.children[i]);

            while (stack.Count > 0)
            {
                TreeViewItem current = stack.Pop();
                result.Add(current);

                if (current.hasChildren && current.children[0] != null)
                {
                    for (int i = current.children.Count - 1; i >= 0; i--)
                    {
                        stack.Push(current.children[i]);
                    }
                }
            }
        }

        public OKAssetBundlesTreeView(TreeViewState state, MultiColumnHeader multicolumnHeader,
            BaseTreeModel<OKBundlesTreeElement> model) : base(state, multicolumnHeader, model)
        {
            Assert.AreEqual(m_SortOptions.Length, Enum.GetValues(typeof(MyColumns)).Length,
                "Ensure number of sort options are in sync with number of MyColumns enum values");

            // Custom setup
            rowHeight = kRowHeights;
            columnIndexForTreeFoldouts = 1;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            customFoldoutYOffset =
                (kRowHeights - EditorGUIUtility.singleLineHeight) *
                0.5f; // center foldout in the row since we also center content. See RowGUI
            extraSpaceBeforeIconAndLabel = kToggleWidth;
            multicolumnHeader.sortingChanged += OnSortingChanged;

            Reload();
        }


        // Note we We only build the visible rows, only the backend has the full tree information. 
        // The treeview only creates info for the row list.
        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            var rows = base.BuildRows(root);
            SortIfNeeded(root, rows);
            return rows;
        }

        void OnSortingChanged(MultiColumnHeader multiColumnHeader)
        {
            SortIfNeeded(rootItem, GetRows());
        }

        void SortIfNeeded(TreeViewItem root, IList<TreeViewItem> rows)
        {
            if (rows.Count <= 1)
                return;

            if (multiColumnHeader.sortedColumnIndex == -1)
            {
                return; // No column to sort for (just use the order the data are in)
            }

            // Sort the roots of the existing tree items
            SortByMultipleColumns();
            TreeToList(root, rows);
            Repaint();
        }

        void SortByMultipleColumns()
        {
            var sortedColumns = multiColumnHeader.state.sortedColumns;

            if (sortedColumns.Length == 0)
                return;

            var myTypes = rootItem.children.Cast<TreeViewItem<OKBundlesTreeElement>>();
            var orderedQuery = InitialOrder(myTypes, sortedColumns);
            for (int i = 1; i < sortedColumns.Length; i++)
            {
                SortOption sortOption = m_SortOptions[sortedColumns[i]];
                bool ascending = multiColumnHeader.IsSortedAscending(sortedColumns[i]);

                switch (sortOption)
                {
                    case SortOption.Name:
                        orderedQuery = orderedQuery.ThenBy(l => l.data.name, ascending);
                        break;
                }
            }

            rootItem.children = orderedQuery.Cast<TreeViewItem>().ToList();
        }

        IOrderedEnumerable<TreeViewItem<OKBundlesTreeElement>> InitialOrder(
            IEnumerable<TreeViewItem<OKBundlesTreeElement>> myTypes, int[] history)
        {
            SortOption sortOption = m_SortOptions[history[0]];
            bool ascending = multiColumnHeader.IsSortedAscending(history[0]);
            switch (sortOption)
            {
                case SortOption.Name:
                    return myTypes.Order(l => l.data.name, ascending);
                default:
                    Assert.IsTrue(false, "Unhandled enum");
                    break;
            }

            // default
            return myTypes.Order(l => l.data.name, ascending);
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (TreeViewItem<OKBundlesTreeElement>)args.item;

            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), item, (MyColumns)args.GetColumn(i), ref args);
            }
        }

        void CellGUI(Rect cellRect, TreeViewItem<OKBundlesTreeElement> item, MyColumns column, ref RowGUIArgs args)
        {
            // Center cell rect vertically (makes it easier to place controls, icons etc in the cells)
            CenterRectUsingSingleLineHeight(ref cellRect);
            GUIStyle style = new GUIStyle();
            style.alignment = TextAnchor.MiddleCenter;
            //style.normal.textColor = Color.gray;
            switch (column)
            {
                case MyColumns.Icon:
                {
                    if (item.data.isFolder)
                    {
                        GUI.DrawTexture(cellRect, s_TestIcons[0], ScaleMode.ScaleToFit);
                    }
                }
                    break;

                case MyColumns.Name:
                {
                    // Do toggle
                    Rect toggleRect = cellRect;
                    toggleRect.x += GetContentIndent(item);
                    toggleRect.width = kToggleWidth;
                    // Default icon and label
                    args.rowRect = cellRect;
                    base.RowGUI(args);
                }
                    break;
                case MyColumns.Path:
                {
                    if (item.data.isNew)
                    {
                        style.normal.textColor = Color.red;
                    }

                    GUI.Label(cellRect, item.data.path, style);
                    //style.normal.textColor = Color.gray;
                }
                    break;
                case MyColumns.FolderBundleType:
                {
                    if (item.data.isFolder)
                    {
                        GUI.backgroundColor = OKBundlesConsts.BundlePackageColor[item.data.folderBundleType];
                        item.data.folderBundleType = EditorGUI.Popup(cellRect, item.data.folderBundleType,
                            OKBundlesConsts.BundlePackageOptions);
                        /*
                         选择了BundlePackageOptions之后，是单纯的对于当前节点的设置。
                         按住左侧CTRL之后，是设置自身和子节点
                         按住左侧ATL之后，是设置自身和子节点（递归）
                         同时按住左侧CTRL和左侧ATL按照ATL逻辑走，建议不要这样操作
                         */
                        Event e = Event.current;

                        switch (item.data.folderBundleType)
                        {
                            case (int)OKBundlesConsts.BundlePackageType.NONE:
                            case (int)OKBundlesConsts.BundlePackageType.SINGLE:
                            case (int)OKBundlesConsts.BundlePackageType.SINGLE_EXTS:
                            case (int)OKBundlesConsts.BundlePackageType.FOLDER_ALL_IN_ONE:

                                break;
                            case (int)OKBundlesConsts.BundlePackageType.FOLDER_ALL_IN_ONE_RECURSIVELY:
                                if (item.data.hasChildren)
                                {
                                    foreach (OKBundlesTreeElement child in item.data.children)
                                    {
                                        child.SetFolderBundleTypeAndDeepChildren(OKBundlesConsts.BundlePackageType
                                            .NONE);
                                    }
                                }

                                break;
                        }

                        GUI.backgroundColor = Color.white;
                    }
                }
                    break;
                case MyColumns.FileBundleName:
                {
                    GUI.Label(cellRect, item.data.bundleName, style);
                }
                    break;
                case MyColumns.TAG:
                {
                    if (item.data.folderBundleType != (int)OKBundlesConsts.BundlePackageType.NONE)
                    {
                        GUI.backgroundColor = Color.green;
                        int index = 0;
                        for (int i = 0; i < tags.tags.Length; i++)
                        {
                            if (item.data.bundleTag == tags.tags[i])
                            {
                                index = i;
                            }
                        }

                        item.data.bundleTag = tags.tags[EditorGUI.Popup(cellRect, index, tags.tags)];
                        Event e = Event.current;

                        if (item.data.hasChildren)
                        {
                            foreach (OKBundlesTreeElement child in item.data.children)
                            {
                                child.SetFolderBundleTagAndDeepChildren(item.data.bundleTag);
                            }
                        }

                        GUI.backgroundColor = Color.white;
                    }
                }
                    break;
                case MyColumns.Location:
                {
                    if (item.data.folderBundleType != (int)OKBundlesConsts.BundlePackageType.NONE)
                    {
                        GUI.backgroundColor = Color.cyan;
                        item.data.Location = EditorGUI.Popup(cellRect, item.data.Location,
                            OKBundlesConsts.BundleLocationName);
                        Event e = Event.current;

                        if (item.data.hasChildren)
                        {
                            foreach (OKBundlesTreeElement child in item.data.children)
                            {
                                child.SetFolderBundleLocationAndDeepChildren(item.data.Location);
                            }
                        }

                        GUI.backgroundColor = Color.white;
                    }
                }
                    break;
            }
        }

        // Dragging
        //--------
        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            return false;
        }
        // Rename
        //--------

        protected override bool CanRename(TreeViewItem item)
        {
            return false;
        }

        protected override void RenameEnded(RenameEndedArgs args)
        {
            // Set the backend name and reload the tree to reflect the new model
            if (args.acceptedRename)
            {
                var element = treeModel.Find(args.itemID);
                element.name = args.newName;
                Reload();
            }
        }

        protected override Rect GetRenameRect(Rect rowRect, int row, TreeViewItem item)
        {
            Rect cellRect = GetCellRectForTreeFoldouts(rowRect);
            CenterRectUsingSingleLineHeight(ref cellRect);
            return base.GetRenameRect(cellRect, row, item);
        }

        // Misc
        //--------

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState(float treeViewWidth)
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent(EditorGUIUtility.FindTexture("FilterByType"),
                        "Sed hendrerit mi enim, eu iaculis leo tincidunt at."),
                    contextMenuText = "Type",
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = 30,
                    minWidth = 30,
                    maxWidth = 60,
                    autoResize = false,
                    allowToggleVisibility = true,
                    canSort = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Folder"),
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 150,
                    minWidth = 60,
                    autoResize = false,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Path"),
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 300,
                    minWidth = 60,
                    autoResize = false,
                    allowToggleVisibility = false,
                    canSort = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Operation"),
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 300,
                    minWidth = 60,
                    autoResize = false,
                    allowToggleVisibility = false,
                    canSort = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("File Bundle Name"),
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 150,
                    minWidth = 60,
                    autoResize = false,
                    allowToggleVisibility = false,
                    canSort = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Tag"),
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 150,
                    minWidth = 60,
                    autoResize = false,
                    allowToggleVisibility = false,
                    canSort = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Location"),
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 100,
                    minWidth = 60,
                    autoResize = false,
                    allowToggleVisibility = false,
                    canSort = false
                },
            };

            Assert.AreEqual(columns.Length, Enum.GetValues(typeof(MyColumns)).Length,
                "Number of columns should match number of enum values: You probably forgot to update one of them.");

            var state = new MultiColumnHeaderState(columns);
            return state;
        }
    }


    static class MyExtensionMethods
    {
        public static IOrderedEnumerable<T> Order<T, TKey>(this IEnumerable<T> source, Func<T, TKey> selector,
            bool ascending)
        {
            if (ascending)
            {
                return source.OrderBy(selector);
            }
            else
            {
                return source.OrderByDescending(selector);
            }
        }

        public static IOrderedEnumerable<T> ThenBy<T, TKey>(this IOrderedEnumerable<T> source, Func<T, TKey> selector,
            bool ascending)
        {
            if (ascending)
            {
                return source.ThenBy(selector);
            }
            else
            {
                return source.ThenByDescending(selector);
            }
        }
    }

    public class OKBundlesConsts
    {
        public static Color[] BundlePackageColor = new Color[]
        {
            Color.white,
            Color.cyan,
            Color.green,
            Color.yellow,
            Color.magenta,
        };

        public enum BundlePackageType
        {
            NONE, //无
            SINGLE, //目录下每个文件标记为一个单独的Bundle
            FOLDER_ALL_IN_ONE, //目录下所有文件标记为同一个Bundle
            FOLDER_ALL_IN_ONE_RECURSIVELY, //目录下所有文件标记为同一个Bundle（递归）
            SINGLE_EXTS, //目录下每个文件(带有扩展名)标记为一个单独的Bundle
        }

        /// <summary>
        /// 资源保存的位置
        /// </summary>
        public enum BundleLocation
        {
            Local = 0, //无
            OnLine = 1
        }

        public static string[] BundleLocationName = new string[]
        {
            "Local",
            "OnLine",
        };

        public static string[] BundlePackageOptions = new string[]
        {
            "无",
            "目录下每个文件标记为一个单独的Bundle",
            "目录下所有文件标记为同一个Bundle",
            "目录下所有文件标记为同一个Bundle（递归）",
            "目录下每个文件(带有扩展名)标记为一个单独的Bundle",
        };

        public static string GetFolderBundleNameForEditor(string path, OKBundlesTreeElement item)
        {
            if (path.IndexOf('/') == 0)
            {
                path = path.Substring(1);
            }

            string result = "";
            switch ((BundlePackageType)item.folderBundleType)
            {
                case BundlePackageType.NONE:
                    //有可能是递归的 要查找下他的父级
                    OKBundlesTreeElement parent = GetParentUnNoneBundleType(item);
                    if (parent != null)
                    {
                        result = GetFolderBundleNameForEditor(parent.path, parent);
                    }

                    break;
                case BundlePackageType.SINGLE:
                    result = path.Replace('/', '_');
                    result += "_{filename}";
                    break;
                case BundlePackageType.SINGLE_EXTS:
                    result = path.Replace('/', '_');
                    result += "_{filename}_{fileextension}";
                    break;
                case BundlePackageType.FOLDER_ALL_IN_ONE:
                    result = path.Replace('/', '_');
                    break;
                case BundlePackageType.FOLDER_ALL_IN_ONE_RECURSIVELY:
                    result = path.Replace('/', '_');
                    break;
            }

            result = result.ToLower();
            return result;
        }

        public static OKBundlesTreeElement GetParentUnNoneBundleType(OKBundlesTreeElement item)
        {
            if (item.folderBundleType == (int)BundlePackageType.FOLDER_ALL_IN_ONE_RECURSIVELY)
            {
                return item;
            }

            if (item.parent != null)
            {
                return GetParentUnNoneBundleType((OKBundlesTreeElement)item.parent);
            }

            return null;
        }
    }
}