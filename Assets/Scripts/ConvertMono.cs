using MultiBuild;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using DspTrarck;
using System;

[Serializable]
public class LapJointNode
{
    public int protoId;
    public Vector3 lapJoint;
}

[Serializable]
public class LapJointAsset
{
    public List<LapJointNode> lapJoints;
}

public class ConvertMono : MonoBehaviour
{
    public const float PI = (float)Math.PI;
    public const float DoublePI = (float)(Math.PI * 2.0f);

    [SerializeField]
    InputField m_BPFileInput;

    [SerializeField]
    InputField m_BPNameInput;

    [SerializeField]
    Text m_MessageText;

    PlanetCoordinate m_PlanetCoordinate;
    Dictionary<int, Vector3> m_ProtoLapJoints=new Dictionary<int, Vector3>();

    void Start()
    {
        LoadLapJoints();
    }

    // Update is called once per frame
    void LoadLapJoints()
    {
        string latJointFile = System.IO.Path.Combine(Application.streamingAssetsPath, "latJoint.json");
        string cnt = File.ReadAllText(latJointFile);
        LapJointAsset lapJointAsset = JsonUtility.FromJson<LapJointAsset>(cnt);
        if (lapJointAsset != null)
        {
            foreach (var node in lapJointAsset.lapJoints)
            {
                m_ProtoLapJoints[node.protoId] = node.lapJoint;
            }
        }
        Debug.LogFormat("LapJoints count:{0}", m_ProtoLapJoints.Count);
    }


    public void DoConvert()
    {
        string bpStringFile = m_BPFileInput.text;
        if (string.IsNullOrEmpty(bpStringFile))
        {
            ShowMessage("file name is empty");
            return;
        }

#if UNITY_EDITOR
        string saveDir = Path.Combine(Application.dataPath, "../bp");
#else
           string saveDir = Path.Combine(Application.dataPath, "bp");
#endif
        if (!Directory.Exists(saveDir))
        {
            Directory.CreateDirectory(saveDir);
        }



        //dataFile = string.IsNullOrEmpty(bpStringFile) ? "D:\\csharp\\ConvertMBData\\ConvertMBData\\t.txt" : dataFile;// args[1];
        string dataContext = File.ReadAllText(bpStringFile);
        BlueprintData data = BlueprintData.Import(dataContext);
        if (data == null)
        {
            ShowMessage("BlueprintData import error");
            return;
        }

        if (m_ProtoLapJoints.Count == 0)
        {
            LoadLapJoints();
        }

        string dataJson = BlueprintData.GetJsonPrety(dataContext);
        string jsonFile = Path.Combine(saveDir, Path.GetFileNameWithoutExtension(bpStringFile) + ".json");
        File.WriteAllText(jsonFile, dataJson);

        int segmentCount = 200;
        foreach (var build in data.copiedBuildings)
        {
            if (build.originalSegmentCount > 0)
            {
                segmentCount = build.originalSegmentCount;
            }
        }

        float planetRadius = segmentCount;

        m_PlanetCoordinate = new PlanetCoordinate();
        m_PlanetCoordinate.segment = segmentCount;
        m_PlanetCoordinate.radius = planetRadius;


        BPData bpData = new BPData();
        //基础信息
        SetBPBaseData(bpData, data, planetRadius);

        //entities
        Dictionary<int, BeltCopy> entityIdToBeltMap = new Dictionary<int, BeltCopy>();
        SetBPEnitiies(bpData, data, planetRadius, ref entityIdToBeltMap);

        //连接
        SetBPConnects(bpData, data, planetRadius, entityIdToBeltMap);

        //归一化
        UpdateBPDataGrid(bpData);

        //保存
        string bpFile = Path.Combine(saveDir, bpData.name);
        BPDataWriter.WriteBPDataToFile(bpFile, bpData);

        string bpJsonFile = Path.Combine(saveDir, Path.GetFileNameWithoutExtension(bpData.name)+".json");
        string bpJson = JsonUtility.ToJson(bpData,true);
        File.WriteAllText(bpJsonFile, bpJson);
    }


	private void SetBPBaseData(BPData bpData, BlueprintData blueprintData,float planetRadius)
    {
        bpData.version = 1;
        if (!string.IsNullOrEmpty(m_BPNameInput.text))
        {
            bpData.name = m_BPNameInput.text;
        }
        else if (!string.IsNullOrEmpty(blueprintData.name))
        {
            bpData.name = blueprintData.name;
        }

        if (string.IsNullOrEmpty(bpData.name))
        {
            bpData.name = GetDefaultName()+".bin";
            m_BPNameInput.text = bpData.name;
        }

        bpData.posType = BPData.PosType.Relative;
        bpData.originalPos = blueprintData.referencePos.ToCartesian(planetRadius);
        Vector2 gcs = m_PlanetCoordinate.LocalToGcs(bpData.originalPos);
        bpData.longitude = gcs.x;
        bpData.latitude = gcs.y;
        bpData.planetRadius = planetRadius;
    }

