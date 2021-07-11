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
        string latJointFile = System.IO.Path.Combine(Application.persistentDataPath, "latJoint.json");
        string cnt = File.ReadAllText(latJointFile);
        LapJointAsset lapJointAsset = JsonUtility.FromJson<LapJointAsset>(cnt);
        if (lapJointAsset != null)
        {
            foreach (var node in lapJointAsset.lapJoints)
            {
                m_ProtoLapJoints[node.protoId] = node.lapJoint;
            }
        }
    }

    public void DoConvert()
    {
        string bpStringFile = m_BPFileInput.text;
        if (string.IsNullOrEmpty(bpStringFile))
        {
            ShowMessage("file name is empty");
            return;
        }

        //dataFile = string.IsNullOrEmpty(bpStringFile) ? "D:\\csharp\\ConvertMBData\\ConvertMBData\\t.txt" : dataFile;// args[1];
        string dataContext = File.ReadAllText(bpStringFile);
        BlueprintData data = BlueprintData.Import(dataContext);
        if (data == null)
        {
            ShowMessage("BlueprintData import error");
            return;
        }

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

        //保存
        string saveDir = Path.Combine(Application.dataPath, "bp");
        if (!Directory.Exists(saveDir))
        {
            Directory.CreateDirectory(saveDir);
        }

        string bpFile = Path.Combine(saveDir, bpData.name);
        BPDataWriter.WriteBPDataToFile(bpFile, bpData);
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
            bpData.name = GetDefaultName();
            m_BPFileInput.text = bpData.name;
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

            entityIdToBeltMap[belt.originalId] = belt;
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
                var outBelt = entityIdToBeltMap[belt.outputId];
                if (outBelt != null)
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
