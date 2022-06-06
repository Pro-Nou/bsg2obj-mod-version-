using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Modding;
using Modding.Blocks;
using InternalModding;
using Modding.Common;
using UnityEngine;

namespace bsg2obj
{
    class exporterControl : SingleInstance<exporterControl>
    {
        public override string Name { get; } = "exporter Control";
        private Rect windowRect = new Rect(300f, 100f, 128f, 145f);
        private readonly int windowID = ModUtility.GetWindowId();
        private bool onlyVis = false;
        private bool exportEmiss = false;
        private Modding.Levels.Level LVL;
        private void Awake()
        {

            //加载配置
            //Events.OnMachineLoaded += LoadConfiguration;
            //Events.OnMachineLoaded += (pmi) => { PMI = pmi; };
            Events.OnLevelLoaded += (lvl) => { LVL = lvl; };
        }
            private void OnGUI()
        {
            if (StatMaster.isMainMenu)
                return;
            if (!StatMaster.hudHidden && StatMaster.isMP) 
            {
                windowRect = GUILayout.Window(windowID, windowRect, new GUI.WindowFunction(toolWindow), "bsg2obj");
            }
        }
        private void toolWindow(int windowID)
        {
            GUILayout.BeginHorizontal();
            if (GUI.Button(new Rect(5f, 20f, 118f, 20f), "Export Machine"))
            {
                exportMachine(PlayerData.localPlayer.machine.gameObject);
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            onlyVis = GUI.Toggle(new Rect(5f, 45f, 118f, 20f), onlyVis, "Only export Vis");
            exportEmiss = GUI.Toggle(new Rect(5f, 70f, 118f, 20f), exportEmiss, "Export emissmap");
            //onlyVis = GUILayout.Toggle(onlyVis, "Only export Vis");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUI.Button(new Rect(5f, 95f, 118f, 20f), "Export Level"))
            {
                try
                {
                    exportLevel(GameObject.Find("MULTIPLAYER LEVEL"));
                }
                catch { }
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUI.Button(new Rect(5f, 120f, 118f, 20f), "Open Folder"))
            {
                Modding.ModIO.OpenFolderInFileBrowser("", true);
            }
            GUILayout.EndHorizontal();
            GUI.DragWindow();
        }
        public void exportLevel(GameObject tgtLevel)
        {
            string lvlName = tgtLevel.name;
            try
            {
                lvlName += " " + LVL.Setup.Name;
            }
            catch { }
            string folderName = lvlName + "(" + DateTime.Now.ToString().Replace('/', '_').Replace(':', '_') + ")";
            Modding.ModIO.CreateDirectory(folderName, true);
            System.IO.TextWriter objWriter = Modding.ModIO.CreateText(folderName + "/" + lvlName + ".obj", true);
            System.IO.TextWriter mtlWriter = Modding.ModIO.CreateText(folderName + "/" + lvlName + ".mtl", true);
            objWriter.WriteLine("#Exported by bsg2obj");
            objWriter.WriteLine("mtllib " + lvlName + ".mtl");
            mtlWriter.WriteLine("#Exported by bsg2obj");
            onlyVis = false;
            //exportEmiss = true;
            long vcount = 0;
            DFSexport(tgtLevel.transform, objWriter, mtlWriter, vcount, folderName);
            objWriter.Close();
            mtlWriter.Close();
        }
        public void exportMachine(GameObject tgtMachine)
        {
            string folderName = tgtMachine.name + "(" + DateTime.Now.ToString().Replace('/', '_').Replace(':', '_') + ")";
            Modding.ModIO.CreateDirectory(folderName, true);
            System.IO.TextWriter objWriter = Modding.ModIO.CreateText(folderName + "/" + tgtMachine.name + ".obj", true);
            System.IO.TextWriter mtlWriter = Modding.ModIO.CreateText(folderName + "/" + tgtMachine.name + ".mtl", true);
            objWriter.WriteLine("#Exported by bsg2obj");
            objWriter.WriteLine("mtllib " + tgtMachine.name + ".mtl");
            mtlWriter.WriteLine("#Exported by bsg2obj");
            long vcount = 0;
            foreach(var a in PlayerData.localPlayer.machine.BuildingBlocks)
            {
                if (onlyVis)
                {
                    if(a.BlockID!=(int)BlockType.CameraBlock&& a.BlockID != (int)BlockType.BuildEdge&& a.BlockID != (int)BlockType.BuildNode&& a.BlockID != (int)BlockType.Pin)
                    vcount = DFSexport(a.transform, objWriter, mtlWriter, vcount, folderName);
                }
                else
                    vcount = DFSexport(a.transform, objWriter, mtlWriter, vcount, folderName);
            }
            objWriter.Close();
            mtlWriter.Close();
        }
        public long DFSexport(Transform tgtTransform, System.IO.TextWriter objWriter, System.IO.TextWriter mtlWriter, long vcount, string folderName)
        {
            long vcounts = vcount;
            bool shouldExport = true;
            if(onlyVis)
            {
                if (tgtTransform.gameObject.name.Contains("Vis") || tgtTransform.gameObject.name == "A" || tgtTransform.gameObject.name == "B" || tgtTransform.gameObject.name == "Cylinder")
                    shouldExport = true;
                else
                    shouldExport = false;
            }
            if (shouldExport)
            {
                try
                {
                    MeshFilter thisMesh = tgtTransform.GetComponent<MeshFilter>();
                    MeshRenderer thisRenderer = tgtTransform.GetComponent<MeshRenderer>();
                    /*
                    if(tgtTransform.gameObject.name== "BuildZone")
                    {
                        try
                        {
                            Debug.Log(thisMesh.name);
                            Debug.Log(thisRenderer.name);
                            Debug.Log(thisRenderer.enabled.ToString());
                            Debug.Log(thisMesh.mesh.vertexCount.ToString());
                            Debug.Log(tgtTransform.gameObject.activeSelf.ToString());
                        }
                        catch { }
                    }
                    */
                    if (thisMesh != null && thisRenderer != null)
                    {
                        if (thisRenderer.enabled && thisMesh.mesh.vertexCount > 0 && tgtTransform.gameObject.activeSelf && thisMesh.mesh.isReadable)
                        {
                            objWriter.WriteLine(" ");
                            objWriter.WriteLine("o " + tgtTransform.name + "_" + thisMesh.GetHashCode().ToString());
                            
                            Vector3[] varray = thisMesh.mesh.vertices;
                            Vector2[] uvarray = thisMesh.mesh.uv;
                            Vector3[] narray = thisMesh.mesh.normals;
                            int[] tarray = thisMesh.mesh.triangles;
                            for (int i = 0; i < varray.Length; i++)
                            {
                                Vector3 worldPos = tgtTransform.TransformPoint(varray[i]);
                                objWriter.WriteLine("v " + worldPos.x.ToString("f6") + " " + worldPos.y.ToString("f6") + " " + worldPos.z.ToString("f6"));
                            }
                            for (int i = 0; i < uvarray.Length; i++)
                            {
                                objWriter.WriteLine("vt " + uvarray[i].x.ToString("f6") + " " + uvarray[i].y.ToString("f6"));
                            }
                            if (varray.Length > uvarray.Length)
                            {
                                for (int j = 0; j < thisMesh.mesh.vertexCount - uvarray.Length; j++)
                                {
                                    objWriter.WriteLine("vt " + 0f.ToString("f6") + " " + 0f.ToString("f6"));
                                }
                            }
                            for (int i = 0; i < narray.Length; i++)
                            {
                                Vector3 worldNormal = tgtTransform.TransformDirection(narray[i]);
                                objWriter.WriteLine("vn " + worldNormal.x.ToString("f6") + " " + worldNormal.y.ToString("f6") + " " + worldNormal.z.ToString("f6"));
                            }
                            objWriter.WriteLine("usemtl mat" + thisRenderer.GetHashCode().ToString());
                            objWriter.WriteLine("s off");
                            for (int i = 0; i < tarray.Length; i += 3)
                            {
                                string a = (tarray[i] + 1 + vcount).ToString();
                                string b = (tarray[i + 1] + 1 + vcount).ToString();
                                string c = (tarray[i + 2] + 1 + vcount).ToString();
                                objWriter.WriteLine("f " + a + "/" + a + "/" + a + " " + b + "/" + b + "/" + b + " " + c + "/" + c + "/" + c);
                            }
                            vcounts += varray.Length;

                            mtlWriter.WriteLine(" ");
                            mtlWriter.WriteLine("newmtl mat" + thisRenderer.GetHashCode().ToString());
                            Color thisColor = thisRenderer.material.GetColor("_Color");
                            mtlWriter.WriteLine("Kd " + thisColor.r.ToString() + " " + thisColor.g.ToString() + " " + thisColor.b.ToString());
                            try
                            {
                                Color thisEmisColor = thisRenderer.material.GetColor("_EmissCol");
                                mtlWriter.WriteLine("Ke " + thisEmisColor.r.ToString() + " " + thisEmisColor.g.ToString() + " " + thisEmisColor.b.ToString());
                            }
                            catch { }
                            try
                            {
                                Color thisEmisColor = thisRenderer.material.GetColor("_EmissionColor");
                                mtlWriter.WriteLine("Ke " + thisEmisColor.r.ToString() + " " + thisEmisColor.g.ToString() + " " + thisEmisColor.b.ToString());
                            }
                            catch { }
                            if (thisRenderer.material.mainTexture != null)
                            {
                                try
                                {
                                    string texname = thisRenderer.material.mainTexture.GetHashCode().ToString() + ".png";
                                    if (!Modding.ModIO.ExistsFile(folderName + "/" + texname, true))
                                    {
                                        ExportPNG(thisRenderer.material.mainTexture, texname, folderName);
                                    }
                                    //mtlWriter.WriteLine("Ks " + thisColor.r.ToString() + " " + thisColor.g.ToString() + " " + thisColor.b.ToString());
                                    mtlWriter.WriteLine("map_Kd " + texname);
                                }
                                catch { }
                            }
                            if (thisRenderer.material.GetTexture("_EmissMap") != null && exportEmiss)
                            {
                                try
                                {
                                    string texname = thisRenderer.material.GetTexture("_EmissMap").GetHashCode().ToString() + ".png";
                                    if (!Modding.ModIO.ExistsFile(folderName + "/" + texname, true))
                                    {
                                        ExportPNG(thisRenderer.material.GetTexture("_EmissMap"), texname, folderName);
                                    }
                                    //mtlWriter.WriteLine("Ks " + thisColor.r.ToString() + " " + thisColor.g.ToString() + " " + thisColor.b.ToString());
                                    mtlWriter.WriteLine("map_Ke " + texname);
                                }
                                catch { }
                            }
                            if (thisRenderer.material.GetTexture("_EmissionMap") != null && exportEmiss)
                            {
                                try
                                {
                                    string texname = thisRenderer.material.GetTexture("_EmissionMap").GetHashCode().ToString() + ".png";
                                    if (!Modding.ModIO.ExistsFile(folderName + "/" + texname, true))
                                    {
                                        ExportPNG(thisRenderer.material.GetTexture("_EmissionMap"), texname, folderName);
                                    }
                                    //mtlWriter.WriteLine("Ks " + thisColor.r.ToString() + " " + thisColor.g.ToString() + " " + thisColor.b.ToString());
                                    mtlWriter.WriteLine("map_Ke " + texname);
                                }
                                catch { }
                            }
                            if (thisRenderer.material.GetTexture("_BumpMap") != null)
                            {
                                try
                                {
                                    string texname = thisRenderer.material.GetTexture("_BumpMap").GetHashCode().ToString() + "Bump.png";
                                    if (!Modding.ModIO.ExistsFile(folderName + "/" + texname, true))
                                    {
                                        ExportPNG(thisRenderer.material.GetTexture("_BumpMap"), texname, folderName);
                                    }
                                    //mtlWriter.WriteLine("Ks " + thisColor.r.ToString() + " " + thisColor.g.ToString() + " " + thisColor.b.ToString());
                                    mtlWriter.WriteLine("map_Bump " + texname);
                                }
                                catch { }
                            }
                        }
                    }
                }
                catch { }
            }
            if(tgtTransform.childCount>0)
            {
                for(int i=0;i< tgtTransform.childCount;i++)
                {
                    if (tgtTransform.GetChild(i).gameObject.name == "Shadow")
                        continue;
                    if (tgtTransform.GetChild(i).gameObject.activeSelf)
                        vcounts = DFSexport(tgtTransform.GetChild(i), objWriter, mtlWriter, vcounts, folderName);
                }
            }
            return vcounts;
        }
        public void ExportPNG(Texture tgtTex, string texName, string folderName)
        {
            RenderTexture tmp = RenderTexture.GetTemporary(
                      tgtTex.width,
                     tgtTex.height,
                     0,
                    RenderTextureFormat.Default,
                      RenderTextureReadWrite.Linear);

            Graphics.Blit(tgtTex, tmp);

            RenderTexture previous = RenderTexture.active;

            RenderTexture.active = tmp;

            Texture2D myTexture2D = new Texture2D(tgtTex.width, tgtTex.height);

            myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            myTexture2D.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(tmp);

            Byte[] bytes = myTexture2D.EncodeToPNG();
            Modding.ModIO.WriteAllBytes(folderName + "/" + texName, bytes, true);
        }
    }
}
