using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DspTrarck
{
    public struct BluePrintFile
    {
        public string name;
        public string filepath;

        public BluePrintFile(string name, string filepath)
        {
            this.name = name;
            this.filepath = filepath;
        }
    }

    public class FactoryBPUI
    {
        private Rect m_UINormalRect = new Rect(30, 160, 200, 300);
        private Rect m_UIMinilRect = new Rect(5, 200, 30, 30);

        private string m_BPName="";

        private bool m_CopyAdd;
        private bool m_WithoutBelt;
        private bool m_WithoutPowerNode;

        private List<BluePrintFile> m_BPFiles;

        private Vector2 m_BPFilesScrollPos;

        private bool m_Mini = false;

        public FactoryBP factoryBP;

        public string bpName
        {
            get
            {
                return m_BPName;
            }
            set
            {
                m_BPName = value;
            }
        }

        public bool isCopyAdd
        {
            get
            {
                return m_CopyAdd;
            }
            set
            {
                m_CopyAdd = value;
            }
        }

        public bool isWithoutBelt => m_WithoutBelt;

        public bool isWithoutPowerNode => m_WithoutPowerNode;
        public void Init(FactoryBP fbp)
        {
            factoryBP = fbp;
        }

        public void Clear()
        {
            m_BPName = "";
            m_CopyAdd = false;
            m_WithoutBelt = false;
            m_WithoutPowerNode = false;
            if (m_BPFiles != null)
            {
                m_BPFiles.Clear();
            }

            m_BPFilesScrollPos = Vector2.zero;
            m_Mini = false;

        }

        public void OnGUI()
        {
            if (m_Mini)
            {
                ShowMIni();
            }
            else
            {
                ShowNormal();
            }
        }

        private void ShowMIni()
        {
            GUIStyle miniBtnSytle = new GUIStyle(GUI.skin.label);
            miniBtnSytle.fontSize = 28;
            if (GUI.Button(m_UIMinilRect, ">", miniBtnSytle))
            {
                m_Mini = false;
            }
        }

        private void ShowNormal()
        {
            GUIStyle miniBtnSytle = new GUIStyle(GUI.skin.label);
            miniBtnSytle.fontSize = 28;
            if (GUI.Button(m_UIMinilRect, "<", miniBtnSytle))
            {
                m_Mini = true;
            }

            GUILayout.BeginArea(m_UINormalRect, GUI.skin.box);
            {
                //BP
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("BP", GUILayout.ExpandWidth(false));
                    m_BPName = GUILayout.TextField(m_BPName, GUILayout.Width(100));
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(5);

                //copy
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Create");
                    //m_CopyAdd = GUILayout.Toggle(m_CopyAdd, "Add");
                    m_WithoutBelt = GUILayout.Toggle(m_WithoutBelt, "NoBeil");
                    m_WithoutPowerNode = GUILayout.Toggle(m_WithoutPowerNode, "NoPower");
                }
                GUILayout.EndHorizontal();

                //action
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Save"))
                    {
                        SaveBPFile();
                    }

                    if (GUILayout.Button("Reresh"))
                    {
                        RefreshBPFiles();
                    }
                }
                GUILayout.EndHorizontal();

                //bpfiles
                ShowBPFiles();
            }
            GUILayout.EndArea();
        }

        private void ShowBPFiles()
        {
            if (m_BPFiles != null && m_BPFiles.Count > 0)
            {
                m_BPFilesScrollPos = GUILayout.BeginScrollView(m_BPFilesScrollPos, GUILayout.Height(200));

                GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
                labelStyle.clipping = TextClipping.Clip;
                for (int i = 0; i < m_BPFiles.Count; ++i)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(m_BPFiles[i].name, labelStyle, GUILayout.MaxWidth(160));
                    if (GUILayout.Button("L"))
                    {
                        LoadBPFile(m_BPFiles[i].filepath);
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
            }

        }

        public void SaveBPFile()
        {
            if (factoryBP.currentData == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(m_BPName))
            {
                factoryBP.currentData.name = m_BPName;
            }

            string filePath= factoryBP.SaveBPData(factoryBP.currentData);
            string name = YH.FileSystem.Relative(filePath, factoryBP.bpDir);
            m_BPFiles.Add(new BluePrintFile(name,filePath));
        }

        private void LoadBPFile(string bpFile)
        {
            m_BPName = Path.GetFileNameWithoutExtension(bpFile);
            factoryBP.LoadCurrentData(bpFile);
        }

        private void RefreshBPFiles()
        {
            if (m_BPFiles == null)
            {
                m_BPFiles = new List<BluePrintFile>();
            }
            else
            {
                m_BPFiles.Clear();
            }

            string[] files = Directory.GetFiles(factoryBP.bpDir);
            foreach (var f in files)
            {
                m_BPFiles.Add(new BluePrintFile(YH.FileSystem.Relative(f,factoryBP.bpDir),f));
            }
        }
    }

}
