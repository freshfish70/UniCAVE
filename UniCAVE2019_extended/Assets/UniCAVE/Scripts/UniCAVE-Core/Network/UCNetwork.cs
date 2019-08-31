﻿//MIT License
//Copyright 2016-Present 
//Ross Tredinnick
//Benny Wysong-Grass
//University of Wisconsin - Madison Virtual Environments Group
//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), 
//to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, 
//sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif


[NetworkSettings(channel = 1, sendInterval = 0.016f)]
public class UCNetwork : NetworkBehaviour
{

    [Tooltip("This object will be transformed by this script")]
    public HeadConfiguration head;

    [TextArea(20, 20)]
    public string launchScript;

    private float lastTime = 0.0f;
    private bool syncedRandomSeed = false;
    private int frameCount = 0;

    void Update()
    {
        if (isServer)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                RpcQuitApplication();
                Application.Quit();
            }

            if (head != null)
                RpcSetTransforms(transform.position, transform.rotation, head.transform.position, head.transform.rotation);
            else
                RpcSetTransforms(transform.position, transform.rotation, Vector3.zero, Quaternion.identity);

            RpcSetTime(Time.time);

            if (!syncedRandomSeed && frameCount > 600)
            {
                //don't sync this until all connections have occurred.
                /*NetworkManager m = gameObject.transform.parent.GetComponent<NetworkManager>();
				if(m != null)
				{
					if (m.connections.Length == numSlaveNodes)
					{*/
                RpcSetRandomSeed(UnityEngine.Random.seed);
                syncedRandomSeed = true;
                //}
                //}
            }

            frameCount++;
        }
    }

    [ClientRpc]
    void RpcSetTransforms(Vector3 myPos, Quaternion myOri, Vector3 headPos, Quaternion headOri)
    {
        transform.SetPositionAndRotation(myPos, myOri);
        if (head != null)
            head.transform.SetPositionAndRotation(headPos, headOri);
    }

    [ClientRpc]
    void RpcSetTime(float canonicalTime)
    {
        if (lastTime == 0.0f)
        {
            lastTime = canonicalTime;
        }
        else
        {
            float ourTime = Time.time;
            float timeDiff = canonicalTime - lastTime;
            float scale = ((canonicalTime - ourTime) + timeDiff) / timeDiff;

            lastTime = canonicalTime;

            if (scale < 0.0f)
            {
                scale = 0.01f;
            }
            else if (scale > 100.0f)
            {
                scale = 100.0f;
            }

            Time.timeScale = scale;
        }
    }

    [ClientRpc]
    void RpcSetRandomSeed(int seed)
    {
        Debug.LogError("Synced random seed to " + seed);
        ParticleSystem[] particleSystems = FindObjectsOfType<ParticleSystem>();
        foreach (ParticleSystem ps in particleSystems)
        {
            ps.Stop();
        }

        Random.InitState(seed);

        foreach (ParticleSystem ps in particleSystems)
        {
            ps.useAutoRandomSeed = false;
            ps.randomSeed = (uint)seed;
            ps.Simulate(0.0f, true, true, false);
            ps.Play();
        }
    }

    [ClientRpc]
    void RpcQuitApplication()
    {
        Application.Quit();
    }


    [Tooltip("You can load PhysicalDisplay settings for all children recursively, right click the name of this script and settings will be loaded from this file path")]
    public string settingsToLoad;
    [ContextMenu("Load Settings For All Children")]
    void LoadSettingsChildren()
    {
        LoadSettingsChildren_h(null);
    }
    void LoadSettingsChildren_h(GameObject it = null)
    {
        if (it == null) it = gameObject;

        for (int i = 0; i < it.transform.childCount; i++)
        {
            GameObject child = it.transform.GetChild(i).gameObject;
            if (child.GetComponent<PhysicalDisplay>() != null)
            {
                child.GetComponent<PhysicalDisplay>().TryToDeSerialize(settingsToLoad);
            }
            LoadSettingsChildren_h(child);
        }
    }

    public List<PhysicalDisplay> GetAllDisplays()
    {
        List<PhysicalDisplay> disps = new List<PhysicalDisplay>();
        List<PhysicalDisplayManager> managers = new List<PhysicalDisplayManager>();
        IterateAllRelevantChildren(gameObject, disps, managers);
        return disps;
    }

    public string GenerateLaunchScript()
    {
        List<PhysicalDisplay> displays = new List<PhysicalDisplay>();
        List<PhysicalDisplayManager> managers = new List<PhysicalDisplayManager>();
        IterateAllRelevantChildren(gameObject, displays, managers);

        bool headStarted = false;

        string res = "# Windows Powershell Launch Script\n";
        res += "# Script Generated On " + System.DateTime.Now.ToLongDateString() + ", " + System.DateTime.Now.ToLongTimeString() + "\n";
        res += "# Setup contains " + displays.Count + " displays and " + managers.Count + " display managers";

        for (int i = 0; i < displays.Count; ++i)
        {
            if (displays[i].manager != null) continue;

            res += "\n\n# Display: " + displays[i].name;
            res += "\nIf ($env:ComputerName -eq '" + displays[i].machineName + "') {";
            if (displays[i].dualPipe && displays[i].dualInstance)
            {
                for (int j = 0; j < 2; j++)
                {
                    res += "\n\t& '.\\" + Application.productName + ".exe'";
                    res += " " + (displays[i].exclusiveFullscreen ? "-screen-fullscreen 1 -adapter " + displays[i].display : "-screen-fullscreen 0 -popupwindow");
                    res += " " + ((displays[i].is3D && !displays[i].dualPipe) ? "-vrmode stereo" : "");
                    res += " " + "eye " + (j == 0 ? "left" : "right");
                }
            }
            else
            {
                res += "\n\t& '.\\" + Application.productName + ".exe'";
                res += " " + (displays[i].exclusiveFullscreen ? "-screen-fullscreen 1 -adapter " + displays[i].display : "-screen-fullscreen 0 -popupwindow");
                res += " " + ((displays[i].is3D && !displays[i].dualPipe) ? "-vrmode stereo" : "");
            }

            res += "\n}";
        }

        for (int i = 0; i < managers.Count; i++)
        {
            res += "\n\n# Display Group: " + managers[i].name;
            res += "\nIf ($env:ComputerName -eq '" + managers[i].machineName + "') {";

            res += "\n\t& '.\\" + Application.productName + ".exe'";
            res += " " + (managers[i].fullscreen ? ("-screen-fullscreen 1 -adapter " + managers[i].displayNumber) : ("-screen-fullscreen 0 -popupwindow"));
            res += " " + "-screen-width " + managers[i].displayResolution.x + " -screen-height " + managers[i].displayResolution.y;
            res += " " + ((displays[0].is3D && !displays[0].dualPipe) ? "-vrmode stereo" : "");

            res += "\n}";
        }

        return res;
    }
    private void IterateAllRelevantChildren(GameObject it, List<PhysicalDisplay> displays, List<PhysicalDisplayManager> managers)
    {
        for (int i = 0; i < it.transform.childCount; i++)
        {
            GameObject child = it.transform.GetChild(i).gameObject;
            if (child.GetComponent<PhysicalDisplay>() != null)
            {
                displays.Add(child.GetComponent<PhysicalDisplay>());
            }
            else if (child.GetComponent<PhysicalDisplayManager>())
            {
                managers.Add(child.GetComponent<PhysicalDisplayManager>());
            }
            IterateAllRelevantChildren(child, displays, managers);
        }
    }

    void OnValidate()
    {
        launchScript = GenerateLaunchScript();
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(UCNetwork))]
public class UCNetworkEditor : Editor {
    private Material material;
    private int selectedIndex = 0;

