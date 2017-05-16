/*
 * @file PlayerPathInspector.cs
 * @brief プレイヤーの移動パスの拡張エディター
 * @date 2017/04/14
 * @author 仁科香苗
 */
using System;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

//操作モード
public enum ManipulationModes
{
    Free,
    SelectAndTransform
}

//新しいハンドルを追加する場所
public enum NewWaypointMode
{
    SceneCamera,            //シーンンビューにおけるカメラの位置(ゲームビューを映しているカメラでない)
    LastWaypoint,           //最後のハンドルの座標
    WaypointIndex,        //指定したハンドルと同じ座標
    WorldCenter              //ワールド空間の中央
}

//プレイヤーパスのエディタ拡張
[CustomEditor(typeof(PlayerPath))]
public class PlayerPathInspector : Editor
{
    private PlayerPath t;
    private ReorderableList pointReorderableList;

    //Editor variables
    private bool visualFoldout;
    private bool manipulationFoldout;
    private bool showRawValues;
    public bool showPointList;
    private ManipulationModes translateMode;
    private ManipulationModes handlePositionMode;
    private NewWaypointMode waypointMode;
    private int waypointIndex = 1;

    //GUIContents
    private GUIContent addPointContent = new GUIContent("ハンドルの追加", "シーンビューのカメラ座標");
    private GUIContent deletePointContent = new GUIContent("×", "ハンドル削除");
    private GUIContent gotoPointContent = new GUIContent("カメラ→ハンドル", "ハンドルの座標へシーンビューのカメラを移動");
    private GUIContent relocateContent = new GUIContent("ハンドル→カメラ", "Sceneビューのカメラの位置にハンドルを移動");
    private GUIContent alwaysShowContent = new GUIContent("パス描画", "パスを常に描画するかどうか");
    private GUIContent chainedContent = new GUIContent("対照", "パス曲線の両端のハンドルを対照とするかどうか");
    private GUIContent unchainedContent = new GUIContent("非対照", "パス曲線の両端のハンドルを対照とするかどうか");

    //Serialized Properties
    private SerializedObject serializedObjectTarget;
    private SerializedProperty playerPathProperty;
    private SerializedProperty perSecond;
    private SerializedProperty firstPointsNumber;
    private SerializedProperty visualPathProperty;
    private SerializedProperty visualInactivePathProperty;
    private SerializedProperty visualHandleProperty;
    private SerializedProperty visualSelectHandleProperty;
    private SerializedProperty alwaysShowProperty;

    private int selectedIndex = -1;
    private bool hasScrollBar = false;

    void OnEnable()
    {
        EditorApplication.update += Update;

        t = (PlayerPath)target;
        if (t == null) return;

        SetupEditorVariables();
        GetVariableProperties();
        SetupReorderableList();
    }

    void OnDisable()
    {
        EditorApplication.update -= Update;
    }

    void Update()
    {
        if (t == null) return;

    }

    public override void OnInspectorGUI()
    {
        serializedObjectTarget.Update();
        DrawBasicSettings();
        GUILayout.Space(5);
        GUILayout.Box("", GUILayout.Width(Screen.width - 20), GUILayout.Height(3));
        DrawVisualDropdown();
        GUILayout.Box("", GUILayout.Width(Screen.width - 20), GUILayout.Height(3));
        DrawManipulationDropdown();
        GUILayout.Box("", GUILayout.Width(Screen.width - 20), GUILayout.Height(3));
        GUILayout.Space(10);
        DrawWaypointList();
        GUILayout.Space(10);
        DrawRawValues();
        serializedObjectTarget.ApplyModifiedProperties();
    }

    void OnSceneGUI()
    {
        if (t.points.Count >= 2)
        {
            for (int i = 0; i < t.points.Count; i++)
            {
                DrawHandles(i);
                Handles.color = Color.white;
            }
        }
    }

    void SelectIndex(int index)
    {
        selectedIndex = index;
        pointReorderableList.index = index;
        Repaint();
    }

