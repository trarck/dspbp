using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using YH.Log;

namespace DspTrarck
{
	public class MultiSelector
	{
		private bool m_EnableSelect;
		private bool m_SelectStart;
		private Vector3 m_MouseStartPosition;
		private Vector3 m_MouseEndPosition;
		private Rect m_SelectRange;
		private bool m_SelectGroundActive;
		private Rect m_SelectGcsRange;
		private bool m_NeedRepeatLongitude;
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
#if DEBUG
					ShowDebugInfo();
#endif
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

			m_SelectGroundActive = false;
			Vector3 startGroundPos = Vector3.zero;
			FactoryBP factoryBP = TrarckPlugin.Instance.factoryBP;
			if (factoryBP.TryScreenPositionToGroundPosition(m_MouseStartPosition, ref startGroundPos))
			{
				Vector3 endGroundPos = Vector3.zero;
				if (factoryBP.TryScreenPositionToGroundPosition(m_MouseEndPosition, ref endGroundPos))
				{
					m_SelectGroundActive = true;
					Vector3 startGcs = factoryBP.planetCoordinate.LocalToGcs(startGroundPos);
					Vector3 endGcs = factoryBP.planetCoordinate.LocalToGcs(endGroundPos);

					float latMin = Mathf.Min(startGcs.y, endGcs.y);
					float latMax = Mathf.Max(startGcs.y, endGcs.y);

					float longMin, longMax;


					m_NeedRepeatLongitude = false;
					if (startGcs.x * endGcs.x > 0)
					{
						//同向
						longMin = Mathf.Min(startGcs.x, endGcs.x);
						longMax = Mathf.Max(startGcs.x, endGcs.x);
					}
					else
					{
						//跨越正负
						longMin = Mathf.Min(startGcs.x, endGcs.x);
						longMax = Mathf.Max(startGcs.x, endGcs.x);
						if (longMax - longMin > Mathf.PI)
						{
							float tempMax = longMin + 2 * Mathf.PI;
							longMin = longMax;
							longMax = tempMax;

							m_NeedRepeatLongitude = true;
						}
					}

					m_SelectGcsRange = new Rect(longMin, latMin, longMax - longMin, latMax - latMin);
				}
			}
		}

		private bool IsInSelectRange(Vector3 pos)
		{
			return m_SelectRange.Contains(new Vector2(pos.x, pos.y));
		}

		private bool IsInGroundRange(Vector3 pos)
		{
			Vector2 gcs = TrarckPlugin.Instance.factoryBP.planetCoordinate.LocalToGcs(pos);
			
			if (m_NeedRepeatLongitude)
			{
				 gcs.x=Mathf.Repeat(gcs.x, 2 * Mathf.PI);
			}

			return m_SelectGcsRange.Contains(gcs);
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
					Camera c = Camera.main;

					YHDebug.LogFormat("CalcSelectEntities:active:{0},screen:{1},gcs:{2}", m_SelectGroundActive,m_SelectRange,m_SelectGcsRange);
					if (m_SelectGroundActive)
					{
						for (int i = 1; i < planetFactory.entityCursor; ++i)
						{
							EntityData entityData = planetFactory.entityPool[i];
							Vector3 screenPos = c.WorldToScreenPoint(entityData.pos);
							//TODO:使用cell index来判断或gcs值
							YHDebug.LogFormat("CalcSelectEntities:screen:{0}={1},gcs:{2}={3}", screenPos, IsInSelectRange(screenPos),TrarckPlugin.Instance.factoryBP.planetCoordinate.LocalToGcs(entityData.pos), IsInGroundRange(entityData.pos));
							if (IsInSelectRange(screenPos) && IsInGroundRange(entityData.pos))
							{
								m_SelectEntities.Add(entityData);
							}
						}
					}
				}

				//Debug.LogFormat("Select enties count:{0}", m_SelectEntities.Count);
				//foreach (var ed in m_SelectEntities)
				//{
				//	Debug.LogFormat("Select entry:{0}", ed.protoId);
				//}

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
