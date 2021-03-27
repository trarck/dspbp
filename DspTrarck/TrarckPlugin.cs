using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using YH.MyInput;

namespace DspTrarck
{
	[BepInPlugin("com.trarck.dspplugin", "Trarck Plug-In", "1.0.0.0")]
	public class TrarckPlugin: BaseUnityPlugin
	{
		private bool m_EnterFactoryBPStart;

		private YH.MyInput.CombineKey m_BPEnterKey = new YH.MyInput.CombineKey("BPEnter", true, KeyCode.LeftControl, KeyCode.RightControl, KeyCode.Z);
		private YH.MyInput.CombineKey m_CopyEntitiesKey = new YH.MyInput.CombineKey("CopyEntities", true, KeyCode.J);
		private YH.MyInput.CombineKey m_BuildEntitiesKey = new YH.MyInput.CombineKey("BuildEntities", true, KeyCode.Z);
		private YH.MyInput.CombineKey m_SaveBPKey = new YH.MyInput.CombineKey("SaveBP", true, KeyCode.S);

		private bool m_SelectStart;
		private Vector3 m_MouseStartPosition;
		private Vector3 m_MouseEndPosition;
		private Rect m_SelectRange;
		private List<EntityData> m_SelectEnties;

		private Texture2D m_BlankTexture;

		private Color m_LineColor = Color.green;

		private FactoryBP m_FactoryBP;
		private bool m_BPBuild = false;

		private static TrarckPlugin s_Instance = null;
		private Harmony m_Harmony;

		public static TrarckPlugin Instance
		{
			get {
				return s_Instance;
			}
		}
		
		public bool BPBuild
		{
			get
			{
				return m_BPBuild;
			}
			set
			{
				m_BPBuild = value;
			}
		}

		public FactoryBP factoryBP
		{
			get
			{
				return m_FactoryBP;
			}
		}

		void Awake()
		{
			m_Harmony = new Harmony("com.trarck.dspplugin");

			m_Harmony.PatchAll(typeof(PlayerAction_Build_Patch));

			//GameData gd = GameMain.data;

			//Player p1 = GameMain.mainPlayer;

			//PlanetData pd1= GameMain.mainPlayer.planetData;
			//PlanetData pd2= GameMain.data.localPlanet;

			//PlanetFactory pf = pd1.factory;

			//FactorySystem fs = pf.factorySystem;

			Debug.Log("HelloWorld");
			m_BlankTexture = CreateDummyTex();
			m_EnterFactoryBPStart = false;
			m_SelectStart = false;

			m_FactoryBP = new FactoryBP();
			m_FactoryBP.Init();

			s_Instance = this;
		}

		void Update()
		{
			KeyManager.Instance.Update();

			if (!m_BPBuild)
			{
				MultiSelectTick();
			}

			if (m_EnterFactoryBPStart)
			{
				PlanetData planetData = GameMain.localPlanet;
				if (planetData != null && planetData != m_FactoryBP.planetData)
				{
					m_FactoryBP.SetPlanetData(planetData);
					m_FactoryBP.player = GameMain.mainPlayer;
				}

				if (m_CopyEntitiesKey.IsDown())
				{
					Debug.Log("On copy entities Key down");
					//copy
					CopyEntities();
				}

				if (m_BuildEntitiesKey.IsDown())
				{
					Debug.Log("On build bp entities Key down");
					//build
					CreateBuildPreviews(Input.mousePosition);
					m_BPBuild = true;
				}

				if (m_SaveBPKey.IsDown())
				{
					Debug.Log("On save bp Key down");
					//save
					SaveCurrentBPData();
				}

				if (m_BPBuild)
				{
					if (Input.GetMouseButtonDown(1))
					{
						m_BPBuild = false;
					}

					//UpdateBuildPreviewsPosition(Input.mousePosition);
				}
			}
		}
		
		void OnGUI()
		{
			if (!m_SelectStart)
			{
				return;
			}
			Rect r = new Rect(m_MouseStartPosition.x, Screen.height - m_MouseStartPosition.y, m_MouseEndPosition.x - m_MouseStartPosition.x, m_MouseStartPosition.y - m_MouseEndPosition.y);
			DrawOutline(r, m_BlankTexture, m_LineColor);
		}