    void SetupEditorVariables()
    {
        translateMode = (ManipulationModes)PlayerPrefs.GetInt("translateMode", 1);
        handlePositionMode = (ManipulationModes)PlayerPrefs.GetInt("handlePositionMode", 0);
        waypointMode = (NewWaypointMode)PlayerPrefs.GetInt("waypointMode", 0);
    }

    void GetVariableProperties()
    {
        serializedObjectTarget = new SerializedObject(t);
        playerPathProperty = serializedObjectTarget.FindProperty("player");
        visualPathProperty = serializedObjectTarget.FindProperty("visual.pathColor");
        visualInactivePathProperty = serializedObjectTarget.FindProperty("visual.inactivePathColor");
        visualHandleProperty = serializedObjectTarget.FindProperty("visual.handleColor");
        visualSelectHandleProperty = serializedObjectTarget.FindProperty("visual.selectHandleColor");
        alwaysShowProperty = serializedObjectTarget.FindProperty("alwaysShow");
        firstPointsNumber = serializedObjectTarget.FindProperty("firstPointsNumber");
        perSecond = serializedObjectTarget.FindProperty("perSecond");
    }

    void SetupReorderableList()
    {
        pointReorderableList = new ReorderableList(serializedObject, serializedObject.FindProperty("points"), true, true, false, false);

        pointReorderableList.elementHeight *= 2;

        pointReorderableList.drawElementCallback = (rect, index, active, focused) =>
        {
            float startRectY = rect.y;
            if (index > t.points.Count - 1) return;
            rect.height -= 2;
            float fullWidth = rect.width - 16 * (hasScrollBar ? 1 : 0);
            rect.width = 40;
            fullWidth -= 40;
            rect.height /= 2;
            GUI.Label(rect, "#" + (index));
            rect.y = startRectY;
            rect.x += rect.width - 20;
            rect.width += 20;
            GUI.Label(rect, "曲線のパス");
            rect.x += rect.width;
            if (GUI.Button(rect, t.points[index].chained ? chainedContent : unchainedContent))
            {
                Undo.RecordObject(t, "Changed chain type");
                t.points[index].chained = !t.points[index].chained;
            }
            rect.x += rect.width + 2;
            rect.y = startRectY;
            rect.width = (fullWidth - 22) / 3 - 35;
            rect.height = (rect.height * 2) / 2 - 1;
            GUI.Label(rect, "カメラ・ハンドル移動");
            rect.x += rect.width;
            if (GUI.Button(rect, gotoPointContent))
            {
                pointReorderableList.index = index;
                selectedIndex = index;
                SceneView.lastActiveSceneView.pivot = t.points[pointReorderableList.index].position;
                SceneView.lastActiveSceneView.size = 3;
                SceneView.lastActiveSceneView.Repaint();
            }
            rect.x += rect.width + 2;
            if (GUI.Button(rect, relocateContent))
            {
                Undo.RecordObject(t, "Relocated waypoint");
                pointReorderableList.index = index;
                selectedIndex = index;
                t.points[pointReorderableList.index].position = SceneView.lastActiveSceneView.camera.transform.position;
                SceneView.lastActiveSceneView.Repaint();
            }
            rect.x += rect.width;
            rect.width = 20f;
            if (GUI.Button(rect, deletePointContent))
            {
                Undo.RecordObject(t, "Deleted a waypoint");
                t.points.Remove(t.points[index]);
                SceneView.RepaintAll();
            }
        };

        pointReorderableList.drawHeaderCallback = rect =>
        {
            float fullWidth = rect.width;
            rect.width = 70;
            GUI.Label(rect, "ハンドル情報: " + t.points.Count);
            rect.width = (fullWidth - 78) / 3;
        };

        pointReorderableList.onSelectCallback = l =>
        {
            selectedIndex = l.index;
            SceneView.RepaintAll();
        };
    }

    void DrawBasicSettings()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("プレイヤー: ", GUILayout.Width(Screen.width / 6f));
        playerPathProperty.objectReferenceValue = (Transform)EditorGUILayout.ObjectField(playerPathProperty.objectReferenceValue, typeof(Transform), true);
        GUILayout.EndHorizontal();