    void OnEnable() {
        //this is used for rendering, don't remove
        material = new Material(Shader.Find("Hidden/Internal-Colored"));
    }

    private void TextAtPosition(int x, int y, int height, string text) {
        GUIStyle myStyle = new GUIStyle();

        myStyle.fontSize = height;
        myStyle.alignment = TextAnchor.UpperLeft;


        //Color32 color = Color.red;
        //EditorGUI.DrawRect(new Rect(x - 11, y, text.Length * 16 + 11, 32 - 2), color);
        //Rect r = GUILayoutUtility.GetLastRect();
        EditorGUI.SelectableLabel(new Rect(x, y, text.Length * 16, height), text, myStyle);
    }

    private void IterateAllRelevantChildren(GameObject it, List<PhysicalDisplay> displays, List<PhysicalDisplayManager> managers) {
        for (int i = 0; i < it.transform.childCount; i++) {
            GameObject child = it.transform.GetChild(i).gameObject;
            if (child.GetComponent<PhysicalDisplay>() != null) {
                displays.Add(child.GetComponent<PhysicalDisplay>());
            } else if (child.GetComponent<PhysicalDisplayManager>()) {
                managers.Add(child.GetComponent<PhysicalDisplayManager>());
            }
            IterateAllRelevantChildren(child, displays, managers);
        }
    }

    private List<PhysicalDisplay> GetAllDisplays() {
        List<PhysicalDisplay> disps = new List<PhysicalDisplay>();
        List<PhysicalDisplayManager> managers = new List<PhysicalDisplayManager>();
        IterateAllRelevantChildren((target as UCNetwork).gameObject, disps, managers);
        return disps;
    }
    private List<PhysicalDisplayManager> GetAllManagers() {
        List<PhysicalDisplay> disps = new List<PhysicalDisplay>();
        List<PhysicalDisplayManager> managers = new List<PhysicalDisplayManager>();
        IterateAllRelevantChildren((target as UCNetwork).gameObject, disps, managers);
        return managers;
    }