		private void MultiSelectTick()
		{
			if (m_BPEnterKey.IsDown())
			{
				m_EnterFactoryBPStart = !m_EnterFactoryBPStart;

				m_SelectStart = false;
			}


			if (m_EnterFactoryBPStart)
			{
				if (Input.GetMouseButtonDown(0))
				{
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
			return m_SelectRange.Contains(new Vector2(pos.x,pos.y));
		}
		private void CalcSelectEnties()
		{
			if (m_SelectEnties == null)
			{
				m_SelectEnties = new List<EntityData>();
			}
			else
			{
				m_SelectEnties.Clear();
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
							m_SelectEnties.Add(entityData);
						}
					}
				}

				Debug.LogFormat("Select enties count:{0}", m_SelectEnties.Count);
				foreach (var ed in m_SelectEnties)
				{
					Debug.LogFormat("Select entry:{0}", ed.protoId);
				}

			}
		}

		private void CopyEntities()
		{
			if (m_SelectEnties != null && m_SelectEnties.Count > 0)
			{
				DateTime now = DateTime.Now;
				string name = now.ToString("yyyy-MM-dd-HH-mm-ss");
				m_FactoryBP.CopyEntities(name, m_SelectEnties, BPData.PosType.Relative);
			}
		}

		private void SaveCurrentBPData()
		{
			m_FactoryBP.SaveBPData(m_FactoryBP.currentData);
		}

		private void CreateBuildPreviews(Vector3 mousePos)
		{
			m_FactoryBP.CreateBuildPreviews();
			Vector3 groundPos = Vector3.zero;
			if (m_FactoryBP.TryScreenPositionToGroundPosition(mousePos, ref groundPos))
			{
				m_FactoryBP.UpdateBuildPosition(groundPos);
			}
		}

		private void UpdateBuildPreviewsPosition(Vector3 mousePos)
		{
			Debug.LogFormat("UpdateBuildPreviewsPosition:mouse pos :{0}", mousePos);
			Vector3 groundPos=Vector3.zero;
			if (m_FactoryBP.TryScreenPositionToGroundPosition(mousePos, ref groundPos))
			{
				Debug.LogFormat("UpdateBuildPreviewsPosition:ground pos :{0}", groundPos);
				m_FactoryBP.UpdateBuildPosition(groundPos);
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

			foreach (var ed in m_SelectEnties)
			{
				var dict = SerializeObject(ed);
				string s = "";
				foreach (var it in dict)
				{
					s += it.Key + "=" + it.Value+",";
				}
				Debug.Log(s);
			}
		}

		static Texture2D CreateDummyTex()
		{
			Texture2D tex = new Texture2D(1, 1);
			tex.name = "[Generated] Dummy Texture";
			tex.hideFlags = HideFlags.DontSave;
			tex.filterMode = FilterMode.Point;
			tex.SetPixel(0, 0, Color.white);
			tex.Apply();
			return tex;
		}

		static public void DrawOutline(Rect rect, Texture2D tex, Color color)
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
		public static Vector3 ScreenPositionToWorldPosition(Vector2 pointer, Camera camera)
		{
			Vector3 cameraPos = new Vector3(pointer.x, pointer.y, GetDistanceFromScreenToWorld(pointer, camera));
			Vector3 worldPos = camera.ScreenToWorldPoint(cameraPos);
			return worldPos;
		}

		/// <summary>
		/// 取得屏幕点所在游戏内的距离
		/// 通常是距地面的距离
		/// </summary>
		/// <param name="pointer"></param>
		/// <returns></returns>
		public static float GetDistanceFromScreenToWorld(Vector2 pointer, Camera camera)
		{
			Ray ray = camera.ScreenPointToRay(pointer);
			RaycastHit hit;
			if (Physics.Raycast(ray, out hit))
			{
				//Debug.Log(hit.transform.name + "," + hit.distance);
				return hit.distance;
			}
			return 0;
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
