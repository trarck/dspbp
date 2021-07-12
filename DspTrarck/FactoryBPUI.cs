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
        private Rect m_UINormalRect = new Rect(30, 160, 200, 350);
        private Rect m_UIMinilRect = new Rect(5, 200, 30, 30);

        private string m_BPName="";

        private bool m_CopyAdd;
        private bool m_WithoutBelt;
        private bool m_WithoutPowerNode;

        private bool m_ShowConnectNode = false;

        private List<BluePrintFile> m_BPFiles;

        private Vector2 m_BPFilesScrollPos;
        private string m_PageItemCountStr;
        private string m_PageIndexStr;
        private int m_PageItemCount = 100;
        private int m_PageIndex = 1;
        private string m_SearchContext = null;
        private List<BluePrintFile> m_SearchedBPFiles;

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
        public bool isShowConnectNode => m_ShowConnectNode;

        public void Init(FactoryBP fbp)
        {
            factoryBP = fbp;
            m_BPFiles = new List<BluePrintFile>();
            m_SearchedBPFiles = new List<BluePrintFile>();
            m_PageItemCountStr = m_PageItemCount.ToString();
            m_PageIndexStr = m_PageIndex.ToString();
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

            if (m_SearchedBPFiles != null)
            {
                m_SearchedBPFiles.Clear();
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

                //create
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Create");
                    //m_CopyAdd = GUILayout.Toggle(m_CopyAdd, "Add");
                    m_WithoutBelt = GUILayout.Toggle(m_WithoutBelt, "NoBelt");
                    m_WithoutPowerNode = GUILayout.Toggle(m_WithoutPowerNode, "NoPower");
                }
                GUILayout.EndHorizontal();

                //build
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("show");
                    m_ShowConnectNode = GUILayout.Toggle(m_ShowConnectNode, "Conn");
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
                List<BluePrintFile> bluePrintFiles = m_BPFiles;
                //search
                GUILayout.BeginHorizontal();
                {
                    m_SearchContext = GUILayout.TextField(m_SearchContext);
                    if (GUILayout.Button("search",GUILayout.Width(60)))
                    {
                        if (!string.IsNullOrEmpty(m_SearchContext))
                        {
                            bluePrintFiles = SearchBPFiles(m_SearchContext);
                        }
                    }
                }
                GUILayout.EndHorizontal();

                //page
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("P:");
                    string pageIndexStr = GUILayout.TextField(m_PageIndexStr);
                    if (pageIndexStr != m_PageIndexStr)
                    {
                        m_PageIndexStr = pageIndexStr;

                        if (!string.IsNullOrEmpty(m_PageIndexStr))
                        {
                            int.TryParse(m_PageIndexStr, out m_PageIndex);
                        }
                    }

                    int maxPage = Mathf.CeilToInt((float)bluePrintFiles.Count / m_PageItemCount);
                    GUILayout.Label(
                        string.Format("/{0}:{1}", maxPage, bluePrintFiles.Count)
                    );
                    //pre
                    if (GUILayout.Button("<"))
                    {
                        --m_PageIndex;
                        if (m_PageIndex < 1)
                        {
                            m_PageIndex = 1;
                        }
                        m_PageIndexStr = m_PageIndex.ToString();
                    }
                    //next
                    if (GUILayout.Button(">"))
                    {
                        ++m_PageIndex;
                        if (m_PageIndex >maxPage)
                        {
                            m_PageIndex = maxPage;
                        }
                        m_PageIndexStr = m_PageIndex.ToString();
                    }
                    GUILayout.Label("PC:");
                    string pageItemCountStr = GUILayout.TextField(m_PageItemCountStr);
                    if (pageItemCountStr != m_PageItemCountStr)
                    {
                        m_PageItemCountStr = pageItemCountStr;
                        if (!string.IsNullOrEmpty(m_PageItemCountStr))
                        {
                            int.TryParse(m_PageItemCountStr, out m_PageItemCount);
                            if (m_PageItemCount == 0)
                            {
                                m_PageItemCount = 100;
                            }
                            m_PageIndex = 1;
                            m_PageIndexStr = m_PageIndex.ToString();
                        }
                    }
                }
                GUILayout.EndHorizontal();

                m_BPFilesScrollPos = GUILayout.BeginScrollView(m_BPFilesScrollPos, GUILayout.Height(200));

                GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
                labelStyle.clipping = TextClipping.Clip;
                int startIndex = Mathf.Max((m_PageIndex-1) * m_PageItemCount,0);
                int endIndex = Mathf.Min(m_PageIndex * m_PageItemCount, bluePrintFiles.Count);
                for (int i = startIndex; i < endIndex; ++i)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(bluePrintFiles[i].name, labelStyle, GUILayout.MaxWidth(160));
                    if (GUILayout.Button("L"))
                    {
                        LoadBPFile(bluePrintFiles[i].filepath);
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
            }
        }

        private List<BluePrintFile> SearchBPFiles(string filter)
        {
            m_SearchedBPFiles.Clear();
            foreach (var bpf in m_BPFiles)
            {
                if (bpf.name.Contains(filter))
                {
                    m_SearchedBPFiles.Add(bpf);
                }
            }
            return m_SearchedBPFiles;
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
            string name = YH.FileSystem.Relative(factoryBP.bpDir, filePath);
            //check name exits
            foreach (var bpf in m_BPFiles)
            {
                if (bpf.name == name)
                {
                    return;
                }
            }

            m_BPFiles.Add(new BluePrintFile(name,filePath));
        }

        private void LoadBPFile(string bpFile)
        {
            m_BPName = Path.GetFileNameWithoutExtension(bpFile);
            factoryBP.LoadCurrentData(bpFile);
        }
        public void RefreshBPFiles()
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
                m_BPFiles.Add(new BluePrintFile(YH.FileSystem.Relative(factoryBP.bpDir,f),f));
            }
        }
    }

}