        firstPointsNumber.intValue = EditorGUILayout.IntField("初期ハンドル位置", firstPointsNumber.intValue);
        perSecond.floatValue = EditorGUILayout.Slider("速度", perSecond.floatValue, 0, 30f);
    }

    void DrawVisualDropdown()
    {
        EditorGUI.BeginChangeCheck();
        GUILayout.BeginHorizontal();
        visualFoldout = EditorGUILayout.Foldout(visualFoldout, "パスの色", true);
        alwaysShowProperty.boolValue = GUILayout.Toggle(alwaysShowProperty.boolValue, alwaysShowContent);
        GUILayout.EndHorizontal();
        if (visualFoldout)
        {
            GUILayout.BeginVertical("Box");
            visualPathProperty.colorValue = EditorGUILayout.ColorField("パス", visualPathProperty.colorValue);
            visualInactivePathProperty.colorValue = EditorGUILayout.ColorField("非アクティブ時のパス", visualInactivePathProperty.colorValue);
            visualSelectHandleProperty.colorValue = EditorGUILayout.ColorField("選択ハンドル", visualSelectHandleProperty.colorValue);
            visualHandleProperty.colorValue = EditorGUILayout.ColorField("非選択ハンドル", visualHandleProperty.colorValue);
            if (GUILayout.Button("初期色設定"))
            {
                Undo.RecordObject(t, "Reset to default color values");
                t.visual = new PathVisual();
            }
            GUILayout.EndVertical();
        }
        if (EditorGUI.EndChangeCheck())
        {
            SceneView.RepaintAll();
        }
    }

    void DrawManipulationDropdown()
    {
        manipulationFoldout = EditorGUILayout.Foldout(manipulationFoldout, "変換操作モード", true);
        EditorGUI.BeginChangeCheck();
        if (manipulationFoldout)
        {
            translateMode = (ManipulationModes)EditorGUILayout.EnumPopup("座標", translateMode);
            handlePositionMode = (ManipulationModes)EditorGUILayout.EnumPopup("ハンドル", handlePositionMode);
        }
        if (EditorGUI.EndChangeCheck())
        {
            PlayerPrefs.SetInt("translateMode", (int)translateMode);
            PlayerPrefs.SetInt("handlePositionMode", (int)handlePositionMode);
            SceneView.RepaintAll();
        }
    }

    void DrawWaypointList()
    {
        //if (GUILayout.Button(showPointList ? "閉じる" : "ハンドルリスト"))
        //    showPointList = !showPointList;
        showPointList = EditorGUILayout.Foldout(showPointList, "ハンドルリスト", true);

        if (showPointList)
        {
            serializedObject.Update();
            pointReorderableList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();

            Rect r = GUILayoutUtility.GetRect(Screen.width - 16, 18);
            r.y -= 10;
            GUILayout.Space(-30);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(addPointContent))
            {
                //Undo.RecordObject(t, "Added camera path point");
                switch (waypointMode)
                {
                    case NewWaypointMode.SceneCamera:
                        t.points.Add(new HandlePoint(SceneView.lastActiveSceneView.camera.transform.position, SceneView.lastActiveSceneView.camera.transform.rotation));
                        break;
                    case NewWaypointMode.LastWaypoint:
                        if (t.points.Count > 0)
                            t.points.Add(new HandlePoint(t.points[t.points.Count - 1].position, t.points[t.points.Count - 1].rotation) { handleNext = t.points[t.points.Count - 1].handleNext, handlePrev = t.points[t.points.Count - 1].handlePrev });
                        else
                        {
                            t.points.Add(new HandlePoint(Vector3.zero, Quaternion.identity));
                            Debug.LogWarning("No previous waypoint found to place this waypoint, defaulting position to world center");
                        }
                        break;
                    case NewWaypointMode.WaypointIndex:
                        if (t.points.Count > waypointIndex - 1 && waypointIndex > 0)
                            t.points.Add(new HandlePoint(t.points[waypointIndex - 1].position, t.points[waypointIndex - 1].rotation) { handleNext = t.points[waypointIndex - 1].handleNext, handlePrev = t.points[waypointIndex - 1].handlePrev });
                        else
                        {
                            t.points.Add(new HandlePoint(Vector3.zero, Quaternion.identity));
                            Debug.LogWarning("Waypoint index " + waypointIndex + " does not exist, defaulting position to world center");
                        }
                        break;
                    case NewWaypointMode.WorldCenter:
                        t.points.Add(new HandlePoint(Vector3.zero, Quaternion.identity));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                selectedIndex = t.points.Count - 1;
                SceneView.RepaintAll();
            }
            //GUILayout.Label("at", GUILayout.Width(20));
            EditorGUI.BeginChangeCheck();
            waypointMode = (NewWaypointMode)EditorGUILayout.EnumPopup(waypointMode, waypointMode == NewWaypointMode.WaypointIndex ? GUILayout.Width(Screen.width / 4) : GUILayout.Width(Screen.width / 2));
            if (waypointMode == NewWaypointMode.WaypointIndex)
            {
                waypointIndex = EditorGUILayout.IntField(waypointIndex, GUILayout.Width(Screen.width / 4));
            }
            if (EditorGUI.EndChangeCheck())
            {
                PlayerPrefs.SetInt("waypointMode", (int)waypointMode);
            }
            GUILayout.EndHorizontal();
        }
    }

    void DrawHandles(int i)
    {
        DrawHandleLines(i);
        Handles.color = t.visual.handleColor;
        DrawNextHandle(i);
        DrawPrevHandle(i);
        DrawWaypointHandles(i);
        DrawSelectionHandles(i);
    }


    void DrawHandleLines(int i)
    {
        Handles.color = t.visual.handleColor;
        if (i < t.points.Count)
            Handles.DrawLine(t.points[i].position, t.points[i].position + t.points[i].handleNext);
        if (i >= 0)
            Handles.DrawLine(t.points[i].position, t.points[i].position + t.points[i].handlePrev);
        Handles.color = Color.white;
    }

    void DrawNextHandle(int i)
    {
        if (i < t.points.Count)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 posNext = Vector3.zero;
            float size = HandleUtility.GetHandleSize(t.points[i].position + t.points[i].handleNext) * 0.1f;
            if (handlePositionMode == ManipulationModes.Free)
            {
                posNext = Handles.FreeMoveHandle(t.points[i].position + t.points[i].handleNext, Quaternion.identity, size, Vector3.zero, Handles.SphereHandleCap);
            }
            else
            {
                if (selectedIndex == i)
                {
                    Handles.color = visualSelectHandleProperty.colorValue;
                    size = HandleUtility.GetHandleSize(t.points[i].position + t.points[i].handleNext) * 0.3f;
                    Handles.SphereHandleCap(0, t.points[i].position + t.points[i].handleNext, Quaternion.identity, size, EventType.Repaint);
                    posNext = Handles.PositionHandle(t.points[i].position + t.points[i].handleNext, Quaternion.identity);
                }
                else if (Event.current.button != 1)
                {
                    if (Handles.Button(t.points[i].position + t.points[i].handleNext, Quaternion.identity, size, size, Handles.CubeHandleCap))
                    {
                        SelectIndex(i);
                    }
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Changed Handle Position");
                t.points[i].handleNext = posNext - t.points[i].position;
                if (t.points[i].chained)
                    t.points[i].handlePrev = t.points[i].handleNext * -1;
            }
        }

    }

    void DrawPrevHandle(int i)
    {
        if (i >= 0)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 posPrev = Vector3.zero;
            float size = HandleUtility.GetHandleSize(t.points[i].position + t.points[i].handlePrev) * 0.1f;
            if (handlePositionMode == ManipulationModes.Free)
            {
                posPrev = Handles.FreeMoveHandle(t.points[i].position + t.points[i].handlePrev, Quaternion.identity, 0.1f * HandleUtility.GetHandleSize(t.points[i].position + t.points[i].handlePrev), Vector3.zero, Handles.SphereHandleCap);
            }
            else
            {
                if (selectedIndex == i)
                {
                    Handles.color = visualSelectHandleProperty.colorValue;
                    Handles.SphereHandleCap(0, t.points[i].position + t.points[i].handlePrev, Quaternion.identity, 0.3f * HandleUtility.GetHandleSize(t.points[i].position + t.points[i].handleNext), EventType.Repaint);
                    posPrev = Handles.PositionHandle(t.points[i].position + t.points[i].handlePrev, Quaternion.identity);
                }
                else if (Event.current.button != 1)
                {
                    if (Handles.Button(t.points[i].position + t.points[i].handlePrev, Quaternion.identity, size, size, Handles.CubeHandleCap))
                    {
                        SelectIndex(i);
                    }
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Changed Handle Position");
                t.points[i].handlePrev = posPrev - t.points[i].position;
                if (t.points[i].chained)
                    t.points[i].handleNext = t.points[i].handlePrev * -1;
            }
        }
    }

    void DrawWaypointHandles(int i)
    {
        if (Tools.current == Tool.Move)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 pos = Vector3.zero;
            if (translateMode == ManipulationModes.SelectAndTransform)
            {
                if (i == selectedIndex)
                {
                    pos = Handles.PositionHandle(t.points[i].position, (Tools.pivotRotation == PivotRotation.Local) ? t.points[i].rotation : Quaternion.identity);
                    Handles.color = visualSelectHandleProperty.colorValue;
                    float size = HandleUtility.GetHandleSize(t.points[i].position) * 0.5f;
                    Handles.CubeHandleCap(0, pos, (Tools.pivotRotation == PivotRotation.Local) ? t.points[i].rotation : Quaternion.identity,size, EventType.Repaint);
                }
            }
            else
            {
                pos = Handles.FreeMoveHandle(t.points[i].position, (Tools.pivotRotation == PivotRotation.Local) ? t.points[i].rotation : Quaternion.identity, HandleUtility.GetHandleSize(t.points[i].position) * 0.2f, Vector3.zero, Handles.RectangleHandleCap);
            }
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Moved Waypoint");
                t.points[i].position = pos;
            }
        }
    }
    void DrawSelectionHandles(int i)
    {
        if (Event.current.button != 1 && selectedIndex != i)
        {
            if (translateMode == ManipulationModes.SelectAndTransform && Tools.current == Tool.Move)
            {
                float size = HandleUtility.GetHandleSize(t.points[i].position) * 0.2f;
                if (Handles.Button(t.points[i].position, Quaternion.identity, size, size, Handles.CubeHandleCap))
                {
                    SelectIndex(i);
                }
            }
        }
    }

    void DrawRawValues()
    {
        //if (GUILayout.Button(showRawValues ? "閉じる" : "ハンドル情報詳細詳細"))
        //showRawValues = !showRawValues;
        showRawValues = EditorGUILayout.Foldout(showRawValues, "ハンドル", true);

        int index = 0;
        if (showRawValues)
        {
            foreach (var i in t.points)
            {
                EditorGUI.BeginChangeCheck();
                //i.showPoints = EditorGUILayout.Foldout(i.showPoints, "#" + index++, true);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("#"+index++))
                    i.showPoints = !i.showPoints;
                GUILayout.Space(410);
                GUILayout.EndHorizontal();
                if (i.showPoints)
                {
                    GUILayout.BeginVertical("Box");
                    //EditorGUILayout.LabelField("#" + index++);
                    Vector3 pos = EditorGUILayout.Vector3Field("座標", i.position);
                    Vector3 posp = EditorGUILayout.Vector3Field("前ハンドルとの曲線パス", i.handlePrev);
                    Vector3 posn = EditorGUILayout.Vector3Field("次ハンドルとの曲線パス", i.handleNext);
                    GUILayout.EndVertical();

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(t, "Changed waypoint transform");
                        i.position = pos;
                        i.handlePrev = posp;
                        i.handleNext = posn;
                        SceneView.RepaintAll();
                    }
                }
            }
        }
    }
}