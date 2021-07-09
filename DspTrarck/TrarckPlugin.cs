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
		private YH.MyInput.CombineKey m_CreateBluePrintKey = new YH.MyInput.CombineKey("CopyEntities", true, KeyCode.J);
		private YH.MyInput.CombineKey m_CopyEntitiesWithoutBeltKey = new YH.MyInput.CombineKey("CopyEntities", true, KeyCode.K);
		private YH.MyInput.CombineKey m_BuildEntitiesKey = new YH.MyInput.CombineKey("BuildEntities", true, KeyCode.I);
		private YH.MyInput.CombineKey m_BuildEntitiesWithoutBeltKey = new YH.MyInput.CombineKey("BuildEntities", true, KeyCode.O);
		private YH.MyInput.CombineKey m_SaveBPKey = new YH.MyInput.CombineKey("SaveBP", true, KeyCode.LeftControl, KeyCode.RightControl, KeyCode.S);

		private FactoryBP m_FactoryBP;
		private FactoryBPUI m_FactoryBPUI;

		private bool m_EnterFactoryBPMode;
		private bool m_BPCreate = false;
		private bool m_BPBuild = false;

		private static TrarckPlugin s_Instance = null;
		private Harmony m_Harmony;

		public bool NeedResetBuildPreview = false;
		public BuildTool_BluePrint_Create bluePrintCreateTool = null;

		public static TrarckPlugin Instance
		{
			get {
				return s_Instance;
			}
		}
		
		public bool isBluePrintMode
		{
			get
			{
				return m_EnterFactoryBPMode;
			}
		}

		public bool isBPBuild
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

		public bool isBPCreate
		{
			get
			{
				return m_BPCreate;
			}
			set
			{
				m_BPCreate = value;
			}
		}

		public FactoryBP factoryBP
		{
			get
			{
				return m_FactoryBP;
			}
		}

		public FactoryBPUI factoryBPUI
		{
			get
			{
				return m_FactoryBPUI;
			}
		}

		void Awake()
		{
			m_Harmony = new Harmony("com.trarck.dspplugin");

			m_Harmony.PatchAll(typeof(PlayerAction_Build_Patch));

			m_Harmony.PatchAll(typeof(UIBuildingGrid_Patch));

			//GameData gd = GameMain.data;

			//Player p1 = GameMain.mainPlayer;

			//PlanetData pd1= GameMain.mainPlayer.planetData;
			//PlanetData pd2= GameMain.data.localPlanet;

			//PlanetFactory pf = pd1.factory;

			//FactorySystem fs = pf.factorySystem;

			m_EnterFactoryBPMode = false;

			m_FactoryBP = new FactoryBP();
			m_FactoryBP.Init();

			m_FactoryBPUI = new FactoryBPUI();
			m_FactoryBPUI.Init(m_FactoryBP);

			s_Instance = this;
		}

		void Update()
		{
			//进入蓝图模式或退出
			if (GameMain.mainPlayer != null
				&& GameMain.mainPlayer.controller != null
				&& GameMain.mainPlayer.controller.cmd.type == ECommand.Build)
			{
				//更新键盘事件
				KeyManager.Instance.Update();

				if (m_BPEnterKey.IsDown())
				{
					if (m_EnterFactoryBPMode)
					{
						ExitBluePrintMode();
					}
					else
					{
						EnterBluePrintMode();
					}
				}
			}
			else if (m_EnterFactoryBPMode)
			{
				//退出蓝图模式
				ExitBluePrintMode();
				return;
			}

			//蓝图功能
			if (m_EnterFactoryBPMode)
			{
				if (m_CreateBluePrintKey.IsDown())
				{
					YHDebug.Log("On create bp Key down");
					m_BPCreate = true;
					m_BPBuild = false;
				}

				if (m_BuildEntitiesKey.IsDown())
				{
					YHDebug.Log("On build bp entities Key down");
					//build
					//CreateBuildPreviews(Input.mousePosition);
					m_BPBuild = true;
					m_BPCreate = false;
				}

				if (m_SaveBPKey.IsDown())
				{
					YHDebug.Log("On save bp Key down");
					//save
					SaveCurrentBPData();
				}

				//if (m_BPBuild)
				//{
				//	//取消build
				//	if (Input.GetMouseButtonDown(1) && (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift)))
				//	{
				//		m_BPBuild = false;
				//	}
				//}

				//if (m_BPCreate)
				//{
				//	//取消create
				//	if (Input.GetMouseButtonDown(1) && (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift)))
				//	{
				//		m_BPCreate = false;
				//	}
				//}
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
			}
		}

		private void EnterBluePrintMode()
		{
			YHDebug.Log("enter bp mode");
			m_EnterFactoryBPMode = true;

			//check planet data
			PlanetData planetData = GameMain.localPlanet;
			if (planetData != null && planetData != m_FactoryBP.planetData)
			{
				m_FactoryBP.SetPlanetData(planetData);
				m_FactoryBP.player = GameMain.mainPlayer;
			}
		}

		private void ExitBluePrintMode()
		{
			YHDebug.Log("exit bp mode");
			m_EnterFactoryBPMode = false;
			m_BPBuild = false;
			m_BPCreate = false;

			m_FactoryBPUI.Clear();
			m_FactoryBP.Clear();
		}

		private void SaveCurrentBPData()
		{
			m_FactoryBPUI.SaveBPFile();
		}

		private void CreateBuildPreviews(Vector3 mousePos)
		{
			//m_FactoryBP.CreateBuildPreviews();
			//Vector3 groundPos = Vector3.zero;
			//if (m_FactoryBP.TryScreenPositionToGroundPosition(mousePos, ref groundPos))
			//{
			//	m_FactoryBP.UpdateBuildPosition(groundPos);
			//}
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