    private string GetDefaultName()
    {
        DateTime now = DateTime.Now;
        string name = now.ToString("yyyy-MM-dd-HH-mm-ss");
        return name;
    }

    public void UpdateBPDataGrid(BPData data)
    {
        if (data.posType == BPData.PosType.Relative)
        {
            //统一化坐标。只有使用相对坐标才能统一化坐标
            NormalizeBPData(data);
        }
    }

    /// <summary>
    /// 设置原点。其他entity都是相对原点位置。
    /// 原点目前使用最小经纬度。后面可以考虑指定。
    /// </summary>
    /// <param name="data"></param>
    public void NormalizeBPData(BPData data)
    {
        Vector3 gcs;

        float latMin = float.MaxValue, latMax = float.MinValue;
        float negativeLongMin = float.MaxValue, negativeLongMax = float.MinValue;
        float positiveLongMin = float.MaxValue, positiveLongMax = float.MinValue;

        float latGridMin = float.MaxValue, latGridMax = float.MinValue;
        //计算经纬度包围盒
        for (int i = 0; i < data.entities.Count; ++i)
        {
            BPEntityData entityData = data.entities[i];
            gcs = m_PlanetCoordinate.LocalToGcs(entityData.pos);
            latMin = Math.Min(latMin, gcs.y);
            latMax = Math.Max(latMax, gcs.y);

            latGridMin = Math.Min(latGridMin, entityData.grid.y);
            latGridMax = Math.Max(latGridMax, entityData.grid.y);

            if (gcs.x < 0)
            {
                negativeLongMin = Math.Min(negativeLongMin, gcs.x);
                negativeLongMax = Math.Max(negativeLongMax, gcs.x);
            }
            else
            {
                positiveLongMin = Math.Min(positiveLongMin, gcs.x);
                positiveLongMax = Math.Max(positiveLongMax, gcs.x);
            }
        }

        Debug.LogFormat("lat:{0},{1}", latGridMin, latGridMax);

        float longMin = 0, longMax = 0;
        float negativeAdd = 0;
        bool needFixNegativeLong = false;

        if (negativeLongMin == float.MaxValue && negativeLongMax == float.MinValue)
        {
            //只在正边
            longMin = positiveLongMin;
            longMax = positiveLongMax;
        }
        else if (positiveLongMin == float.MaxValue && positiveLongMax == float.MinValue)
        {
            //只在负边
            longMin = negativeLongMin;
            longMax = negativeLongMax;
        }
        else
        {
            //pMin   pMax   nMIn   nMax
            //nMIn nMax  pMin  pMax

            //跨越
            //pMax -> nMin
            float maxDistance = 2 * Mathf.PI + negativeLongMin - positiveLongMax;
            //nMax->pMin
            float minDistance = positiveLongMin - negativeLongMax;

            if (minDistance <= maxDistance)
            {
                longMin = negativeLongMin;
                longMax = positiveLongMax;
            }
            else
            {
                longMin = positiveLongMin;
                longMax = negativeLongMax + 2 * Mathf.PI;

                needFixNegativeLong = true;
                negativeAdd = 2 * Mathf.PI;
            }
        }

        //Debug.LogFormat("rect:{0},{1},{2},{3}", positiveLongMin, positiveLongMax, negativeLongMin, negativeLongMax);
       // Debug.LogFormat("long:{0},{1}", longMin, longMax);

        NormalizeEntities(data, new Vector3(longMin, latMin), latGridMin);
        //Debug.LogFormat("Bounds:{0}", data.gridBounds);
        //记录经维度
        data.longitude = longMin;
        data.latitude = latMin;

        //记录原位置
        Vector3 normalPos = m_PlanetCoordinate.GcsToNormal(data.longitude, data.latitude);
        data.originalPos = m_PlanetCoordinate.NormalToGround(normalPos);
    }
    private void NormalizeEntities(BPData data, Vector3 originalGcs, float originalLatGrid)
    {
        Vector3 gcs;

        for (int i = 0; i < data.entities.Count; ++i)
        {
            BPEntityData entityData = data.entities[i];
            gcs = m_PlanetCoordinate.LocalToGcs(entityData.pos);
            //偏移经度
            gcs.x = DeltaRadian(originalGcs.x, gcs.x);
            //这里要保证维度是原来的维度。
            //这里的经度已经是偏移的。转换成grid后，就是偏移的grid。
            Vector2 gridOffset = m_PlanetCoordinate.GcsToGrid(gcs);
            //Debug.LogFormat("e:{0},{1},{2}", gcs.x, gcs.y, gridOffset);
            //偏移维度
            gridOffset.y -= originalLatGrid;
            entityData.grid = gridOffset;

            if (entityData.type != BPEntityType.Inserter)
            {
                //rotation
                Quaternion rot = SphericalRotation(entityData.pos, 0);
                // Debug.LogFormat("rot:{0},{1},{2},{3}", rot.x, rot.y, rot.z, rot.w);
                //Debug.LogFormat("erot:{0},{1},{2},{3}", entityData.rot.x, entityData.rot.y, entityData.rot.z, entityData.rot.w);

                Quaternion rotInverse = Quaternion.Inverse(rot);
                entityData.rot = rotInverse * entityData.rot;
            }

            //只有爪子有第二个位置
            if (entityData.type == BPEntityType.Inserter)
            {
                gcs = m_PlanetCoordinate.LocalToGcs(entityData.pos2);
                //偏移经度
                gcs.x = DeltaRadian(originalGcs.x, gcs.x);
                //这里要保证维度是原来的维度。
                //这里的经度已经是偏移的。转换成grid后，就是偏移的grid。
                gridOffset = m_PlanetCoordinate.GcsToGrid(gcs);
                //Debug.LogFormat("e2:{0},{1},{2}", gcs.x, gcs.y, gridOffset);
                //偏移维度
                gridOffset.y -= originalLatGrid;
                entityData.grid2 = gridOffset;

                //rotation
                //rot = SphericalRotation(entityData.pos2, 0);
                ////Debug.LogFormat("rot2:{0},{1},{2},{3}", rot.x, rot.y, rot.z, rot.w);
                ////Debug.LogFormat("erot2:{0},{1},{2},{3}", entityData.rot2.x, entityData.rot2.y, entityData.rot2.z, entityData.rot2.w);
                //rotInverse = Quaternion.Inverse(rot);
                //entityData.rot2 = rotInverse * entityData.rot2;
            }
        }
    }

