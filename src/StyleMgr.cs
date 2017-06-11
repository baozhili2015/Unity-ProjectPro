using UnityEditor;
using UnityEngine;

namespace ProjectPro {
    static class StyleMgr {
        public static Texture2D ListUp;
        public static Texture2D ListDown;
        public static Texture2D PreUp;
        public static Texture2D PreDown;
        public static Texture2D NextUp;
        public static Texture2D NextDown;
        public static Texture2D UpUp;
        public static Texture2D UpDown;

        static StyleMgr() {
            ListUp = EditorGUIUtility.Load("Assets/ProjectPro/list_up.png") as Texture2D;
            ListDown = EditorGUIUtility.Load("Assets/ProjectPro/list_down.png") as Texture2D;
            PreUp = EditorGUIUtility.Load("Assets/ProjectPro/pre_up.png") as Texture2D;
            PreDown = EditorGUIUtility.Load("Assets/ProjectPro/pre_down.png") as Texture2D;
            NextUp = EditorGUIUtility.Load("Assets/ProjectPro/next_up.png") as Texture2D;
            NextDown = EditorGUIUtility.Load("Assets/ProjectPro/next_down.png") as Texture2D;
            UpUp = EditorGUIUtility.Load("Assets/ProjectPro/up_up.png") as Texture2D;
            UpDown = EditorGUIUtility.Load("Assets/ProjectPro/up_down.png") as Texture2D;
        }
    }
}
