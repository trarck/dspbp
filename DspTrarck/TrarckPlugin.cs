using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace DspTrarck
{
	[BepInPlugin("com.trarck.dspplugin", "Trarck Plug-In", "1.0.0.0")]
	public class TrarckPlugin: BaseUnityPlugin
	{
		private bool m_EnterFactoryBPStart;
		private KeyCode m_EnterFBPKeyOne= KeyCode.LeftControl;
		private KeyCode m_EnterFBPKeyTwo = KeyCode.Z;
		private bool m_EnterFBPKeyOneDown = false;
		private bool m_EnterFBPKeyTwoDown = false;

		private KeyCode m_CopyEntitiesKey = KeyCode.J;
		private KeyCode m_BuildEntitiesKey = KeyCode.K;
		private KeyCode m_SaveBPKey = KeyCode.S;

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

				if (Input.GetKeyDown(m_CopyEntitiesKey))
				{
					Debug.Log("On copy entities Key down");
					//copy
					CopyEntities();
				}

				if (Input.GetKeyDown(m_BuildEntitiesKey))
				{
					Debug.Log("On build bp entities Key down");
					//build
					CreateBuildPreviews(Input.mousePosition);
					m_BPBuild = true;
				}

				if (Input.GetKeyDown(m_SaveBPKey))
				{
					Debug.Log("On save bp Key down");
					//save
					SaveCurrentBPData();
				}

				if (Input.GetKeyDown(KeyCode.Z))
				{

					Vector3 mousePos = Input.mousePosition;
					Vector3 hitPos = m_FactoryBP.playerActionBuild.groundTestPos;
					Vector3 snapPos = m_FactoryBP.playerActionBuild.groundSnappedPos;

					Vector3 gcs = m_FactoryBP.planetCoordinate.LocalToGcs(snapPos);
					Vector3 grid = m_FactoryBP.planetCoordinate.LocalToGrid(snapPos);
					Vector2Int cell = m_FactoryBP.planetCoordinate.LocalToCell(snapPos);
					Vector3 localPosNormal = m_FactoryBP.planetCoordinate.CellToLocalNormal(cell);
					Vector3 localPos = m_FactoryBP.planetCoordinate.LocalNormalToLocal(localPosNormal);

					Vector2Int cell1 = m_FactoryBP.planetCoordinate.LocalToCell(hitPos);
					Vector3 gcs1 = m_FactoryBP.planetCoordinate.CellToGcs(cell1);
					Vector3 grid1 = m_FactoryBP.planetCoordinate.CellToGrid(cell1);
					Vector3 localPosNormal1 = m_FactoryBP.planetCoordinate.CellToLocalNormal(cell1);
					Vector3 localPos1 = m_FactoryBP.planetCoordinate.LocalNormalToLocal(localPosNormal1);

					Debug.LogFormat("Test1:mousePos :{0},hit pos:({1},{2},{3}),snap Pos:({4},{5},{6}),cell pos:({7},{8},{9}),pos2:({10},{11},{12})", mousePos,
						hitPos.x, hitPos.y, hitPos.z,
						snapPos.x	 , snapPos.y, snapPos.z,
							localPos.x, localPos.y, localPos.z,
							localPos1.x, localPos1.y, localPos1.z
						);

					Debug.LogFormat("Test2:gcs :{0},{1} ={2},{3},grid:{4},{5}={6},{7}", gcs.x, gcs.y,gcs1.x,gcs1.y,
						  grid.x,grid.y,grid1.x,grid1.y
						);

					Debug.LogFormat("Test3:pos Normal :{0},{1},{2}={3},{4},{5}={6},{7},{8}",
								snapPos.normalized.x, snapPos.normalized.y, snapPos.normalized.z,
								localPosNormal.x, localPosNormal.y, localPosNormal.z,
								localPosNormal1.x, localPosNormal1.y, localPosNormal1.z
								);

					Debug.LogFormat("Test4:cell :{0},{1}={2},{3}",
						cell.x, cell.y,
						cell1.x, cell1.y);
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
			if (Input.GetKeyDown(m_EnterFBPKeyOne))
			{
				m_EnterFBPKeyOneDown = true;
				Debug.Log(1);
			}

			if (Input.GetKeyDown(m_EnterFBPKeyTwo))
			{
				m_EnterFBPKeyTwoDown = true;
				Debug.Log(3);
			}

			if (m_EnterFBPKeyOneDown && m_EnterFBPKeyTwoDown)
			{
				m_EnterFactoryBPStart = !m_EnterFactoryBPStart;

				m_SelectStart = false;
			}

			if (Input.GetKeyUp(m_EnterFBPKeyOne))
			{
				m_EnterFBPKeyOneDown = false;
				Debug.Log(2);
			}

			if (Input.GetKeyUp(m_EnterFBPKeyTwo))
			{
				m_EnterFBPKeyTwoDown = false;
				Debug.Log(4);
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
