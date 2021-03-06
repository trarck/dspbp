using System;
using System.Collections.Generic;
using UnityEngine;

namespace DspTrarck
{
	public enum BPEntityType
	{
		None,
		Belt,
		Splitter,
		Storage,
		Tank,
		Miner,
		Inserter,
		Assembler,
		Fractionate,
		Lab,
		Station,
		Ejector,
		Silo,
		PowerGen,
		PowerConsumer,
		PowerExchanger,
		PowerNode,
		PowerAccumulator
	}

	//[Serializable]
	//public class ConnectData
	//{
	//	//public BuildPreview output;

	//	//public BuildPreview input;

	//	public int outputObjId;

	//	public int inputObjId;

	//	public int outputToSlot;

	//	public int inputFromSlot;

	//	public int outputFromSlot;

	//	public int inputToSlot;

	//	public int outputOffset;

	//	public int inputOffset;

	//	public void ResetInfos()
	//	{
	//		outputObjId = 0;
	//		inputObjId = 0;
	//		outputToSlot = 0;
	//		inputFromSlot = 0;
	//		outputFromSlot = 0;
	//		inputToSlot = 0;
	//		outputOffset = 0;
	//		inputOffset = 0;
	//	}
	//}

	[Serializable]
	public class ConnectData
	{
		//public BuildPreview output;

		//public BuildPreview input;

		public int fromObjId;
		public int fromSlot;

		public int toObjId;
		public int toSlot;
		public int offset;

		public bool isOutput;

		public void ResetInfos()
		{
			fromObjId = 0;
			fromSlot = 0;
			toObjId = 0;
			toSlot = 0;
			offset = 0;
			isOutput = false;
		}
	}


	[Serializable]
	public class BPEntityData
	{
		//copy时游戏内实体id
		public int entityId;

		public int protoId;

		public BPEntityType type;

		public Vector3 pos;
		public Quaternion rot;

		//x:longitude index,y:latitude index
		public Vector2 grid;
		//离地面的距离
		public float offsetGround;

		public short pickOffset;

		public short insertOffset;

		public int recipeId;

		public int filterId;

		//for insert
		public Vector3 pos2;
		public Quaternion rot2;

		public Vector2 grid2;
		//离地面的距离2
		public float offsetGround2;

		//for miner	   insert
		public int paramCount;
		public int[] parameters;
	}

	[Serializable]
	public class BPData
	{
		public enum PosType
		{
			//相对位置。整个蓝图原点，归一化成(0,0)。
			//建造时，根据当前鼠标位置建造。
			Relative,
			//觉对位置。星球的固定位置
			//建造时，只能用蓝图内的坐标。不随当前位置而变化。用于星球复制。
			Absolute
		}

		//数据版本号
		public int version=1;
		//名称
		public string name;

		//位置类型
		public PosType posType;

		//保存的物体
		public List<BPEntityData> entities;

		//物体之间的连接
		public List<ConnectData> connects;

		//经纬度
		public float latitude;
		public float longitude;

		//金纬度大小
		public Vector2 gcsMin;
		public Vector2 gcsMax;

		//x:longitude index,y:latitude index ,z:0
		//public BoundsInt gridBounds;


		//保存时的原点位置。
		public Vector3 originalPos;

		//生成蓝图时的星球半径
		public float planetRadius;


		public BPData()
		{
			entities = new List<BPEntityData>();
		}

		public static bool IsConnectExists(ConnectData connect, List<ConnectData> connects, bool checkSelf = true)
		{
			if (connects != null && connects.Count > 0)
			{
				foreach (var iter in connects)
				{
					//完全相等
					if (checkSelf && iter.fromObjId == connect.fromObjId && iter.toObjId == connect.toObjId && iter.fromSlot == connect.fromSlot && iter.toSlot == connect.toSlot && iter.isOutput==connect.isOutput)
					{
						return true;
					}

					//反向连接。连接是相互的，只保留一个就可以了。
					if (iter.fromObjId == connect.toObjId && iter.toObjId == connect.fromObjId && iter.fromSlot == connect.toSlot && iter.toSlot == connect.fromSlot && iter.isOutput!=connect.isOutput)
					{
						return true;
					}
				}
			}
			return false;
		}

	}
}
