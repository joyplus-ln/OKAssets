using System;
using System.Collections.Generic;
using UnityEngine;


namespace OKAssets.Editor
{

	[Serializable]
	public class OKTreeElement
	{
		[SerializeField] int m_ID;
		[SerializeField] string m_Name;
		[SerializeField] int m_Depth;

		[NonSerialized] OKTreeElement m_Parent;
		[NonSerialized] List<OKTreeElement> m_Children;

		public int depth
		{
			get { return m_Depth; }
			set { m_Depth = value; }
		}

		public OKTreeElement parent
		{
			get { return m_Parent; }
			set { m_Parent = value; }
		}

		public List<OKTreeElement> children
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

		public OKTreeElement()
		{
		}

		public OKTreeElement(string name, int depth, int id)
		{
			m_Name = name;
			m_Depth = depth;
			m_ID = id;
		}
	}

}


