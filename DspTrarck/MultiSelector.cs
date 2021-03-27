using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace DspTrarck
{
	public class MultiSelector
	{
		private bool m_EnableSelect;
		private bool m_SelectStart;
		private Vector3 m_MouseStartPosition;
		private Vector3 m_MouseEndPosition;
		private Rect m_SelectRange;
		private List<EntityData> m_SelectEntities;

		private Texture2D m_BlankTexture;
		private Color m_LineColor = Color.green;

		public bool enableSelect
		{
			get
			{
				return m_EnableSelect;
			}
			set
			{
				m_EnableSelect = value;
				m_SelectStart = false;
			}
		}

		public bool selectStarted
		{
			get
			{
				return m_SelectStart;
			}
		}

		public Vector3 startPosition
		{
			get
			{
				return m_MouseStartPosition;
			}
		}

		public Vector3 endPosition
		{
			get
			{
				return m_MouseEndPosition;
			}
		}

		public Rect selectRange
		{
			get
			{
				return m_SelectRange;
			}
		}

		public List<EntityData> selectEntities
		{
			get
			{
				return m_SelectEntities;
			}
		}

		public void Init()
		{
			m_BlankTexture = CreateDummyTex();
		}

		public void ClearSelectData()
		{
			if (m_SelectEntities != null)
			{
				m_SelectEntities.Clear();
			}
			m_MouseStartPosition = Vector3.zero;
			m_MouseEndPosition = Vector3.zero;
			m_SelectRange = Rect.zero;
		}

		public void Update()
		{
			if (m_EnableSelect)
			{
				if (Input.GetMouseButtonDown(0))
				{
					ClearSelectData();
					m_SelectStart = true;
					m_MouseStartPosition = Input.mousePosition;
				}

				if (Input.GetMouseButtonUp(0))
				{
					m_SelectStart = false;
					m_MouseEndPosition = Input.mousePosition;
					CalcSelectRange();
					CalcSelectEnties();
					ShowDebugInfo();
				}

				if (m_SelectStart)
				{
					m_MouseEndPosition = Input.mousePosition;
				}
			}
		}

		public void OnGUI()
		{
			if (!m_SelectStart)
			{
				return;
			}

			Rect r = new Rect(m_MouseStartPosition.x, Screen.height - m_MouseStartPosition.y, m_MouseEndPosition.x - m_MouseStartPosition.x, m_MouseStartPosition.y - m_MouseEndPosition.y);
			DrawOutline(r, m_BlankTexture, m_LineColor);
		}

		private void CalcSelectRange()
		{
			float xMin = Mathf.Min(m_MouseStartPosition.x, m_MouseEndPosition.x);
			float xMax = Mathf.Max(m_MouseStartPosition.x, m_MouseEndPosition.x);

			float yMin = Mathf.Min(m_MouseStartPosition.y, m_MouseEndPosition.y);
			float yMax = Mathf.Max(m_MouseStartPosition.y, m_MouseEndPosition.y);

			m_SelectRange = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
		}

		private bool IsInSelectRange(Vector3 pos)
		{
			return m_SelectRange.Contains(new Vector2(pos.x, pos.y));
		}

		private void CalcSelectEnties()
		{
			if (m_SelectEntities == null)
			{
				m_SelectEntities = new List<EntityData>();
			}
			else
			{
				m_SelectEntities.Clear();
			}

			PlanetData planetData = GameMain.localPlanet;
			if (planetData != null)
			{
				PlanetFactory planetFactory = planetData.factory;
				if (planetFactory != null)
				{
					FactorySystem factorySystem = planetFactory.factorySystem;

					Camera c = Camera.main;

					for (int i = 1; i < planetFactory.entityCursor; ++i)
					{
						EntityData entityData = planetFactory.entityPool[i];
						Vector3 screenPos = c.WorldToScreenPoint(entityData.pos);
						if (IsInSelectRange(screenPos))
						{
							m_SelectEntities.Add(entityData);
						}
					}
				}

				Debug.LogFormat("Select enties count:{0}", m_SelectEntities.Count);
				foreach (var ed in m_SelectEntities)
				{
					Debug.LogFormat("Select entry:{0}", ed.protoId);
				}

			}

		}

		private void ShowDebugInfo()
		{
			PlanetData planetData = GameMain.localPlanet;
			if (planetData != null)
			{
				PlanetFactory planetFactory = planetData.factory;

				Debug.LogFormat("enties:{0},prebuild:{1},vein:{2},vege:{3}", planetFactory.entityCursor, planetFactory.prebuildCursor, planetFactory.veinCursor, planetFactory.vegeCursor);

				if (planetFactory != null)
				{
					FactorySystem factorySystem = planetFactory.factorySystem;
					Debug.LogFormat("miner:{0},insert:{1},Assembler:{2},Fractionate:{3},Ejector:{4},silo:{5},lab:{6}",
						factorySystem.minerCursor, factorySystem.inserterCursor, factorySystem.assemblerCursor, factorySystem.fractionateCursor,
						factorySystem.ejectorCursor, factorySystem.siloCursor, factorySystem.labCursor
						);
				}
			}

			foreach (var ed in m_SelectEntities)
			{
				var dict = SerializeObject(ed);
				string s = "";
				foreach (var it in dict)
				{
					s += it.Key + "=" + it.Value + ",";
				}
				Debug.Log(s);
			}
		}

		private static void DrawOutline(Rect rect, Texture2D tex, Color color)
		{
			if (Event.current.type == EventType.Repaint)
			{
				Color oldColor = GUI.color;
				GUI.color = color;
				GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, 1f, rect.height), tex);
				GUI.DrawTexture(new Rect(rect.xMax, rect.yMin, 1f, rect.height), tex);
				GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, rect.width, 1f), tex);
				GUI.DrawTexture(new Rect(rect.xMin, rect.yMax, rect.width, 1f), tex);
				GUI.color = oldColor;
			}
		}

		private static Texture2D CreateDummyTex()
		{
			Texture2D tex = new Texture2D(1, 1);
			tex.name = "[Generated] Dummy Texture";
			tex.hideFlags = HideFlags.DontSave;
			tex.filterMode = FilterMode.Point;
			tex.SetPixel(0, 0, Color.white);
			tex.Apply();
			return tex;
		}

		public static Dictionary<string, object> SerializeObject(object obj)
		{
			Dictionary<string, object> data = new Dictionary<string, object>();
			Type type = obj.GetType();
			FieldInfo[] fields = YH.ReflectionUtils.GetFields(type);
			foreach (var fieldInfo in fields)
			{
				data[fieldInfo.Name] = fieldInfo.GetValue(obj);
			}
			return data;
		}


	}
}
