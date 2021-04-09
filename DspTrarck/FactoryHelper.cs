using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DspTrarck
{
	public class FactoryHelper
	{
		private static Pose[] emptyPoseArr = new Pose[0];

		public static bool ObjectIsBelt(PlanetFactory factory, int objId)
		{
			if (objId == 0)
			{
				return false;
			}
			if (objId > 0)
			{
				return LDB.items.Select(factory.entityPool[objId].protoId)?.prefabDesc.isBelt ?? false;
			}
			return LDB.items.Select(factory.prebuildPool[-objId].protoId)?.prefabDesc.isBelt ?? false;
		}

		public static Pose[] GetLocalGates(PlanetFactory factory, int objId)
		{
			if (objId == 0)
			{
				return emptyPoseArr;
			}
			PrefabDesc prefabDesc;
			if (objId > 0)
			{
				ModelProto modelProto = LDB.models.Select(factory.entityPool[objId].modelIndex);
				if (modelProto == null)
				{
					return emptyPoseArr;
				}
				prefabDesc = modelProto.prefabDesc;
			}
			else
			{
				ModelProto modelProto2 = LDB.models.Select(factory.prebuildPool[-objId].modelIndex);
				if (modelProto2 == null)
				{
					return emptyPoseArr;
				}
				prefabDesc = modelProto2.prefabDesc;
			}
			if (!prefabDesc.multiLevel || prefabDesc.multiLevelAllowInserter)
			{
				return prefabDesc.slotPoses;
			}
			factory.ReadObjectConn(objId, 14, out var _, out var otherObjId, out var _);
			if (otherObjId != 0)
			{
				return emptyPoseArr;
			}
			return prefabDesc.slotPoses;
		}

		public static PrefabDesc GetPrefabDesc(PlanetFactory factory, int objId)
		{
			if (objId == 0)
			{
				return null;
			}
			if (objId > 0)
			{
				return LDB.models.Select(factory.entityPool[objId].modelIndex)?.prefabDesc;
			}
			return LDB.models.Select(factory.prebuildPool[-objId].modelIndex)?.prefabDesc;
		}

		public static Pose GetObjectPose(PlanetFactory factory, int objId)
		{
			if (objId == 0)
			{
				return Pose.identity;
			}
			if (objId > 0)
			{
				return new Pose(factory.entityPool[objId].pos, factory.entityPool[objId].rot);
			}
			return new Pose(factory.prebuildPool[-objId].pos, factory.prebuildPool[-objId].rot);
		}
	}
}
