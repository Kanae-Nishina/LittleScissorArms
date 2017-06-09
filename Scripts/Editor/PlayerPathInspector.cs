/*!
 * @file PlayerPathInspector.cs
 * @brief プレイヤーの移動パスの拡張エディター
 * @date 2017/04/14
 * @author 仁科香苗
 * @note 参考:PlayerPath(https://www.assetstore.unity3d.com/jp/#!/content/47769)
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using System;

/*! @brief プレイヤーパスのエディタ拡張*/
[CustomEditor(typeof(PlayerPath))]
[CanEditMultipleObjects]
public class PlayerPathInspector : Editor
{
    private bool waypointsFoldout = true;      /*! ポイント折りたたみフラグ*/
    private bool previewFoldout = true;          /*! プレビュー折りたたみフラグ*/
    private bool eventsFoldout = true;             /*! イベント折りたたみフラグ*/
    private bool utilityFoldout = true;               /*! ユーティリティー折りたたみフラグ*/
    private bool showPathSamples = true;     /*! パスサンプルの可視化*/
    private bool showTangents = true;            /*! 接線の可視化*/
    private Vector2 scrollPos;                             /*! スクロールポジション*/

    [SerializeField]
    private ReorderableList wl;                                 /*! 入れ替え可能なリスト*/
    private int currentSelectedWaypoint = -1;      /*! 現在選択されているポイント*/
    //private GUIStyle boldFoldoutStyle;                  /*! 折りたたみの種類*/
    private GUIStyle actionButtonStyleLeft;        /*! アクションボタンスタイル左側*/
    private GUIStyle actionButtonStyleRight;     /*! アクションボタンスタイル右側*/
    private GUIStyle rightMiniButton;                   /*! ボタンスタイル*/
    private SerializedProperty waypoints;           /*! ポイント*/

    /*! @brief アクティブ時のイベント*/
    void OnEnable()
    {
        showPathSamples = EditorPrefs.GetBool("PlayerPath.ShowPathSamples", false);
        showTangents = EditorPrefs.GetBool("PlayerPath.ShowTangents", true);
        waypoints = serializedObject.FindProperty("waypoints");
        SettingReorderableList(); //ポイントリストの並び替え設定
    }

    /*! @brief インスペクターの表示*/
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        CharacterPreference(); //文字設定
        EditorGUIUtility.labelWidth = 120;
        if (wl == null)
            OnEnable();

        wl.index = currentSelectedWaypoint;

        BasePreference();                               //基本設定
        WaypointPreference();                        //ポイントの設定
        EventPreference();                              //イベント設定
        Preview();                                              //プレビュー
        UtilityPreference();                            //ユーティリティ

