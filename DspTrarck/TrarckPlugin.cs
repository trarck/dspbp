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
		private YH.MyInput.CombineKey m_BPEnterKey = new YH.MyInput.CombineKey("BPEnter", true, KeyCode.LeftControl, KeyCode.RightControl, KeyCode.Z);
		private YH.MyInput.CombineKey m_CopyEntitiesKey = new YH.MyInput.CombineKey("CopyEntities", true, KeyCode.J);
		private YH.MyInput.CombineKey m_CopyEntitiesWithoutBeltKey = new YH.MyInput.CombineKey("CopyEntities", true, KeyCode.K);
		private YH.MyInput.CombineKey m_BuildEntitiesKey = new YH.MyInput.CombineKey("BuildEntities", true, KeyCode.I);
		private YH.MyInput.CombineKey m_BuildEntitiesWithoutBeltKey = new YH.MyInput.CombineKey("BuildEntities", true, KeyCode.O);
		private YH.MyInput.CombineKey m_SaveBPKey = new YH.MyInput.CombineKey("SaveBP", true, KeyCode.LeftControl, KeyCode.RightControl, KeyCode.S);


		private bool m_EnterFactoryBPMode;

		private FactoryBP m_FactoryBP;
		private MultiSelector m_MultiSelector;
		private FactoryBPUI m_FactoryBPUI;

		private bool m_BPBuild = false;

		private static TrarckPlugin s_Instance = null;
		private Harmony m_Harmony;

		public bool NeedResetBuildPreview = false;

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

			m_Harmony.PatchAll(typeof(BuildTool_Click_Patch));
			m_Harmony.PatchAll(typeof(PlayerAction_Build_Patch));

			//GameData gd = GameMain.data;

			//Player p1 = GameMain.mainPlayer;

			//PlanetData pd1= GameMain.mainPlayer.planetData;
			//PlanetData pd2= GameMain.data.localPlanet;

			//PlanetFactory pf = pd1.factory;

			//FactorySystem fs = pf.factorySystem;

			m_EnterFactoryBPMode = false;

			m_FactoryBP = new FactoryBP();
			m_FactoryBP.Init();


			m_MultiSelector = new MultiSelector();
			m_MultiSelector.Init(m_FactoryBP);

			m_FactoryBPUI = new FactoryBPUI();
			m_FactoryBPUI.Init(m_FactoryBP);


			s_Instance = this;
		}

		void Update()
		{
			if (m_EnterFactoryBPMode)
			{
				//如果不是build模式，什么都不做。
				if (GameMain.mainPlayer != null
				&& GameMain.mainPlayer.controller != null
				&& GameMain.mainPlayer.controller.cmd.type != ECommand.Build)
				{

					Debug.Log("exit bp mode");
					m_EnterFactoryBPMode = false;
					m_BPBuild = false;

					m_MultiSelector.Clear();
					m_FactoryBPUI.Clear();
					m_FactoryBP.Clear();

					return;
				}
			}

			//更新键盘事件
			KeyManager.Instance.Update();

			//是否开启蓝图
			if (m_BPEnterKey.IsDown())
			{
				Debug.Log("enter bp mode");
				m_EnterFactoryBPMode = !m_EnterFactoryBPMode;

				if (m_MultiSelector != null)
				{
					m_MultiSelector.enableSelect = m_EnterFactoryBPMode;
				}
			}

			//如果不在蓝图建造中，更新多选
			if (m_MultiSelector != null && !m_BPBuild)
			{
				m_MultiSelector.Update();
			}

			//蓝图功能
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
					//取消build
					if (Input.GetMouseButtonDown(1) && (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift)))
					{
						m_BPBuild = false;
					}
				}
			}
		}
		
		void OnGUI()
		{
			if (m_EnterFactoryBPMode)
			{
				if (m_FactoryBPUI != null)
				{
					m_FactoryBPUI.OnGUI();
				}

				if (m_MultiSelector != null)
				{
					m_MultiSelector.OnGUI();
				}
			}
		}


		private string CopyEntities(List<EntityData> entities,string bpName,bool noBelt)
		{
			if (entities != null && entities.Count > 0)
			{
				if (string.IsNullOrEmpty(bpName))
				{
					bpName = GetDefaultName();
				}

				//不要电线杆，会有碰撞问题
				for (int i = entities.Count - 1; i >= 0; --i)
				{
					if (entities[i].powerNodeId != 0)
					{
						entities.RemoveAt(i);
					}
				}

				if (noBelt)
				{
					//filter etities
					for  (int i=entities.Count-1;i>=0;--i)
					{
						if (entities[i].beltId != 0)
						{
							entities.RemoveAt(i);
						}
					}
				}

				m_FactoryBP.CopyEntities(bpName, entities, BPData.PosType.Relative);

				return bpName;
			}
			return null;
		}

		private void CopyEntities()
		{
			string name=CopyEntities(m_MultiSelector.selectEntities, m_FactoryBPUI.bpName, m_FactoryBPUI.isCopyWithoutBelt);
			if (string.IsNullOrEmpty(m_FactoryBPUI.bpName))
			{
				m_FactoryBPUI.bpName = name;
			}
		}

		private void CopyEntitiesWithoutBelt()
		{
			string name = CopyEntities(m_MultiSelector.selectEntities, m_FactoryBPUI.bpName, true);
			if (string.IsNullOrEmpty(m_FactoryBPUI.bpName))
			{
				m_FactoryBPUI.bpName = name;
			}
		}

		private string GetDefaultName()
		{
			DateTime now = DateTime.Now;
			string name = now.ToString("yyyy-MM-dd-HH-mm-ss");
			return name;
		}

		private void SaveCurrentBPData()
		{
			m_FactoryBPUI.SaveBPFile();
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