    public static float Repeat(float t, float length)
    {
        return Clamp(t - Floor(t / length) * length, 0f, length);
    }

    public static float Floor(float f)
    {
        return (float)Math.Floor(f);
    }

    public static float Clamp(float value, float min, float max)
    {
        if (value < min)
        {
            value = min;
        }
        else if (value > max)
        {
            value = max;
        }
        return value;
    }

    public static float DeltaRadian(float current, float target)
    {
        var delta = Repeat(target - current, DoublePI);
        if (delta > PI)
        {
            delta -= DoublePI;
        }
        return delta;
    }

    public static Quaternion SphericalRotation(Vector3 pos, float angle)
    {
        pos.Normalize();
        Vector3 normalized = Vector3.Cross(pos, Vector3.up).normalized;
        Vector3 forward;
        if (normalized.sqrMagnitude < 0.0001f)
        {
            float num = Mathf.Sign(pos.y);
            normalized = Vector3.right * num;
            forward = Vector3.forward * num;
        }
        else
        {
            forward = Vector3.Cross(normalized, pos).normalized;
        }
        if (angle == 0f)
        {
            return Quaternion.LookRotation(forward, pos);
        }
        return Quaternion.LookRotation(forward, pos) * Quaternion.AngleAxis(angle, Vector3.up);
    }
    #region Building