        serializedObject.ApplyModifiedProperties();
        ((PlayerPath)serializedObject.targetObject).UpdatePathSamples();
    }

    /*! @brief シーンビュー表示*/
    void OnSceneGUI()
    {
        SerializedObject pm = new SerializedObject(target);
        PlayerPath pmo = (PlayerPath)target;
        Handles.matrix = ((PlayerPath)pm.targetObject).transform.localToWorldMatrix;
        DrawPointOnScene(pm, pmo);   //ポイント描画
        DrawSampledOnScene(pmo);    //サンプリング描画
        pm.ApplyModifiedProperties();
    }

    /*! @brief シーンビューにポイントの描画*/
    void DrawPointOnScene(SerializedObject pm, PlayerPath pmo)
    {
        bool isGlobalMode = Tools.pivotRotation == PivotRotation.Global;
        for (int i = 0; i < waypoints.arraySize; i++)
        {
            SerializedProperty wp = waypoints.GetArrayElementAtIndex(i);
            float size = HandleUtility.GetHandleSize(wp.FindPropertyRelative("position").vector3Value);
            if (Handles.Button(wp.FindPropertyRelative("position").vector3Value,
                    Quaternion.identity, size / 10f, size / 5f, Handles.CubeHandleCap))
            {
                currentSelectedWaypoint = i;
                EditorUtility.SetDirty(target);
            }

            if (currentSelectedWaypoint == i)
            {
                //座標
                wp.FindPropertyRelative("position").vector3Value = PositionHandle(wp.FindPropertyRelative("position").vector3Value, isGlobalMode ? Quaternion.identity : Quaternion.Euler(wp.FindPropertyRelative("rotation").vector3Value), false);

                if (showTangents)
                {
                    //前ハンドル
                    wp.FindPropertyRelative("inTangent").vector3Value = PositionHandle(
                        wp.FindPropertyRelative("inTangent").vector3Value + wp.FindPropertyRelative("position").vector3Value,
                        isGlobalMode ? Quaternion.identity : Quaternion.Euler(wp.FindPropertyRelative("rotation").vector3Value), true) - wp.FindPropertyRelative("position").vector3Value;
                    if (wp.FindPropertyRelative("symmetricTangents").boolValue)
                        wp.FindPropertyRelative("outTangent").vector3Value = -wp.FindPropertyRelative("inTangent").vector3Value;

                    //次ハンドル
                    wp.FindPropertyRelative("outTangent").vector3Value = PositionHandle(
                        wp.FindPropertyRelative("outTangent").vector3Value + wp.FindPropertyRelative("position").vector3Value,
                        isGlobalMode ? Quaternion.identity : Quaternion.Euler(wp.FindPropertyRelative("rotation").vector3Value), true) - wp.FindPropertyRelative("position").vector3Value;

                    if (wp.FindPropertyRelative("symmetricTangents").boolValue)
                        wp.FindPropertyRelative("inTangent").vector3Value = -wp.FindPropertyRelative("outTangent").vector3Value;

                    //ハンドル描画
                    Handles.color = Color.green;
                    Handles.DrawLine(wp.FindPropertyRelative("inTangent").vector3Value + wp.FindPropertyRelative("position").vector3Value, wp.FindPropertyRelative("position").vector3Value);
                    Handles.DrawLine(wp.FindPropertyRelative("outTangent").vector3Value + wp.FindPropertyRelative("position").vector3Value, wp.FindPropertyRelative("position").vector3Value);
                    Handles.color = Color.white;
                }
            }

            if (i > 0)
            {
                Handles.DrawBezier(
                    waypoints.GetArrayElementAtIndex(i - 1).FindPropertyRelative("position").vector3Value,
                    waypoints.GetArrayElementAtIndex(i).FindPropertyRelative("position").vector3Value,
                    waypoints.GetArrayElementAtIndex(i - 1).FindPropertyRelative("position").vector3Value +
                    waypoints.GetArrayElementAtIndex(i - 1).FindPropertyRelative("outTangent").vector3Value,
                    waypoints.GetArrayElementAtIndex(i).FindPropertyRelative("position").vector3Value +
                    waypoints.GetArrayElementAtIndex(i).FindPropertyRelative("inTangent").vector3Value,
                    pmo.pathColor, null, 5f);

            }
        }

        if (pm.FindProperty("loop").boolValue && waypoints.arraySize > 1)
        {
            //ループの場合、最初と最後のポイントをつなぐ
            Handles.DrawBezier(
                waypoints.GetArrayElementAtIndex(waypoints.arraySize - 1).FindPropertyRelative("position").vector3Value,
                waypoints.GetArrayElementAtIndex(0).FindPropertyRelative("position").vector3Value,
                waypoints.GetArrayElementAtIndex(waypoints.arraySize - 1).FindPropertyRelative("position").vector3Value + waypoints.GetArrayElementAtIndex(waypoints.arraySize - 1).FindPropertyRelative("outTangent").vector3Value,
                waypoints.GetArrayElementAtIndex(0).FindPropertyRelative("position").vector3Value + waypoints.GetArrayElementAtIndex(0).FindPropertyRelative("inTangent").vector3Value,
                pmo.pathColor, null, 2f);
        }
    }

    /*! @brief シーンビューにサンプリング描画*/
    void DrawSampledOnScene(PlayerPath pmo)
    {
        Handles.color = Color.yellow;
        for (int i = 1; i < pmo.samplesNum; i++)
        {
            Handles.DrawLine(pmo.positionSamples[i - 1], pmo.positionSamples[i]);
        }
        if (showPathSamples)
        {
            Handles.color = Color.cyan;
            for (int i = 1; i < pmo.samplesNum; i++)
            {
                float handleSize = HandleUtility.GetHandleSize(pmo.positionSamples[i - 1]);
                Handles.DotHandleCap(0, pmo.positionSamples[i - 1], Quaternion.identity, handleSize / 20f, EventType.Repaint);
            }
        }
    }

    /*! @brief ReorderableList設定*/
    private void SettingReorderableList()
    {
        wl = new ReorderableList(serializedObject, waypoints);
            wl.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                if (index > waypoints.arraySize - 1)
                {
                    return;
                }
                rect.y += 2;
                EditorGUIUtility.labelWidth = 20;
                if (GUI.Button(new Rect(rect.x, rect.y, EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight), "\u2023"))
                {
                    PlayerPath pm = (PlayerPath)target;
                    pm.currentPos = ComputePosForWaypoint(index);

                }
                EditorGUI.PropertyField(
                    new Rect(rect.x + EditorGUIUtility.singleLineHeight, rect.y, rect.width - 120 - EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight),
                    waypoints.GetArrayElementAtIndex(index).FindPropertyRelative("position"), new GUIContent("" + (index + 1)));
            };

        wl.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "WayPoint");
        };

        wl.onRemoveCallback = (ReorderableList l) =>
        {
            if (EditorUtility.DisplayDialog("けいこく！!", "ポイントを消しますよ！", "いいよ！", "だめ！"))
            {
                waypoints.DeleteArrayElementAtIndex(l.index);
                if (currentSelectedWaypoint >= waypoints.arraySize)
                    currentSelectedWaypoint = -1;
            }
        };

        wl.onCanRemoveCallback = (ReorderableList l) =>
        {
            return true;
        };

        wl.onSelectCallback = (ReorderableList l) =>
        {
            currentSelectedWaypoint = l.index;
            SceneView.RepaintAll();
        };

        wl.onAddCallback = (ReorderableList l) =>
        {
            if (currentSelectedWaypoint == -1)
            {
                InsertWaypointAt(waypoints.arraySize, true);
            }
            else
            {
                InsertWaypointAt(currentSelectedWaypoint + 1, true);
            }
        };
    }

    /*! @brief 文字設定*/
    private void CharacterPreference()
    {
        //項目の文字太さ
        //boldFoldoutStyle = new GUIStyle(EditorStyles.foldout);
        //boldFoldoutStyle.fontStyle = FontStyle.Bold;
        //右ボタンの設定
        rightMiniButton = new GUIStyle(EditorStyles.miniButton);
        rightMiniButton.fixedWidth = 100;

        //
        actionButtonStyleLeft = new GUIStyle(EditorStyles.miniButtonLeft);
        if (EditorGUIUtility.isProSkin)
            actionButtonStyleLeft.normal.textColor = Color.yellow;
        else
            actionButtonStyleLeft.normal.textColor = Color.black;
        actionButtonStyleLeft.fontStyle = FontStyle.Bold;
        actionButtonStyleLeft.fontSize = 11;
        actionButtonStyleRight = new GUIStyle(EditorStyles.miniButtonRight);
        if (EditorGUIUtility.isProSkin)
            actionButtonStyleRight.normal.textColor = Color.yellow;
        else
            actionButtonStyleRight.normal.textColor = Color.black;
        actionButtonStyleRight.fontStyle = FontStyle.Bold;
        actionButtonStyleRight.fontSize = 11;
    }

    /*! @brief 基本設定*/
    private void BasePreference()
    {
        EditorGUILayout.BeginVertical("Box");
        if (!serializedObject.isEditingMultipleObjects)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("target"), new GUIContent("メインプレイヤー"));

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("loop"), new GUIContent("ループするかどうか"));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Slider(serializedObject.FindProperty("speed"), 0.001f, 0.1f, new GUIContent("移動速度"));

        EditorGUILayout.BeginHorizontal(GUILayout.Width(300f));
        EditorGUILayout.BeginVertical(GUILayout.Width(150f));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("samplesNum"), new GUIContent("サンプリング数", "数が多いほど精度が上がりますがパフォーマンスに影響があります。"), GUILayout.Width(150f));
        if (serializedObject.FindProperty("samplesNum").intValue <= 5)
            serializedObject.FindProperty("samplesNum").intValue = 5;
        if (serializedObject.FindProperty("samplesNum").intValue >= 10000)
            serializedObject.FindProperty("samplesNum").intValue = 10000;
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();

        EditorGUIUtility.labelWidth = 90;
        GUI.enabled = true;
        EditorGUILayout.EndVertical();
        EditorGUILayout.Separator();
    }

    /*! @brief ポイントごとの設定*/
    private void WaypointPreference()
    {
        if (!waypoints.hasMultipleDifferentValues)
        {
            wl.DoLayoutList();
        }
        else
        {
            currentSelectedWaypoint = -1;
            EditorGUILayout.HelpBox("選択したパスのウェイポイントが同じではないため、ウェイポイントデータを編集することはできません。", MessageType.Info);
        }

        if (currentSelectedWaypoint > waypoints.arraySize - 1)
            currentSelectedWaypoint = -1;

        if (currentSelectedWaypoint != -1)
        {
            EditorGUIUtility.labelWidth = 60;
            EditorGUILayout.BeginHorizontal();
            WaypointAddOrDel();
            EditorGUILayout.EndHorizontal();

            if (waypointsFoldout)
            {
                EditorGUILayout.BeginVertical("Box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("position"),
                    new GUIContent("座標"));

                GUI.enabled = currentSelectedWaypoint > 0;
                if (GUILayout.Button(new GUIContent("整列化", "前後のポイントと直線上になるよう設定します。"), rightMiniButton))
                {
                    RemoveWaypointAt(currentSelectedWaypoint);
                    currentSelectedWaypoint = ((currentSelectedWaypoint - 1) % waypoints.arraySize);
                    serializedObject.ApplyModifiedProperties();
                    if (currentSelectedWaypoint == -1)
                    {
                        InsertWaypointAt(waypoints.arraySize, true);
                    }
                    else
                    {
                        InsertWaypointAt(currentSelectedWaypoint + 1, true);
                    }
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("velocity"), new GUIContent("速度", "ハンドル通過前後の速度の種類を設定します。"));
                EditorGUIUtility.labelWidth = 20;
                EditorGUILayout.PropertyField(waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("inVariation"), new GUIContent("In", "イージーイン"));
                EditorGUIUtility.labelWidth = 30;
                EditorGUILayout.PropertyField(waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("outVariation"), new GUIContent("Out", "イージーアウト"));
                EditorGUIUtility.labelWidth = 60;
                GUI.enabled = (currentSelectedWaypoint > 0 && currentSelectedWaypoint < (waypoints.arraySize - 1));
                if (GUILayout.Button(new GUIContent("平均化", "前後のポイントから速度を平均化します。"), rightMiniButton))
                {
                    waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("velocity").floatValue =
                    (waypoints.GetArrayElementAtIndex(currentSelectedWaypoint - 1).FindPropertyRelative("velocity").floatValue +
                    waypoints.GetArrayElementAtIndex(currentSelectedWaypoint + 1).FindPropertyRelative("velocity").floatValue) / 2f;
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();

                EditorGUIUtility.labelWidth = 130;
                EditorGUILayout.PropertyField(waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("symmetricTangents"), new GUIContent("ハンドルの対象化"));
                EditorGUIUtility.labelWidth = 80;
                EditorGUILayout.PropertyField(waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("inTangent"),
                    new GUIContent("前ハンドル"));
                if (waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("symmetricTangents").boolValue)
                    waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("outTangent").vector3Value = -waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("inTangent").vector3Value;
                if (!waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("symmetricTangents").boolValue)
                {
                    EditorGUILayout.PropertyField(waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("outTangent"),
                        new GUIContent("次ハンドル"));
                    if (waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("symmetricTangents").boolValue)
                        waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("inTangent").vector3Value = -waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("outTangent").vector3Value;
                }

                EditorGUIUtility.labelWidth = 60;
                //EditorGUILayout.Separator();
                EditorGUILayout.PropertyField(waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("reached"));
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.Separator();
        }
    }

    /*! @brief ポイントの明確化、追加、削除*/
    private void WaypointAddOrDel()
    {
        waypointsFoldout = EditorGUILayout.Foldout(waypointsFoldout, "Waypoint " + (currentSelectedWaypoint + 1), true/*boldFoldoutStyle*/);
        if (GUILayout.Button(new GUIContent("明確化", "選択中のポイントを見やすくします。"), EditorStyles.miniButtonLeft, GUILayout.Width(60)))
        {
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.pivot = ((PlayerPath)serializedObject.targetObject).transform.TransformPoint(waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("position").vector3Value);
            }
        }
        if (GUILayout.Button(new GUIContent("追加", "選択中のポイントの次のポイントを追加します。"), EditorStyles.miniButtonMid, GUILayout.Width(60)))
        {
            if (currentSelectedWaypoint == -1)
            {
                InsertWaypointAt(waypoints.arraySize, true);
            }
            else
            {
                InsertWaypointAt(currentSelectedWaypoint + 1, true);
            }
        }
        if (GUILayout.Button(new GUIContent("削除", "選択中のポイントを削除します。"), EditorStyles.miniButtonRight, GUILayout.Width(60)))
        {
            RemoveWaypointAt(currentSelectedWaypoint);
            currentSelectedWaypoint = (currentSelectedWaypoint - 1) % (waypoints.arraySize);
        }
    }

    /*! @brief イベント*/
    private void EventPreference()
    {
        eventsFoldout = EditorGUILayout.Foldout(eventsFoldout, "Events", true/*boldFoldoutStyle*/);
        if (eventsFoldout)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("waypointChanged"));
        }
    }

    /*! @brief プレビュー*/
    private void Preview()
    {
        EditorGUILayout.BeginHorizontal();
        previewFoldout = EditorGUILayout.Foldout(previewFoldout, "Preview", true/*boldFoldoutStyle*/);
        EditorGUILayout.EndHorizontal();
        if (previewFoldout)
        {

            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.BeginHorizontal();

            EditorGUIUtility.labelWidth = 70;
            EditorGUILayout.Slider(serializedObject.FindProperty("currentPos"), 0f, 1f, new GUIContent("座標", "パス全体の補間値としての座標"));

            EditorGUIUtility.labelWidth = 120;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("updateTransform"), new GUIContent("移動の更新", "アニメーション中に移動更新するかどうか"));
            EditorGUIUtility.labelWidth = 70;

            for (int i = 0; i < targets.Length; i++)
            {
                if (!((PlayerPath)targets[i]).updateTransform && ((PlayerPath)targets[i]).target != null)
                {
                    PlayerPath pmo = (PlayerPath)targets[i];
                    Vector3 position = Vector3.zero;
                    Quaternion rotation = Quaternion.identity;
                    float velocity = 0f;
                    int waypoint = 0;
                    pmo.GetSampledWayPoint(pmo.currentPos, out position, out velocity, out waypoint);
                    pmo.UpdateTarget(position, velocity);
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
    }

    /*! @brief ユーティリティ設定*/
    private void UtilityPreference()
    {
        utilityFoldout = EditorGUILayout.Foldout(utilityFoldout, "Utility", true/*boldFoldoutStyle*/);
        if (utilityFoldout)
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUIUtility.labelWidth = 120;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("pathColor"), new GUIContent("パスの色"));
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            showPathSamples = EditorGUILayout.Toggle(new GUIContent("サンプリングの可視化"), showPathSamples);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool("PlayerPath.ShowPathSamples", showPathSamples);
                ((SceneView)SceneView.sceneViews[0]).Repaint();
            }
            EditorGUI.BeginChangeCheck();
            showTangents = EditorGUILayout.Toggle(new GUIContent("ハンドルの可視化"), showTangents);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool("PlayerPath.ShowTangents", showTangents);
                ((SceneView)SceneView.sceneViews[0]).Repaint();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.Separator();
    }

    /*! @brief ポイントの挿入と整列*/
    private void InsertWaypointAt(int index, bool align)
    {

        Waypoint item = new Waypoint();

        if (align)
        {
            if (index < waypoints.arraySize)
            {
                float pos1 = CalcPosForWaypointIndex(index - 1);
                float pos2 = CalcPosForWaypointIndex(index);
                float pos = (pos1 + pos2) / 2f;

                item.position = ((PlayerPath)serializedObject.targetObject).computePositionAtPos(pos);
                item.velocity = (((PlayerPath)serializedObject.targetObject).waypoints[index - 1].velocity + ((PlayerPath)serializedObject.targetObject).waypoints[index].velocity) / 2f;

                Quaternion fForward = Quaternion.LookRotation(((PlayerPath)serializedObject.targetObject).computePositionAtPos(pos + 0.001f) - ((PlayerPath)serializedObject.targetObject).computePositionAtPos(pos), Vector3.up);
                item.inTangent = -1 * Vector3.forward;
                item.outTangent = fForward * Vector3.forward;

            }
            else
            {
                //最後のポイント
                if (waypoints.arraySize > 0)
                {
                    item.position = ((PlayerPath)serializedObject.targetObject).waypoints[index - 1].position + 5f * (GetFaceForwardForIndex(index - 1) * Vector3.forward);
                    item.velocity = ((PlayerPath)serializedObject.targetObject).waypoints[index - 1].velocity;
                    item.inTangent = -1 * (Quaternion.Euler(item.rotation) * Vector3.forward);
                    item.outTangent = Quaternion.Euler(item.rotation) * Vector3.forward;
                    item.symmetricTangents = true;
                }
                else
                {
                    //最初のポイント
                    item.position = new Vector3(5f, 5f, 0f);
                    item.rotation = new Vector3(0f, 0f, 0f);
                    item.velocity = 1f;
                    item.inTangent = -1 * (Quaternion.Euler(item.rotation) * Vector3.forward);
                    item.outTangent = Quaternion.Euler(item.rotation) * Vector3.forward;
                    item.symmetricTangents = true;
                }
            }
        }

        waypoints.InsertArrayElementAtIndex(index);

        waypoints.GetArrayElementAtIndex(index).FindPropertyRelative("position").vector3Value = item.position;
        waypoints.GetArrayElementAtIndex(index).FindPropertyRelative("velocity").floatValue = item.velocity;
        waypoints.GetArrayElementAtIndex(index).FindPropertyRelative("inTangent").vector3Value = item.inTangent;
        waypoints.GetArrayElementAtIndex(index).FindPropertyRelative("outTangent").vector3Value = item.outTangent;
        waypoints.GetArrayElementAtIndex(index).FindPropertyRelative("symmetricTangents").boolValue = true;

        //現在選択しているポイント
        currentSelectedWaypoint = index;

        //シーンビューに反映
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.pivot = ((PlayerPath)serializedObject.targetObject).transform.TransformPoint(waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("position").vector3Value);
        }
    }

    /*! @brief 指定されたポイントの削除*/
    private void RemoveWaypointAt(int index)
    {
        waypoints.DeleteArrayElementAtIndex(index);
    }

    /*! @brief パス全体のポイントの補間値の計算*/
    private float CalcPosForWaypointIndex(int index)
    {
        //return (float)index / (float)(serializedObject.FindProperty ("waypoints").arraySize - (((PlayerPath)serializedObject.targetObject).loop ? 0f : 1f));
        return (float)index / (((PlayerPath)target).waypoints.Length - (((PlayerPath)target).loop ? 0f : 1f));
    }

    /*! @brief 特定のポイントの補間値の計算*/
    public float ComputePosForWaypoint(int waypoint)
    {
        PlayerPath pm = (PlayerPath)target;
        float pos = 0f;
        float step = 0.0001f;

        int i = 0;
        while (pm.waypointSamples[i] != waypoint)
        {
            pos += pm.samplesDistances[i++];
        }

        pos /= pm.totalDistance;


        float p = pos;
        Vector3 position;
        float vel;
        int wp;
        float lastDistanceFromWaypoint;

        pm.GetSampledWayPoint(p, out position, out vel, out wp);

        do
        {
            lastDistanceFromWaypoint = Vector3.Distance(position, pm.waypoints[waypoint].position);

            p += step;
            if (p > 1f)
                p = 1f;

            pm.GetSampledWayPoint(p, out position, out vel, out wp);
        } while (Vector3.Distance(position, pm.waypoints[waypoint].position) <= lastDistanceFromWaypoint && p < 1);

        pos = p;

        return pos;
    }

    /*! @brief 指定されたポイント到達時の進行方向取得*/
    private Quaternion GetFaceForwardForIndex(int index)
    {
        Quaternion rot;
        if (((PlayerPath)serializedObject.targetObject).waypoints.Length <= 1)
            rot = Quaternion.identity;
        else
        {
            float pos = CalcPosForWaypointIndex(index);
            if (index < ((PlayerPath)serializedObject.targetObject).waypoints.Length - 1)
            {
                rot = Quaternion.LookRotation(((PlayerPath)serializedObject.targetObject).computePositionAtPos(pos + 0.001f) - ((PlayerPath)serializedObject.targetObject).computePositionAtPos(pos), Vector3.up);
            }
            else
                rot = Quaternion.LookRotation(((PlayerPath)serializedObject.targetObject).computePositionAtPos(pos) - ((PlayerPath)serializedObject.targetObject).computePositionAtPos(pos - 0.001f), Vector3.up);
        }

        return rot;
    }

    /*! @brief ポイントの向きの設定。オブジェクトが前方を向くようにする。*/
    private void FaceForward(int index)
    {
        waypoints.GetArrayElementAtIndex(index).FindPropertyRelative("rotation").vector3Value = GetFaceForwardForIndex(index).eulerAngles;
    }

    /*! @brief ポジションハンドルの作成*/
    private Vector3 PositionHandle(Vector3 position, Quaternion rotation, bool mini)
    {
        //float handleSize = HandleUtility.GetHandleSize(position) / (mini ? 2f : 1f);
        Color color = Handles.color;

        bool xPresent = true;

        if (SceneView.sceneViews.Count > 0)
            if (((SceneView)SceneView.sceneViews[0]).in2DMode)
                xPresent = false;

        if (xPresent)
        {
            Handles.color = Handles.xAxisColor;
            position = Handles.PositionHandle(position, Quaternion.identity);
        }


        Handles.color = color;
        return position;
    }

    /*! @brief パスオブジェクトの作成*/
    [MenuItem("GameObject/Path/Create new PlayerPath")]
    public static void CreateNewPlayerPath(MenuCommand menuCommand)
    {
        //オブジェクトの生成
        GameObject go = new GameObject("PlayerPath");
        go.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
        Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);

        go.AddComponent<PlayerPath>();
        Selection.activeObject = go;

        Undo.RegisterCreatedObjectUndo(go, "Create new PlayerPath");
    }
}