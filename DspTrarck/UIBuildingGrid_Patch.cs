using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using YH.Log;

namespace DspTrarck
{
	public class UIBuildingGrid_Patch
	{
		[HarmonyPostfix, HarmonyPriority(Priority.Last), HarmonyPatch(typeof(UIBuildingGrid), "Update")]
		public static void UIBuildingGrid_Update_Postfix(ref UIBuildingGrid __instance, ref Material ___material)
		{
			if (TrarckPlugin.Instance.isBPCreate)
			{
				PlanetData planetData = GameMain.localPlanet;
				Player mainPlayer = GameMain.mainPlayer;
				PlanetFactory planetFactory = planetData?.factory;
				if (planetFactory == null || !planetData.factoryLoaded)
				{
					planetData = null;
				}
				PlanetGrid planetGrid = null;
				if (mainPlayer != null && planetData != null && planetData.aux != null && (uint)planetData.aux.activeGridIndex < (uint)planetData.aux.customGrids.Count)
				{
					planetGrid = planetData.aux.customGrids[planetData.aux.activeGridIndex];
				}
				if (planetGrid != null)
				{
					Vector4 value = Vector4.zero;
					if (TrarckPlugin.Instance.bluePrintCreateTool.cursorType > 0 && TrarckPlugin.Instance.bluePrintCreateTool.castGround)
					{
						value = planetGrid.GratboxByCenterSize(TrarckPlugin.Instance.bluePrintCreateTool.castGroundPos, TrarckPlugin.Instance.bluePrintCreateTool.cursorSize);
					}
					___material.SetVector("_CursorGratBox", value);
				}
			}
		}
	}
}
