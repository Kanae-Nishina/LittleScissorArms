/*
 * @file CameraWorkInspector.cs
 * @brief カメラワーク拡張エディター
 * @date 2017/04/21
 * @author 仁科香苗
 * @note 参考:CameraWork(https://www.assetstore.unity3d.com/jp/#!/content/47769)
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using System;

//カメラワークのエディタ拡張
[CustomEditor(typeof(CameraWork))]
[CanEditMultipleObjects]
public class CameraWorkInspector : Editor
{
    private bool waypointsFoldout = true;   //ポイント折りたたみフラグ
    private bool previewFoldout = true; //プレビュー折りたたみフラグ
    private bool eventsFoldout = true;  //イベント折りたたみフラグ
    private bool utilityFoldout = true;//ユーティリティー折りたたみフラグ
    private bool showPathSamples = true;//パスサンプルの可視化
    private bool showTangents = true;//接線の可視化

    [SerializeField]
    private ReorderableList wl; //入れ替え可能なリスト
    private int currentSelectedWaypoint = -1;   //現在選択されているポイント
    private GUIStyle boldFoldoutStyle;//折りたたみの種類
    private GUIStyle actionButtonStyleLeft;//アクションボタンスタイル左側
    private GUIStyle actionButtonStyleRight;//アクションボタンスタイル右側
    private GUIStyle rightMiniButton;//ボタンスタイル

    //アクティブ時のイベント
    void OnEnable()
    {
        ////showPathSamples = EditorPrefs.GetBool("CameraWork.ShowPathSamples", false);
        //showTangents = EditorPrefs.GetBool("CameraWork.ShowTangents", true);

        ////SerializedProperty waypoints = serializedObject.FindProperty("waypoints");

        //wl = new ReorderableList(serializedObject, waypoints);

        //wl.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        //{
        //    if (index > waypoints.arraySize - 1)
        //    {
        //        return;
        //    }

        //    rect.y += 2;
        //    EditorGUIUtility.labelWidth = 20;

        //    if (GUI.Button(new Rect(rect.x, rect.y, EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight), "\u2023"))
        //    {
        //        CameraWork pm = (CameraWork)target;
        //        pm.CurrentPos = ComputePosForWaypoint(index);

        //    }

        //    EditorGUI.PropertyField(
        //        new Rect(rect.x + EditorGUIUtility.singleLineHeight, rect.y, rect.width - 120 - EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight),
        //        waypoints.GetArrayElementAtIndex(index).FindPropertyRelative("position"),
        //        new GUIContent("" + (index + 1)));

        //    EditorGUIUtility.labelWidth = 30;

        //    if (serializedObject.FindProperty("globalLookAt").objectReferenceValue != null)
        //        GUI.enabled = false;

        //    EditorGUI.PropertyField(
        //        new Rect(rect.x + rect.width - 120, rect.y, 120, EditorGUIUtility.singleLineHeight),
        //        waypoints.GetArrayElementAtIndex(index).FindPropertyRelative("lookAt"),
        //        new GUIContent("注視点"));

        //    GUI.enabled = true;
        //};

        //wl.drawHeaderCallback = (Rect rect) =>
        //{
        //    EditorGUI.LabelField(rect, "ポイント");
        //};

        //wl.onRemoveCallback = (ReorderableList l) =>
        //{
        //    if (EditorUtility.DisplayDialog("けいこく！",
        //            "ポイントを消します。", "OK！", "ダメ！"))
        //    {

        //        waypoints.DeleteArrayElementAtIndex(l.index);
        //        if (currentSelectedWaypoint >= waypoints.arraySize)
        //            currentSelectedWaypoint = -1;
        //    }
        //};

        //wl.onCanRemoveCallback = (ReorderableList l) =>
        //{
        //    return true;
        //};

        //wl.onSelectCallback = (ReorderableList l) =>
        //{
        //    currentSelectedWaypoint = l.index;
        //    SceneView.RepaintAll();
        //};

        //wl.onAddCallback = (ReorderableList l) =>
        //{
        //    if (currentSelectedWaypoint == -1)
        //    {
        //        InsertWaypointAt(waypoints.arraySize, true);
        //    }
        //    else
        //    {
        //        InsertWaypointAt(currentSelectedWaypoint + 1, true);
        //    }
        //};

    }

    //インスペクターの表示
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        boldFoldoutStyle = new GUIStyle(EditorStyles.foldout);
        rightMiniButton = new GUIStyle(EditorStyles.miniButton);

        boldFoldoutStyle.fontStyle = FontStyle.Bold;
        rightMiniButton.fixedWidth = 100;

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

        //SerializedProperty waypoints = serializedObject.FindProperty("waypoints");

        EditorGUIUtility.labelWidth = 120;

        //if (wl == null)
        //    OnEnable();

        //wl.index = currentSelectedWaypoint;

        #region Base
        var t = target as CameraWork;

        EditorGUILayout.BeginVertical("Box");
        if (!serializedObject.isEditingMultipleObjects)
            t.target = (Transform)EditorGUILayout.ObjectField("カメラ", t.target, typeof(Transform), true);
        //EditorGUILayout.PropertyField(serializedObject.FindProperty("target"), new GUIContent("パスに添わせるカメラ"));

        //EditorGUILayout.PropertyField(serializedObject.FindProperty("player"), new GUIContent("メインプレイヤー"));
        t.player = (MainCharacterController)EditorGUILayout.ObjectField("プレイヤー", t.player, typeof(MainCharacterController), true);

        EditorGUIUtility.labelWidth = 90;
        //EditorGUILayout.BeginHorizontal();
        //EditorGUILayout.PropertyField(serializedObject.FindProperty("loop"), new GUIContent("パスをループさせるか"));
        //EditorGUILayout.EndHorizontal();
        //EditorGUILayout.Slider(serializedObject.FindProperty("speed"), 0.001f, 0.1f, new GUIContent("移動速度"));
        //EditorGUILayout.Slider(serializedObject.FindProperty("offsetY"), 0f, 10f, new GUIContent("オフセットY軸"));
        //EditorGUILayout.Slider(serializedObject.FindProperty("dist"), 7f, 20f, new GUIContent("プレイヤーとの距離"));
        //EditorGUILayout.Slider(serializedObject.FindProperty("zoomOutDist"), 1f, 10f, new GUIContent("ズームアウトの距離"));
        t.offsetY = EditorGUILayout.Slider("Y軸オフセット", t.offsetY, 0f, 10f);
        t.dist = EditorGUILayout.Slider("距離", t.dist, 5f, 20f);
        EditorGUILayout.LabelField("ズームアウトでの加算値");
        t.zoomOutDist = EditorGUILayout.Slider("", t.zoomOutDist, 1f, 10f);

        EditorGUIUtility.labelWidth = 120;

        //EditorGUILayout.BeginHorizontal(GUILayout.Width(300f));

        //EditorGUILayout.BeginVertical(GUILayout.Width(150f));
        //EditorGUILayout.PropertyField(serializedObject.FindProperty("presampledPath"), new GUIContent("移動の等速化"), GUILayout.Width(150f));

        //if (serializedObject.FindProperty("presampledPath").boolValue)
        //{
        //    EditorGUILayout.PropertyField(serializedObject.FindProperty("samplesNum"), new GUIContent("サンプリング数", "数が多いほど精度が上がる代わりにパフォーマンスに影響が出ます。"), GUILayout.Width(150f));
        //    if (serializedObject.FindProperty("samplesNum").intValue <= 5)
        //        serializedObject.FindProperty("samplesNum").intValue = 5;
        //    if (serializedObject.FindProperty("samplesNum").intValue >= 10000)
        //        serializedObject.FindProperty("samplesNum").intValue = 10000;
        //}

        //EditorGUILayout.EndVertical();
        //EditorGUILayout.EndHorizontal();
        EditorGUIUtility.labelWidth = 90;
        t.globalLookAt = (Transform)EditorGUILayout.ObjectField("注視点", t.globalLookAt, typeof(Transform), true);

        GUI.enabled = true;

        EditorGUILayout.EndVertical();
        EditorGUILayout.Separator();
        EditorUtility.SetDirty(t); //インスペクター上のGUI変更メソッド

        #endregion

        //OffSetY();

        //DistCurve();

        #region WayPoint
        //if (!waypoints.hasMultipleDifferentValues)
        //{
        //    wl.DoLayoutList();
        //}
        //else
        //{
        //    currentSelectedWaypoint = -1;
        //    EditorGUILayout.HelpBox("You can't edit waypoints data because waypoints of selected paths are not the same.", MessageType.Info);
        //}

        //if (currentSelectedWaypoint > waypoints.arraySize - 1)
        //    currentSelectedWaypoint = -1;

        //if (currentSelectedWaypoint != -1)
        //{
        //    EditorGUIUtility.labelWidth = 60;
        //    EditorGUILayout.BeginHorizontal();

        //    waypointsFoldout = EditorGUILayout.Foldout(waypointsFoldout, "Waypoint " + (currentSelectedWaypoint + 1), boldFoldoutStyle);
        //    if (GUILayout.Button(new GUIContent("ポイントの明確化", "選択したポイントを見やすくします。"), EditorStyles.miniButtonLeft, GUILayout.Width(60)))
        //    {
        //        if (SceneView.lastActiveSceneView != null)
        //        {
        //            SceneView.lastActiveSceneView.pivot = ((CameraWork)serializedObject.targetObject).transform.TransformPoint(waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("position").vector3Value);
        //        }
        //    }
        //    if (GUILayout.Button(new GUIContent("追加", "選んだポイントの次のポイントを追加します。"), EditorStyles.miniButtonMid, GUILayout.Width(60)))
        //    {
        //        if (currentSelectedWaypoint == -1)
        //        {
        //            InsertWaypointAt(waypoints.arraySize, true);
        //        }
        //        else
        //        {
        //            InsertWaypointAt(currentSelectedWaypoint + 1, true);
        //        }
        //    }
        //    if (GUILayout.Button(new GUIContent("削除", "選んだポイントを削除します。"), EditorStyles.miniButtonRight, GUILayout.Width(60)))
        //    {
        //        RemoveWaypointAt(currentSelectedWaypoint);
        //        currentSelectedWaypoint = (currentSelectedWaypoint - 1) % (waypoints.arraySize);
        //    }

        //    EditorGUILayout.EndHorizontal();

        //    if (waypointsFoldout)
        //    {

        //        EditorGUILayout.BeginVertical("Box");
        //        EditorGUILayout.BeginHorizontal();

        //        EditorGUILayout.PropertyField(waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("position"),
        //            new GUIContent("座標"));

        //        GUI.enabled = currentSelectedWaypoint > 0;
        //        if (GUILayout.Button(new GUIContent("整列化", "前後のポイントと一直線上になるよう配置します。"), rightMiniButton))
        //        {
        //            RemoveWaypointAt(currentSelectedWaypoint);
        //            currentSelectedWaypoint = ((currentSelectedWaypoint - 1) % waypoints.arraySize);

        //            serializedObject.ApplyModifiedProperties();

        //            if (currentSelectedWaypoint == -1)
        //            {
        //                InsertWaypointAt(waypoints.arraySize, true);
        //            }
        //            else
        //            {
        //                InsertWaypointAt(currentSelectedWaypoint + 1, true);
        //            }
        //        }
        //        GUI.enabled = true;
        //        EditorGUILayout.EndHorizontal();

        //        EditorGUILayout.BeginHorizontal();

        //        if (serializedObject.FindProperty("globalLookAt").objectReferenceValue != null)
        //            GUI.enabled = false;

        //        if (waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("lookAt").objectReferenceValue != null)
        //            GUI.enabled = false;

        //        EditorGUILayout.PropertyField(waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("rotation"),
        //            new GUIContent("回転"));

        //        if (GUILayout.Button(new GUIContent("正面回転", "回転を正面にします。"), rightMiniButton))
        //        {
        //            FaceForward(currentSelectedWaypoint);
        //        }

        //        GUI.enabled = true;

        //        EditorGUILayout.EndHorizontal();

        //        //EditorGUILayout.PropertyField(waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("offsetY"), new GUIContent("オフセットY ", "次のポイントまでのY軸オフセット"));
        //        //EditorGUILayout.PropertyField(waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("distance"), new GUIContent("距離 ", "次のポイントまでのプレイヤーとの距離"));
        //        //EditorGUILayout.BeginHorizontal();
        //        //EditorGUILayout.PropertyField(waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("velocity"), new GUIContent("速度", "ポイント通過の速度変更"));

        //        //EditorGUIUtility.labelWidth = 20;

        //        //EditorGUILayout.PropertyField(waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("inVariation"), new GUIContent("In", "イージーインの速度"));

        //        //EditorGUIUtility.labelWidth = 30;

        //        //EditorGUILayout.PropertyField(waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("outVariation"), new GUIContent("Out", "イージーアウトの速度"));

        //        //EditorGUIUtility.labelWidth = 60;

        //        //GUI.enabled = (currentSelectedWaypoint > 0 && currentSelectedWaypoint < (waypoints.arraySize - 1));
        //        //if (GUILayout.Button(new GUIContent("平均化", "パスの前後の平均値で速度を設定します。"), rightMiniButton))
        //        //{
        //        //    waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("velocity").floatValue =
        //        //    (waypoints.GetArrayElementAtIndex(currentSelectedWaypoint - 1).FindPropertyRelative("velocity").floatValue +
        //        //    waypoints.GetArrayElementAtIndex(currentSelectedWaypoint + 1).FindPropertyRelative("velocity").floatValue) / 2f;
        //        //}
        //        //GUI.enabled = true;
        //        //EditorGUILayout.EndHorizontal();

        //        EditorGUIUtility.labelWidth = 130;
        //        EditorGUILayout.PropertyField(waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("symmetricTangents"), new GUIContent("ハンドルの対象化", "チェックを入れるとハンドルが非対象になります。"));
        //        EditorGUIUtility.labelWidth = 80;

        //        EditorGUILayout.PropertyField(waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("inTangent"),
        //            new GUIContent("In Tangent", "ポイントに入るハンドル"));
        //        if (waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("symmetricTangents").boolValue)
        //            waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("outTangent").vector3Value = -waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("inTangent").vector3Value;


        //        if (!waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("symmetricTangents").boolValue)
        //        {
        //            EditorGUILayout.PropertyField(waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("outTangent"),
        //                new GUIContent("Out Tangent", "ポイントから出るハンドル"));
        //            if (waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("symmetricTangents").boolValue)
        //                waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("inTangent").vector3Value = -waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("outTangent").vector3Value;
        //        }

        //        EditorGUIUtility.labelWidth = 60;

        //        EditorGUILayout.Separator();

        //        EditorGUILayout.PropertyField(waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("reached"));

        //        EditorGUILayout.EndVertical();
        //    }

        //    EditorGUILayout.Separator();
        //}
        #endregion
        #region イベント
        //eventsFoldout = EditorGUILayout.Foldout(eventsFoldout, "Events", boldFoldoutStyle);
        //if (eventsFoldout)
        //{
        //    EditorGUILayout.PropertyField(serializedObject.FindProperty("waypointChanged"));
        //}

        //EditorGUILayout.BeginHorizontal();
        #endregion
        #region プレビュー
        //previewFoldout = EditorGUILayout.Foldout(previewFoldout, "Preview", boldFoldoutStyle);

        //EditorGUILayout.EndHorizontal();
        //if (previewFoldout)
        //{

        //    EditorGUILayout.BeginVertical("Box");
        //    EditorGUILayout.BeginHorizontal();

        //    EditorGUIUtility.labelWidth = 70;
        //    EditorGUILayout.Slider(serializedObject.FindProperty("currentPos"), 0f, 1f, new GUIContent("座標", "パス上の補間値における座標"));

        //    EditorGUIUtility.labelWidth = 120;
        //    EditorGUILayout.PropertyField(serializedObject.FindProperty("updateTransform"), new GUIContent("移動の更新", "アニメーション中に移動を更新するかどうか"));
        //    EditorGUIUtility.labelWidth = 70;

        //    for (int i = 0; i < targets.Length; i++)
        //    {
        //        if (!((CameraWork)targets[i]).updateTransform)
        //        {
        //            if (((CameraWork)targets[i]).presampledPath)
        //            {
        //                CameraWork pmo = (CameraWork)targets[i];
        //                Vector3 position = Vector3.zero;
        //                Quaternion rotation = Quaternion.identity;
        //                float velocity = 0f;
        //                int waypoint = 0;
        //                pmo.sampledPositionAndRotationAndVelocityAndWaypointAtPos(pmo.currentPos, out position, out rotation, out velocity, out waypoint);
        //                pmo.UpdateTarget(position, rotation);
        //            }
        //            else
        //            {
        //                ((CameraWork)targets[i]).UpdateTarget(
        //                    ((CameraWork)targets[i]).computePositionAtPos(((CameraWork)targets[i]).currentPos),
        //                    ((CameraWork)targets[i]).computeRotationAtPos(((CameraWork)targets[i]).currentPos)
        //                );
        //            }
        //        }
        //    }

        //    EditorGUILayout.EndHorizontal();
        //    EditorGUILayout.EndVertical();
        //}
        #endregion
        #region ユーティリティ
        //// 単一パスの編集中、他のユーティリティ機能の有効化
        //EditorGUILayout.BeginHorizontal();
        //utilityFoldout = EditorGUILayout.Foldout(utilityFoldout, "Utility", boldFoldoutStyle);
        //EditorGUILayout.EndHorizontal();
        //if (utilityFoldout)
        //{

        //    EditorGUILayout.BeginVertical("Box");
        //    EditorGUIUtility.labelWidth = 120;

        //    EditorGUILayout.PropertyField(serializedObject.FindProperty("pathColor"), new GUIContent("パスの色"));

        //    if (serializedObject.FindProperty("presampledPath").boolValue)
        //    {
        //        EditorGUI.BeginChangeCheck();
        //        showPathSamples = EditorGUILayout.Toggle(new GUIContent("サンプリングの表示"), showPathSamples);
        //        if (EditorGUI.EndChangeCheck())
        //        {
        //            EditorPrefs.SetBool("CameraWork.ShowPathSamples", showPathSamples);
        //            ((SceneView)SceneView.sceneViews[0]).Repaint();
        //        }
        //    }


        //    EditorGUI.BeginChangeCheck();
        //    showTangents = EditorGUILayout.Toggle(new GUIContent("ハンドルの表示"), showTangents);
        //    if (EditorGUI.EndChangeCheck())
        //    {
        //        EditorPrefs.SetBool("CameraWork.ShowTangents", showTangents);
        //        ((SceneView)SceneView.sceneViews[0]).Repaint();
        //    }

        //    EditorGUILayout.EndVertical();

        //}

        //EditorGUILayout.Separator();

        //serializedObject.ApplyModifiedProperties();
        //if (serializedObject.FindProperty("presampledPath").boolValue)
        //    ((CameraWork)serializedObject.targetObject).UpdatePathSamples();
        #endregion
    }

    //基本設定
    void Base()
    {

        if (!serializedObject.isEditingMultipleObjects)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("target"), new GUIContent("パスに添わせるカメラ"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("player"), new GUIContent("メインプレイヤー"));

        EditorGUIUtility.labelWidth = 90;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("loop"), new GUIContent("パスをループさせるか"));

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Slider(serializedObject.FindProperty("speed"), 0.001f, 0.1f, new GUIContent("移動速度"));
        EditorGUILayout.Slider(serializedObject.FindProperty("zoomOutDist"), 1f, 10f, new GUIContent("ズームアウトの距離"));

        EditorGUIUtility.labelWidth = 120;

        EditorGUILayout.BeginHorizontal(GUILayout.Width(300f));

        EditorGUILayout.BeginVertical(GUILayout.Width(150f));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("presampledPath"), new GUIContent("移動の等速化"), GUILayout.Width(150f));

        if (serializedObject.FindProperty("presampledPath").boolValue)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("samplesNum"), new GUIContent("サンプリング数", "数が多いほど精度が上がる代わりにパフォーマンスに影響が出ます。"), GUILayout.Width(150f));
            if (serializedObject.FindProperty("samplesNum").intValue <= 5)
                serializedObject.FindProperty("samplesNum").intValue = 5;
            if (serializedObject.FindProperty("samplesNum").intValue >= 10000)
                serializedObject.FindProperty("samplesNum").intValue = 10000;
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        EditorGUIUtility.labelWidth = 90;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("globalLookAt"), new GUIContent("全体の注視点"));

        GUI.enabled = true;

        EditorGUILayout.EndVertical();
        EditorGUILayout.Separator();
    }

    //Y軸オフセット
    void OffSetY()
    {
        EditorGUILayout.BeginVertical("Box");
        //var t = target as CameraWork;
        EditorGUILayout.LabelField("Y軸のオフセット");
        // t.offsetY = EditorGUILayout.CurveField("Yのオフセット", t.offsetY);
        serializedObject.FindProperty("offsetY").animationCurveValue = EditorGUILayout.CurveField(serializedObject.FindProperty("offsetY").animationCurveValue);
        EditorGUILayout.EndVertical();
    }

    void DistCurve()
    {
        EditorGUILayout.BeginVertical("Box");
        //var t = target as CameraWork;
        EditorGUILayout.LabelField("プレイヤー座標による距離");
        //t.dist = EditorGUILayout.CurveField("距離", t.dist);
        serializedObject.FindProperty("dist").animationCurveValue = EditorGUILayout.CurveField(serializedObject.FindProperty("dist").animationCurveValue);
        EditorGUILayout.EndVertical();
    }

    //ポイント設定
    void WayPointsetting(SerializedProperty waypoints)
    {
        if (!waypoints.hasMultipleDifferentValues)
        {
            wl.DoLayoutList();
        }
        else
        {
            currentSelectedWaypoint = -1;
            EditorGUILayout.HelpBox("You can't edit waypoints data because waypoints of selected paths are not the same.", MessageType.Info);
        }

        if (currentSelectedWaypoint > waypoints.arraySize - 1)
            currentSelectedWaypoint = -1;

        if (currentSelectedWaypoint != -1)
        {
            EditorGUIUtility.labelWidth = 60;
            EditorGUILayout.BeginHorizontal();

            waypointsFoldout = EditorGUILayout.Foldout(waypointsFoldout, "Waypoint " + (currentSelectedWaypoint + 1), boldFoldoutStyle);
            if (GUILayout.Button(new GUIContent("ポイントの明確化", "選択したポイントを見やすくします。"), EditorStyles.miniButtonLeft, GUILayout.Width(60)))
            {
                if (SceneView.lastActiveSceneView != null)
                {
                    SceneView.lastActiveSceneView.pivot = ((CameraWork)serializedObject.targetObject).transform.TransformPoint(waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("position").vector3Value);
                }
            }
            if (GUILayout.Button(new GUIContent("追加", "選んだポイントの次のポイントを追加します。"), EditorStyles.miniButtonMid, GUILayout.Width(60)))
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
            if (GUILayout.Button(new GUIContent("削除", "選んだポイントを削除します。"), EditorStyles.miniButtonRight, GUILayout.Width(60)))
            {
                RemoveWaypointAt(currentSelectedWaypoint);
                currentSelectedWaypoint = (currentSelectedWaypoint - 1) % (waypoints.arraySize);
            }

            EditorGUILayout.EndHorizontal();

            if (waypointsFoldout)
            {

                EditorGUILayout.BeginVertical("Box");
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.PropertyField(waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("position"),
                    new GUIContent("座標"));

                GUI.enabled = currentSelectedWaypoint > 0;
                if (GUILayout.Button(new GUIContent("整列化", "前後のポイントと一直線上になるよう配置します。"), rightMiniButton))
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

                if (serializedObject.FindProperty("globalLookAt").objectReferenceValue != null)
                    GUI.enabled = false;

                if (waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("lookAt").objectReferenceValue != null)
                    GUI.enabled = false;

                EditorGUILayout.PropertyField(waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("rotation"),
                    new GUIContent("回転"));

                if (GUILayout.Button(new GUIContent("正面回転", "回転を正面にします。"), rightMiniButton))
                {
                    FaceForward(currentSelectedWaypoint);
                }

                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("velocity"), new GUIContent("速度", "ポイント通過の速度変更"));

                EditorGUIUtility.labelWidth = 20;

                EditorGUILayout.PropertyField(waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("inVariation"), new GUIContent("In", "イージーインの速度"));

                EditorGUIUtility.labelWidth = 30;

                EditorGUILayout.PropertyField(waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("outVariation"), new GUIContent("Out", "イージーアウトの速度"));

                EditorGUIUtility.labelWidth = 60;

                GUI.enabled = (currentSelectedWaypoint > 0 && currentSelectedWaypoint < (waypoints.arraySize - 1));
                if (GUILayout.Button(new GUIContent("平均化", "パスの前後の平均値で速度を設定します。"), rightMiniButton))
                {
                    waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("velocity").floatValue =
                    (waypoints.GetArrayElementAtIndex(currentSelectedWaypoint - 1).FindPropertyRelative("velocity").floatValue +
                    waypoints.GetArrayElementAtIndex(currentSelectedWaypoint + 1).FindPropertyRelative("velocity").floatValue) / 2f;
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();

                EditorGUIUtility.labelWidth = 130;
                EditorGUILayout.PropertyField(waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("symmetricTangents"), new GUIContent("ハンドルの対象化", "チェックを入れるとハンドルが非対象になります。"));
                EditorGUIUtility.labelWidth = 80;

                EditorGUILayout.PropertyField(waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("inTangent"),
                    new GUIContent("In Tangent", "ポイントに入るハンドル"));
                if (waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("symmetricTangents").boolValue)
                    waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("outTangent").vector3Value = -waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("inTangent").vector3Value;


                if (!waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("symmetricTangents").boolValue)
                {
                    EditorGUILayout.PropertyField(waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("outTangent"),
                        new GUIContent("Out Tangent", "ポイントから出るハンドル"));
                    if (waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("symmetricTangents").boolValue)
                        waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("inTangent").vector3Value = -waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("outTangent").vector3Value;
                }

                EditorGUIUtility.labelWidth = 60;

                EditorGUILayout.Separator();

                EditorGUILayout.PropertyField(waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("reached"));

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Separator();
        }
    }

    //イベント
    void EventSetting()
    {
        eventsFoldout = EditorGUILayout.Foldout(eventsFoldout, "Events", boldFoldoutStyle);
        if (eventsFoldout)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("waypointChanged"));
        }

        EditorGUILayout.BeginHorizontal();
    }

    //プレビュー
    void PreviewSetting()
    {
        previewFoldout = EditorGUILayout.Foldout(previewFoldout, "Preview", boldFoldoutStyle);

        EditorGUILayout.EndHorizontal();
        if (previewFoldout)
        {

            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.BeginHorizontal();

            EditorGUIUtility.labelWidth = 70;
            EditorGUILayout.Slider(serializedObject.FindProperty("currentPos"), 0f, 1f, new GUIContent("座標", "パス上の補間値における座標"));

            EditorGUIUtility.labelWidth = 120;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("updateTransform"), new GUIContent("移動の更新", "アニメーション中に移動を更新するかどうか"));
            EditorGUIUtility.labelWidth = 70;

            for (int i = 0; i < targets.Length; i++)
            {
                if (!((CameraWork)targets[i]).updateTransform)
                {
                    if (((CameraWork)targets[i]).presampledPath)
                    {
                        CameraWork pmo = (CameraWork)targets[i];
                        Vector3 position = Vector3.zero;
                        Quaternion rotation = Quaternion.identity;
                        float velocity = 0f;
                        int waypoint = 0;
                        pmo.sampledPositionAndRotationAndVelocityAndWaypointAtPos(pmo.currentPos, out position, out rotation, out velocity, out waypoint);
                        pmo.UpdateTarget(position, rotation);
                    }
                    else
                    {
                        ((CameraWork)targets[i]).UpdateTarget(
                            ((CameraWork)targets[i]).computePositionAtPos(((CameraWork)targets[i]).currentPos),
                            ((CameraWork)targets[i]).computeRotationAtPos(((CameraWork)targets[i]).currentPos)
                        );
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

    }

    //ユーティリティー
    void UtilitySetting()
    {
        // 単一パスの編集中、他のユーティリティ機能の有効化
        EditorGUILayout.BeginHorizontal();
        utilityFoldout = EditorGUILayout.Foldout(utilityFoldout, "Utility", boldFoldoutStyle);
        EditorGUILayout.EndHorizontal();
        if (utilityFoldout)
        {

            EditorGUILayout.BeginVertical("Box");
            EditorGUIUtility.labelWidth = 120;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("pathColor"), new GUIContent("パスの色"));

            if (serializedObject.FindProperty("presampledPath").boolValue)
            {
                EditorGUI.BeginChangeCheck();
                showPathSamples = EditorGUILayout.Toggle(new GUIContent("サンプリングの表示"), showPathSamples);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorPrefs.SetBool("CameraWork.ShowPathSamples", showPathSamples);
                    ((SceneView)SceneView.sceneViews[0]).Repaint();
                }
            }


            EditorGUI.BeginChangeCheck();
            showTangents = EditorGUILayout.Toggle(new GUIContent("ハンドルの表示"), showTangents);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool("CameraWork.ShowTangents", showTangents);
                ((SceneView)SceneView.sceneViews[0]).Repaint();
            }

            EditorGUILayout.EndVertical();

        }

        EditorGUILayout.Separator();

        serializedObject.ApplyModifiedProperties();
        if (serializedObject.FindProperty("presampledPath").boolValue)
            ((CameraWork)serializedObject.targetObject).UpdatePathSamples();
    }

#if true
    //シーンビュー表示
    void OnSceneGUI()
    {
        bool isGlobalMode = Tools.pivotRotation == PivotRotation.Global;

        SerializedObject pm = new SerializedObject(target);
        CameraWork pmo = (CameraWork)target;
        SerializedProperty waypoints = pm.FindProperty("waypoints");
        Handles.matrix = ((CameraWork)pm.targetObject).transform.localToWorldMatrix;


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

        //サンプリングの描画
        if (pmo.presampledPath)
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

        pm.ApplyModifiedProperties();
    }
#endif

    //ポイントの挿入と整列
    private void InsertWaypointAt(int index, bool align)
    {
        SerializedProperty waypoints = serializedObject.FindProperty("waypoints");

        Waypoint item = new Waypoint();

        if (align)
        {
            if (index < waypoints.arraySize)
            {
                float pos1 = CalcPosForWaypointIndex(index - 1);
                float pos2 = CalcPosForWaypointIndex(index);
                float pos = (pos1 + pos2) / 2f;

                item.position = ((CameraWork)serializedObject.targetObject).computePositionAtPos(pos);
                item.rotation = ((CameraWork)serializedObject.targetObject).computeRotationAtPos(pos).eulerAngles;
                item.velocity = (((CameraWork)serializedObject.targetObject).waypoints[index - 1].velocity + ((CameraWork)serializedObject.targetObject).waypoints[index].velocity) / 2f;

                Quaternion fForward = Quaternion.LookRotation(((CameraWork)serializedObject.targetObject).computePositionAtPos(pos + 0.001f) - ((CameraWork)serializedObject.targetObject).computePositionAtPos(pos), Vector3.up);
                item.inTangent = -1 * Vector3.forward;
                item.outTangent = fForward * Vector3.forward;

            }
            else
            {
                //最後のポイント
                if (waypoints.arraySize > 0)
                {
                    item.position = ((CameraWork)serializedObject.targetObject).waypoints[index - 1].position + 5f * (GetFaceForwardForIndex(index - 1) * Vector3.forward);
                    item.rotation = GetFaceForwardForIndex(index - 1).eulerAngles;
                    item.velocity = ((CameraWork)serializedObject.targetObject).waypoints[index - 1].velocity;
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
        waypoints.GetArrayElementAtIndex(index).FindPropertyRelative("rotation").vector3Value = item.rotation;
        waypoints.GetArrayElementAtIndex(index).FindPropertyRelative("velocity").floatValue = item.velocity;
        waypoints.GetArrayElementAtIndex(index).FindPropertyRelative("inTangent").vector3Value = item.inTangent;
        waypoints.GetArrayElementAtIndex(index).FindPropertyRelative("outTangent").vector3Value = item.outTangent;
        waypoints.GetArrayElementAtIndex(index).FindPropertyRelative("symmetricTangents").boolValue = true;

        //現在選択しているポイント
        currentSelectedWaypoint = index;

        //シーンビューに反映
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.pivot = ((CameraWork)serializedObject.targetObject).transform.TransformPoint(waypoints.GetArrayElementAtIndex(currentSelectedWaypoint).FindPropertyRelative("position").vector3Value);
        }
    }

    //指定されたポイントの削除
    private void RemoveWaypointAt(int index)
    {
        serializedObject.FindProperty("waypoints").DeleteArrayElementAtIndex(index);
    }

    //パス全体のポイントの補間値の計算
    private float CalcPosForWaypointIndex(int index)
    {
        //return (float)index / (float)(serializedObject.FindProperty ("waypoints").arraySize - (((CameraWork)serializedObject.targetObject).loop ? 0f : 1f));
        return (float)index / (((CameraWork)target).waypoints.Length - (((CameraWork)target).loop ? 0f : 1f));
    }

    //特定のポイントの補間値の計算
    public float ComputePosForWaypoint(int waypoint)
    {
        CameraWork pm = (CameraWork)target;
        float pos = 0f;
        float step = 0.0001f;

        if (!pm.presampledPath)
        {
            pos = CalcPosForWaypointIndex(waypoint);
        }
        else
        {
            int i = 0;
            while (pm.WaypointSamples[i] != waypoint)
            {
                pos += pm.SamplesDistances[i++];
            }

            pos /= pm.TotalDistance;


            float p = pos;
            Vector3 position;
            Quaternion rotation;
            float vel;
            int wp;
            float lastDistanceFromWaypoint;

            pm.sampledPositionAndRotationAndVelocityAndWaypointAtPos(p, out position, out rotation, out vel, out wp);

            do
            {
                lastDistanceFromWaypoint = Vector3.Distance(position, pm.Waypoints[waypoint].Position);

                p += step;
                if (p > 1f)
                    p = 1f;

                pm.sampledPositionAndRotationAndVelocityAndWaypointAtPos(p, out position, out rotation, out vel, out wp);
            } while (Vector3.Distance(position, pm.Waypoints[waypoint].Position) <= lastDistanceFromWaypoint && p < 1);

            pos = p;
        }

        return pos;
    }

    //指定されたポイント到達時の進行方向取得
    private Quaternion GetFaceForwardForIndex(int index)
    {
        Quaternion rot;
        if (((CameraWork)serializedObject.targetObject).waypoints.Length <= 1)
            rot = Quaternion.identity;
        else
        {
            float pos = CalcPosForWaypointIndex(index);
            if (index < ((CameraWork)serializedObject.targetObject).waypoints.Length - 1)
            {
                rot = Quaternion.LookRotation(((CameraWork)serializedObject.targetObject).computePositionAtPos(pos + 0.001f) - ((CameraWork)serializedObject.targetObject).computePositionAtPos(pos), Vector3.up);
            }
            else
                rot = Quaternion.LookRotation(((CameraWork)serializedObject.targetObject).computePositionAtPos(pos) - ((CameraWork)serializedObject.targetObject).computePositionAtPos(pos - 0.001f), Vector3.up);
        }

        return rot;
    }

    //ポイントの向きの設定。オブジェクトが前方を向くようにする。
    private void FaceForward(int index)
    {
        serializedObject.FindProperty("waypoints").GetArrayElementAtIndex(index).FindPropertyRelative("rotation").vector3Value = GetFaceForwardForIndex(index).eulerAngles;
    }

    //ポジションハンドルの作成
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

    [MenuItem("GameObject/Path/Create new CameraWork")]
    //パスオブジェクトの作成
    public static void CreateNewCameraWork(MenuCommand menuCommand)
    {
        //オブジェクトの生成
        GameObject go = new GameObject("CameraWork");
        go.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
        Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);

        go.AddComponent<CameraWork>();
        Selection.activeObject = go;

        Undo.RegisterCreatedObjectUndo(go, "Create new CameraWork");
    }
}


