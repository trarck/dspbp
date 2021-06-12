using HarmonyLib;
using UnityEngine;
using YH.Log;


namespace DspTrarck
{
	public class BuildTool_BluePrint : BuildTool
	{
		public ItemProto handItem;

		public PrefabDesc handPrefabDesc;

		public int modelOffset;

		public bool castTerrain;

		public bool castPlatform;

		public bool castGround;

		public Vector3 castGroundPos = Vector3.zero;

		public Vector3 castGroundPosSnapped = Vector3.zero;

		public bool castObject;

		public int castObjectId;

		public Vector3 castObjectPos;

		private bool isDragging;

		public Vector3 startGroundPosSnapped = Vector3.zero;

		public Vector3[] dotsSnapped;

		public bool cursorValid;

		public Vector3 cursorTarget;

		public bool waitForConfirm;

		public bool multiLevelCovering;

		public float yaw;

		public float gap;

		public bool tabgapDir = true;
	}
}