    private void SetBPEnitiies(BPData bpData, BlueprintData blueprintData, float planetRadius, ref Dictionary<int, BeltCopy> entityIdToBeltMap)
    {
        //建筑信息
        bpData.entities = new List<BPEntityData>();
        foreach (var building in blueprintData.copiedBuildings)
        {
            BPEntityData entiyData = new BPEntityData();

            entiyData.entityId = building.originalId;
            entiyData.protoId = building.protoId;
            Vector3 originalPos = (building.cursorRelativePos + blueprintData.referencePos).ToCartesian(planetRadius);
            entiyData.pos = originalPos;
            entiyData.rot = Extention.SphericalRotation(originalPos, building.cursorRelativeYaw);

            //build 设置为None
            entiyData.type = BPEntityType.None;

            entiyData.recipeId = building.recipeId;
            //物流站
            if (building.stationConfig != null)
            {
                SetStationData(entiyData, building);
            }
            //四向分流器
            if (building.splitterSettings != null)
            {
                SetSplitterData(entiyData, building);
            }
            //可堆叠物品
            if (building.altitude > 0)
            {
                SetMultiLevelData(entiyData, building);
            }

            SetEntityGcsGrid(entiyData);
            bpData.entities.Add(entiyData);
        }

        //分捡器
        foreach (var inserter in blueprintData.copiedInserters)
        {
            BPEntityData entiyData = new BPEntityData();

            entiyData.type = BPEntityType.Inserter;
            entiyData.entityId = inserter.originalId;
            entiyData.protoId = inserter.protoId;
            entiyData.pos = (inserter.posDelta + blueprintData.referencePos).ToCartesian(planetRadius);
            entiyData.rot = inserter.rot;
            entiyData.pos2 = (inserter.pos2Delta + blueprintData.referencePos).ToCartesian(planetRadius);
            entiyData.rot2 = inserter.rot2;

            entiyData.pickOffset = inserter.pickOffset;
            entiyData.insertOffset = inserter.insertOffset;
            entiyData.filterId = inserter.filterId;

            SetEntityGcsGrid(entiyData);
            bpData.entities.Add(entiyData);
        }

        //传送带
        foreach (var belt in blueprintData.copiedBelts)
        {
            BPEntityData entiyData = new BPEntityData();

            entiyData.type = BPEntityType.Belt;
            entiyData.entityId = belt.originalId;
            entiyData.protoId = belt.protoId;
            entiyData.pos = (belt.cursorRelativePos + blueprintData.referencePos).ToCartesian(planetRadius);
            entiyData.rot = Quaternion.identity;
            entiyData.offsetGround = belt.altitude;

            SetEntityGcsGrid(entiyData);
            entityIdToBeltMap[belt.originalId] = belt;
            bpData.entities.Add(entiyData);
        }
    }

    private void SetStationData(BPEntityData entityData, BuildingCopy building)
    {
        entityData.paramCount = 2048;
        entityData.parameters = new int[2048];
        int num4 = 0;
        if (building.stationSettings != null)
        {
            foreach(var stationSetting in building.stationSettings)
            {
                int i = stationSetting.index;
                entityData.parameters[num4 + i * 6] = stationSetting.itemId;
                entityData.parameters[num4 + i * 6 + 1] = (int)stationSetting.localLogic;
                entityData.parameters[num4 + i * 6 + 2] = (int)stationSetting.remoteLogic;
                entityData.parameters[num4 + i * 6 + 3] = stationSetting.max;
            }
        }
        num4 += 192;
        if (building.slotFilters != null)
        {
            foreach(var slotFilter in building.slotFilters)
            {
                int j = slotFilter.slotIndex;
                entityData.parameters[num4 + j * 4] = 0;
                entityData.parameters[num4 + j * 4 + 1] = slotFilter.storageIdx;
            }
        }
        num4 += 128;
        if (building.stationConfig!=null)
        {
            var stationConfig = building.stationConfig;
            entityData.parameters[num4] = (int)stationConfig.workEnergyPerTick;
            entityData.parameters[num4 + 1] = _round2int(stationConfig.tripRangeDrones * 100000000.0);
            entityData.parameters[num4 + 2] = _round2int(stationConfig.tripRangeShips / 100.0);
            entityData.parameters[num4 + 3] = (stationConfig.includeOrbitCollector ? 1 : (-1));
            entityData.parameters[num4 + 4] = _round2int(stationConfig.warpEnableDist);
            entityData.parameters[num4 + 5] = (stationConfig.warperNecessary ? 1 : (-1));
            entityData.parameters[num4 + 6] = stationConfig.deliveryDrones;
            entityData.parameters[num4 + 7] = stationConfig.deliveryShips;
        }
        num4 += 64;
    }

    private void SetSplitterData(BPEntityData entityData, BuildingCopy building)
    {
        if (building.splitterSettings != null)
        {
            entityData.filterId = building.splitterSettings.outFilter;

            entityData.paramCount = 4;
            entityData.parameters = new int[entityData.paramCount];

            if (building.splitterSettings.inPriority)
            {
                entityData.parameters[building.splitterSettings.inPrioritySlot] = 1;
            }
            if (building.splitterSettings.outPriority)
            {
                entityData.parameters[building.splitterSettings.outPrioritySlot] = 1;
            }
        }
    }

    private void SetMultiLevelData(BPEntityData entityData, BuildingCopy building)
    {
        Vector3 lapJoint;
        if (m_ProtoLapJoints.TryGetValue(building.protoId, out lapJoint))
        {
            entityData.offsetGround = building.altitude * lapJoint.y;
        }
    }

