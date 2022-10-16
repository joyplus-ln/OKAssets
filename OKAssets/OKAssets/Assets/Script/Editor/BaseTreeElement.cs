using System;
using System.Collections.Generic;
using UnityEngine;


namespace OKAssets.Editor
{

	[Serializable]
	public class BaseTreeElement
	{
		[SerializeField] int m_ID;
		[SerializeField] string m_Name;
		[SerializeField] int m_Depth;

		[NonSerialized] BaseTreeElement m_Parent;
		[NonSerialized] List<BaseTreeElement> m_Children;

		public int depth
		{
			get { return m_Depth; }
			set { m_Depth = value; }
		}

		public BaseTreeElement parent
		{
			get { return m_Parent; }
			set { m_Parent = value; }
		}

		public List<BaseTreeElement> children
		{
			get { return m_Children; }
			set { m_Children = value; }
		}

		public bool hasChildren
		{
			get { return children != null && children.Count > 0; }
		}

		public string name
		{
			get { return m_Name; }
			set { m_Name = value; }
		}

		public int id
		{
			get
			{
				return m_ID;
			}

			set
			{
				m_ID = value;
			}
		}

		public BaseTreeElement()
		{
		}

		public BaseTreeElement(string name, int depth, int id)
		{
			m_Name = name;
			m_Depth = depth;
			m_ID = id;
		}
	}

}


