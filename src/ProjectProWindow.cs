using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ProjectPro {
    public class ProjectProWindow : EditorWindow {
        //ProjectBrowser反射数据
        private MethodInfo _onGui;
        private MethodInfo _showFolderContents;
        private MethodInfo _onLostFocus;
        private MethodInfo _onInspectorUpdate;
        //[SerializeField]
        private EditorWindow _projectWindow;
        private FieldInfo _lastFramedIDInfo;
        private IList _projectBrowsers;
        private FieldInfo _lastFoldersInfo;
        private FieldInfo _parentInfo;

        private List<string> _pathList = new List<string>(8);
        private int _pathIndex = 0;
        [SerializeField]
        private int _count = -1;


        public ProjectProWindow() {
            Log("ProjectProWindow");
        }

        [MenuItem("Window/ProjectPro")]
        public static void Show() {
            var window = EditorWindow.GetWindow<ProjectProWindow>();
            ((EditorWindow)window).Show();
            var titleContent = new GUIContent(EditorGUIUtility.FindTexture("folder Icon"));
            titleContent.text = "ProjectPro";
            window.titleContent = titleContent;
            window.minSize = new Vector2(240, 120);
        }

        [MenuItem("Assets/ProjectPro/ShowGUID")]
        public static void ShowGuid() {
            var path = AssetDatabase.GetAssetPath(Selection.activeInstanceID);
            var guid = AssetDatabase.AssetPathToGUID(path);
            Debug.Log(Path.GetFileName(path) + " guid:" + guid);
        }

        [MenuItem("Assets/ProjectPro/CopyAssetPath")]
        public static void CopyAssetPath() {
            EditorGUIUtility.systemCopyBuffer = "";
            foreach (var i in Selection.instanceIDs) {
                EditorGUIUtility.systemCopyBuffer += AssetDatabase.GetAssetPath(i) + "\n";
            }
        }

        [MenuItem("Assets/ProjectPro/CopyAssetPath", true)]
        public static bool CopyAssetPathVaild() {
            return Selection.instanceIDs.Length >= 1;
        }


        void OnEnable() {
            object thisParent = null;
            var editorWindowInfo = typeof(EditorWindow);
            _parentInfo = editorWindowInfo.GetField("m_Parent", BindingFlags.Instance | BindingFlags.NonPublic);

            _projectWindow = EditorGUIUtility.Load("Assets/ProjectPro/projectPro.asset") as EditorWindow;
            _projectWindow = _projectWindow ?? ScriptableObject.CreateInstance("ProjectBrowser") as EditorWindow;
            if (AssetDatabase.Contains(_projectWindow) == false) {
                AssetDatabase.CreateAsset(_projectWindow, "Assets/ProjectPro/projectPro.asset");
            }



            _projectWindow.titleContent = new GUIContent("_ test windows");
            var projectWindowType = _projectWindow.GetType();
            Log("_projectWindow.instance id=" + _projectWindow.GetInstanceID());
            _onGui = projectWindowType.GetMethod("OnGUI", BindingFlags.NonPublic | BindingFlags.Instance);
            _showFolderContents = projectWindowType.GetMethod("ShowFolderContents", BindingFlags.NonPublic | BindingFlags.Instance);
            _onLostFocus = projectWindowType.GetMethod("OnLostFocus", BindingFlags.NonPublic | BindingFlags.Instance);
            _onInspectorUpdate = projectWindowType.GetMethod("OnInspectorUpdate", BindingFlags.NonPublic | BindingFlags.Instance);

            ////_selectedPathSplitted = projectWindowType.GetField("m_SelectedPathSplitted", bindingAttr: BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic);
            _lastFoldersInfo = projectWindowType.GetField("m_LastFolders", BindingFlags.Instance | BindingFlags.NonPublic);
            var browsersInfo = projectWindowType.GetField("s_ProjectBrowsers", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);
            _projectBrowsers = browsersInfo.GetValue(null) as IList;

            _lastFramedIDInfo = projectWindowType.GetField("m_LastFramedID", BindingFlags.NonPublic | BindingFlags.Instance);
            _lastFramedIDInfo.SetValue(_projectWindow, -1);

            _projectBrowsers.Remove(_projectWindow);

            thisParent = _parentInfo.GetValue(this);
            if (thisParent == null)
                Log("thisParent == null");
            else {
                Log(thisParent.GetHashCode());
                _parentInfo.SetValue(_projectWindow, thisParent);
            }

            var onEnable = projectWindowType.GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic);
            onEnable.Invoke(_projectWindow, null);
        }

        private void OnDisable() {
            Log("OnDisable");
        }

        void OnInspectorUpdate() {
            if ((int)_lastFramedIDInfo.GetValue(_projectWindow) != -1) {
                Focus();
            }
            //_count++;
            _onInspectorUpdate.Invoke(_projectWindow, null);
        }

        void OnLostFocus() {
            _lastFramedIDInfo.SetValue(_projectWindow, -1);
            _onLostFocus.Invoke(_projectWindow, null);
        }

        public void OnGUI() {
            var curEvent = Event.current;
            if (curEvent.shift && curEvent.button == 1 && curEvent.type == EventType.mouseDown) {
                EditorUtility.DisplayPopupMenu(new Rect(curEvent.mousePosition.x, curEvent.mousePosition.y, 0f, 0f), "Assets/ProjectPro", null);
                curEvent.Use();
            }

            var pos = _projectWindow.position;
            if (Mathf.Abs(pos.width - position.width) > 1 || Mathf.Abs(pos.height - position.height) > 1) {
                var p = position;
                p.height -= 20;
                _parentInfo.SetValue(_projectWindow, null);
                _projectWindow.position = p;
            }

            if (_parentInfo.GetValue(_projectWindow) == null) {
                var thisParent = _parentInfo.GetValue(this);
                if (thisParent == null) return;
                _parentInfo.SetValue(_projectWindow, thisParent);
            }
            var cmdName = Event.current.commandName;
            if (cmdName == "SoftDelete") {
                try {
                    _onGui.Invoke(_projectWindow, null);
                } catch (Exception e) {
                    Focus(); //避免用delete键删除操作时报错
                }
            } else {
                _onGui.Invoke(_projectWindow, null);
            }

            DrawBottomBar();

            _parentInfo.SetValue(_projectWindow, null);//避免Create操作报错

            var path = (_lastFoldersInfo.GetValue(_projectWindow) as string[])[0];
            _pathIndex = Mathf.Max(_pathIndex, 0);
            var hasChange = path != (_pathList.Count > _pathIndex ? _pathList[_pathIndex] : "");
            if (hasChange) {
                if (_pathIndex > 0) {
                    _pathList.RemoveRange(_pathIndex, _pathList.Count - _pathIndex - 1);
                }
                _pathList.Add(path);
                _pathIndex = _pathList.Count - 1;
                Repaint();
            }
            for (int i = _pathList.Count - 1; i >= 0; i--) {
                if (EditorGUIUtility.Load(_pathList[i]) == null) {
                    _pathIndex = _pathIndex >= i ? _pathIndex - 1 : _pathIndex;
                    _pathList.RemoveAt(i);
                }
            }
        }


        private void DrawBottomBar() {
            var rect = new Rect(0, position.height - 18, position.width, 64);

            GUILayout.BeginArea(rect);
            GUILayout.BeginHorizontal(EditorStyles.toolbar, new GUILayoutOption[0]);
            var preStyle = new GUIStyle();
            preStyle.normal.background = StyleMgr.PreUp;
            preStyle.active.background = StyleMgr.PreDown;
            if (GUILayout.Button("", preStyle, GUILayout.Width(16), GUILayout.Height(16))) {
                if (_pathIndex > 0) {
                    _pathIndex -= 1;
                    var select = _pathList[_pathIndex];
                    ShowFolderContents(select);
                }


            }
            GUILayout.Space(10);
            var nextStyle = new GUIStyle();
            nextStyle.normal.background = StyleMgr.NextUp;
            nextStyle.active.background = StyleMgr.NextDown;
            if (GUILayout.Button("", nextStyle, GUILayout.Width(16), GUILayout.Height(16))) {
                if (_pathIndex + 1 < _pathList.Count) {
                    _pathIndex += 1;
                    var select = _pathList[_pathIndex];
                    ShowFolderContents(select);
                }
            }
            GUILayout.Space(10);
            var listStyle = new GUIStyle();
            listStyle.normal.background = StyleMgr.ListUp;
            listStyle.active.background = StyleMgr.ListDown;
            if (GUILayout.Button("", listStyle, GUILayout.Width(16), GUILayout.Height(16))) {
                var contents = new GUIContent[_pathList.Count];
                for (int i = 0; i < _pathList.Count; i++) {
                    contents[i] = new GUIContent(Path.GetFileNameWithoutExtension(_pathList[i]));
                }
                rect.x = 0;
                rect.y = -50;
                EditorUtility.DisplayCustomMenu(rect, contents, _pathIndex, OnMenu, null);
            }
            GUILayout.Space(10);
            var upStyle = new GUIStyle();
            upStyle.normal.background = StyleMgr.UpUp;
            upStyle.active.background = StyleMgr.UpDown;
            if (GUILayout.Button("", upStyle, GUILayout.Width(16), GUILayout.Height(16))) {
                var curPath = _pathList[_pathIndex];
                if (curPath != "Assets") {
                    var select = curPath.Replace("/" + Path.GetFileNameWithoutExtension(curPath), "");
                    _pathIndex = _pathList.IndexOf(select);
                    Log(_pathIndex);
                    ShowFolderContents(select);
                }
            }
            GUILayout.Space(position.width - 78);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void ShowFolderContents(string folderPath) {
            Log(folderPath);
            var folderObj = EditorGUIUtility.Load(folderPath);
            if (folderObj != null) {
                var instanceId = folderObj.GetInstanceID();
                _showFolderContents.Invoke(_projectWindow, new object[] { instanceId, false });
            } else {
                _pathList.Remove(folderPath);
                ShowFolderContents("Assets");
            }

        }

        private void OnMenu(object userData, string[] options, int selected) {
            ShowFolderContents(_pathList[selected]);
            _pathIndex = selected;
        }

        private void Log(object log) {
#if DEBUG
            Debug.Log(log);
#endif
        }
    }
}