    private void SetEntityGcsGrid(BPEntityData entityData)
    {
        //直角坐标转换成格子坐标。保留原坐标，比对作用。
        entityData.grid = m_PlanetCoordinate.LocalToGrid(entityData.pos);

        if (entityData.type == BPEntityType.Inserter)
        {
            entityData.offsetGround = Mathf.Max(0, entityData.pos.magnitude - m_PlanetCoordinate.radius - 0.2f);
            entityData.grid2 = m_PlanetCoordinate.LocalToGrid(entityData.pos2);
            entityData.offsetGround2 = Mathf.Max(0, entityData.pos2.magnitude - m_PlanetCoordinate.radius - 0.2f);
        }
    }
    #endregion //Building

    #region Connect

    private void SetBPConnects(BPData bpData, BlueprintData blueprintData, float planetRadius, Dictionary<int, BeltCopy> entityIdToBeltMap)
    {
        bpData.connects = new List<ConnectData>();
        //处理建筑连接
        foreach (var building in blueprintData.copiedBuildings)
        {
            if (building.connectedBuildingId > 0)
            {
                ConnectData connect = new ConnectData();
                connect.fromObjId = building.originalId;
                connect.toObjId = building.connectedBuildingId;
                connect.fromSlot = 14;
                connect.toSlot = 15;
                connect.isOutput = false;
                bpData.connects.Add(connect);
            }
        }

        //处理分捡器连接
        foreach (var inseter in blueprintData.copiedInserters)
        {
            //input
            ConnectData inputConn = new ConnectData();
            inputConn.fromObjId = inseter.originalId;
            inputConn.toObjId = inseter.pickTarget;
            inputConn.fromSlot = 1;
            inputConn.toSlot = inseter.startSlot;
            inputConn.isOutput = false;
            bpData.connects.Add(inputConn);

            //output
            ConnectData outputConn = new ConnectData();
            outputConn.fromObjId = inseter.originalId;
            outputConn.toObjId = inseter.insertTarget;
            outputConn.fromSlot = 0;
            outputConn.toSlot = inseter.endSlot;
            outputConn.isOutput = true;
            bpData.connects.Add(outputConn);
        }

        //处理belt连接
        foreach (var belt in blueprintData.copiedBelts)
        {
            //传送带之间只处理output。
            if (belt.outputId > 0)
            {
                ConnectData outputConn = new ConnectData();
                outputConn.fromObjId = belt.originalId;
                outputConn.toObjId = belt.outputId;
                outputConn.fromSlot = 0;
                outputConn.isOutput = true;
                outputConn.toSlot = -1;
                BeltCopy outBelt;
                if (entityIdToBeltMap.TryGetValue(belt.outputId, out outBelt))
                {
                    if (outBelt.backInputId == belt.originalId)
                    {
                        outputConn.toSlot = 1;
                    }
                    if (outBelt.leftInputId == belt.originalId)
                    {
                        outputConn.toSlot = 2;
                    }
                    if (outBelt.rightInputId == belt.originalId)
                    {
                        outputConn.toSlot = 3;
                    }
                }
                else
                {
                    Debug.LogFormat("Can't find output id:{0}", belt.outputId);
                }
                bpData.connects.Add(outputConn);
            }

            //处理建筑的连接
            if (belt.connectedBuildingId > 0)
            {
                if (belt.connectedBuildingIsOutput)
                {
                    //output
                    ConnectData outputConn = new ConnectData();
                    outputConn.fromObjId = belt.originalId;
                    outputConn.toObjId = belt.connectedBuildingId;
                    outputConn.fromSlot = 0;
                    outputConn.toSlot = belt.connectedBuildingSlot;
                    outputConn.isOutput = true;
                    bpData.connects.Add(outputConn);
                }
                else
                {
                    //input
                    ConnectData inputConn = new ConnectData();
                    inputConn.fromObjId = belt.originalId;
                    inputConn.toObjId = belt.connectedBuildingId;
                    inputConn.fromSlot = 1;
                    inputConn.toSlot = belt.connectedBuildingSlot;
                    inputConn.isOutput = false;
                    bpData.connects.Add(inputConn);
                }
            }
        }
    }

    #endregion //Connect
    private void ShowMessage(string msg)
    {
        m_MessageText.text = msg;
    }

    private int _round2int(double d)
    {
        if (!(d > 0.0))
        {
            return (int)(d - 0.5);
        }
        return (int)(d + 0.5);
    }
}
