using System;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using YH.MyInput;
using YH.Log;

namespace DspTrarck
{
	[BepInPlugin("com.trarck.dspplugin", "Trarck Plug-In", "1.0.0.0")]
	public class TrarckPlugin: BaseUnityPlugin
	{
		private bool m_EnterFactoryBPMode;

		private YH.MyInput.CombineKey m_BPEnterKey = new YH.MyInput.CombineKey("BPEnter", true, KeyCode.LeftControl, KeyCode.RightControl, KeyCode.Z);
		private YH.MyInput.CombineKey m_CopyEntitiesKey = new YH.MyInput.CombineKey("CopyEntities", true, KeyCode.J);
		private YH.MyInput.CombineKey m_CopyEntitiesWithoutBeltKey = new YH.MyInput.CombineKey("CopyEntities", true, KeyCode.K);
		private YH.MyInput.CombineKey m_BuildEntitiesKey = new YH.MyInput.CombineKey("BuildEntities", true, KeyCode.I);
		private YH.MyInput.CombineKey m_BuildEntitiesWithoutBeltKey = new YH.MyInput.CombineKey("BuildEntities", true, KeyCode.O);
		private YH.MyInput.CombineKey m_SaveBPKey = new YH.MyInput.CombineKey("SaveBP", true, KeyCode.S);

		private MultiSelector m_MultiSelector;
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

			m_EnterFactoryBPMode = false;

			m_MultiSelector = new MultiSelector();
			m_MultiSelector.Init();

			m_FactoryBP = new FactoryBP();
			m_FactoryBP.Init();

			s_Instance = this;
		}

		void Update()
		{
			KeyManager.Instance.Update();

			if (m_BPEnterKey.IsDown())
			{
				m_EnterFactoryBPMode = !m_EnterFactoryBPMode;

				if (m_MultiSelector != null)
				{
					m_MultiSelector.enableSelect = m_EnterFactoryBPMode;
				}
			}

			if (m_MultiSelector != null && !m_BPBuild)
			{
				m_MultiSelector.Update();
			}

			if (m_EnterFactoryBPMode)
			{
				//check planet data
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

				if (m_CopyEntitiesWithoutBeltKey.IsDown())
				{
					YHDebug.Log("On copy entities without belt Key down");
					CopyEntitiesWithoutBelt();
				}

				if (m_BuildEntitiesKey.IsDown())
				{
					YHDebug.Log("On build bp entities Key down");
					//build
					CreateBuildPreviews(Input.mousePosition);
					m_BPBuild = true;
				}



				if (m_SaveBPKey.IsDown())
				{
					YHDebug.Log("On save bp Key down");
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
			if (m_MultiSelector==null)
			{
				return;
			}
			m_MultiSelector.OnGUI();
		}

		private void CopyEntities()
		{
			if (m_MultiSelector.selectEntities != null && m_MultiSelector.selectEntities.Count > 0)
			{
				DateTime now = DateTime.Now;
				string name = now.ToString("yyyy-MM-dd-HH-mm-ss");
				m_FactoryBP.CopyEntities(name, m_MultiSelector.selectEntities, BPData.PosType.Relative);
			}
		}

		private void CopyEntitiesWithoutBelt()
		{
			if (m_MultiSelector.selectEntities != null && m_MultiSelector.selectEntities.Count > 0)
			{
				//filter etities
				List<EntityData> entities = new List<EntityData>();
				foreach (var ed in m_MultiSelector.selectEntities)
				{
					if (ed.beltId == 0)
					{
						entities.Add(ed);						
					}
				}

				DateTime now = DateTime.Now;
				string name = now.ToString("yyyy-MM-dd-HH-mm-ss");
				m_FactoryBP.CopyEntities(name, entities, BPData.PosType.Relative);
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
			YHDebug.LogFormat("UpdateBuildPreviewsPosition:mouse pos :{0}", mousePos);
			Vector3 groundPos=Vector3.zero;
			if (m_FactoryBP.TryScreenPositionToGroundPosition(mousePos, ref groundPos))
			{
				YHDebug.LogFormat("UpdateBuildPreviewsPosition:ground pos :{0}", groundPos);
				m_FactoryBP.UpdateBuildPosition(groundPos);
			}
		}

		//public static Vector3 ScreenPositionToWorldPosition(Vector2 pointer, Camera camera)
		//{
		//	Vector3 cameraPos = new Vector3(pointer.x, pointer.y, GetDistanceFromScreenToWorld(pointer, camera));
		//	Vector3 worldPos = camera.ScreenToWorldPoint(cameraPos);
		//	return worldPos;
		//}

		/// <summary>
		/// 取得屏幕点所在游戏内的距离
		/// 通常是距地面的距离
		/// </summary>
		/// <param name="pointer"></param>
		/// <returns></returns>
		//public static float GetDistanceFromScreenToWorld(Vector2 pointer, Camera camera)
		//{
		//	Ray ray = camera.ScreenPointToRay(pointer);
		//	RaycastHit hit;
		//	if (Physics.Raycast(ray, out hit))
		//	{
		//		//Debug.Log(hit.transform.name + "," + hit.distance);
		//		return hit.distance;
		//	}
		//	return 0;
		//}
	}
}
