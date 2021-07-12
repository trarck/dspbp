using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using DspTrarck;

public class ConvertMyBP : MonoBehaviour
{
    [SerializeField]
    InputField m_BPBinaryFileInput;

    private string GetSaveDir()
    {
#if UNITY_EDITOR
        string saveDir = Path.Combine(Application.dataPath, "../json");
#else
           string saveDir = Path.Combine(Application.dataPath, "json");
#endif
        if (!Directory.Exists(saveDir))
        {
            Directory.CreateDirectory(saveDir);
        }
        return saveDir;
    }

    public void ConverToJson()
    {
        string binFile = m_BPBinaryFileInput.text;
        if (string.IsNullOrEmpty(binFile) || !File.Exists(binFile))
        {
            return;
        }

        BPData bpData = BPDataReader.ReadBPDataFromFile(binFile);
        if (bpData == null)
        {
            return;
        }

        string jsonStr = JsonUtility.ToJson(bpData,true);
        string saveDir = GetSaveDir();
        string jsonFile=Path.Combine(saveDir,Path.GetFileNameWithoutExtension(binFile)+".json");
        File.WriteAllText(jsonFile, jsonStr);
    }
}