    public override void OnInspectorGUI() {
        UCNetwork cave = target as UCNetwork;

        cave.head = (HeadConfiguration)EditorGUILayout.ObjectField("Head", cave.head, typeof(HeadConfiguration), true);

        if (GUILayout.Button("Save Launch Script")) {
            string launchScript = cave.GenerateLaunchScript();
            string savePath = EditorUtility.SaveFilePanel("Save Launch Script", "./", Application.productName + ".ps1", "ps1");
            if (savePath != null && savePath.Length != 0) {
                System.IO.File.WriteAllText(savePath, launchScript);
                Debug.Log("Saved launch script to " + savePath);
            } else {
                Debug.Log("Didn't save file (no path given)");
            }
        }
        List<PhysicalDisplay> displays = GetAllDisplays();
        List<PhysicalDisplayManager> managers = GetAllManagers();
        Dictionary<string, List<object>> havingName = new Dictionary<string, List<object>>();
        List<string> machines = new List<string>();
        for (int i = 0; i < managers.Count; i++) {
            if (!havingName.ContainsKey(managers[i].machineName)) {
                havingName[managers[i].machineName] = new List<object> { managers[i] };
                machines.Add(managers[i].machineName);
            } else {
                havingName[managers[i].machineName].Add(managers[i].gameObject);
            }
        }
        for (int i = 0; i < displays.Count; i++) {
            if (displays[i].manager == null) {
                if (displays[i].machineName == null || !havingName.ContainsKey(displays[i].machineName)) {
                    havingName[displays[i].machineName] = new List<object> { displays[i] };
                    machines.Add(displays[i].machineName);
                } else {
                    havingName[displays[i].machineName].Add(displays[i].gameObject);
                }
            }
        }

        //List<string> errors = new List<string>();
        //foreach(var kvp in havingName) {
        //    if(kvp.Value.Count > 1) {
        //        errors.Add("These GameObjects have conflicting use of machine name " + kvp.Key + " :");
        //        foreach(var obj in kvp.Value) {
        //            errors.Add("\t" + Util.ObjectFullName(obj));
        //        }
        //    }
        //}

        //if(errors.Count != 0) {
        //    GUIStyle colored = new GUIStyle();
        //    colored.fontSize = 18;
        //    colored.normal.textColor = new Color(0.7f, 0, 0);
        //    EditorGUILayout.LabelField("WARNING: Invalid CAVE Configuration", colored);
        //    foreach (var er in errors) {
        //        EditorGUILayout.LabelField(er);
        //    }
        //    return;
        //}

        //if (selectedIndex >= machines.Count) selectedIndex = 0;
        //selectedIndex = EditorGUILayout.Popup("Selected Machine", selectedIndex, machines.ToArray(), EditorStyles.popup);

        /*
        List<object> selectedObjs = havingName[machines[selectedIndex]];
        Dictionary<int, List<KeyValuePair<string, RectInt>>> usingDisplay = new Dictionary<int, List<KeyValuePair<string, RectInt>>>();
        for (int i = 0; i < selectedObjs.Count; i++) {
            int displayIndex = -1;
            List<RectInt> viewports = new List<RectInt>();
            if (selectedObjs[i] is PhysicalDisplayManager) {
                displayIndex = (selectedObjs[i] as PhysicalDisplayManager).displayNumber;
                foreach (PhysicalDisplay disp in (selectedObjs[i] as PhysicalDisplayManager).displays) {
                    viewports.Add(new KeyValuePair<string, RectInt>());
                }
            } else if (selectedObjs[i] is PhysicalDisplay) {
                if ((selectedObjs[i] as PhysicalDisplay).exclusiveFullscreen) {
                    displayIndex = (selectedObjs[i] as PhysicalDisplay).display;
                }
            }
            if (usingDisplay.ContainsKey(displayIndex)) {
                usingDisplay[displayIndex].Add(sele)
            } else {
                usingDisplay[displayIndex] = new List<RectInt> { }
            }
        }
        for (int i = 0; i < selectedObjs.Count; i++) {
            selectedObjs[i].GetComponent < PhysicalDisplayManager >
            int dispIndex = managers[i].displayNumber;

            EditorGUILayout.LabelField("Display " + dispIndex + " :");
            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            Rect drawSpace = GUILayoutUtility.GetRect(10, 10000, 200, 200);
            if (Event.current.type == EventType.Repaint) {
                GUI.BeginClip(drawSpace);
                GL.PushMatrix();

                //GL.Viewport(drawSpace);
                //GL.Clear(true, false, Color.black);
                //material.SetPass(0);

                //GL.Begin(GL.QUADS);
                //GL.Color(Color.white);
                //    GL.Vertex3(0,               0,                  0);
                //    GL.Vertex3(drawSpace.width, 0,                  0);
                //    GL.Vertex3(drawSpace.width, drawSpace.height,   0);
                //    GL.Vertex3(0,               drawSpace.height,   0);
                //GL.End();

                TextAtPosition(0, 0, 12, "long ass string");

                GL.PopMatrix();
                GUI.EndClip();
            }
            GUILayout.EndHorizontal();
        }*/

        if (GUI.changed) {
            EditorUtility.SetDirty(cave);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
    }
}
#endif