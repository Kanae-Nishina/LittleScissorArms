/*!
 * @file CameraWorkInspector.cs
 * @brief カメラワーク拡張エディター
 * @date 2017/04/21
 * @author 仁科香苗
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using System;

[CustomEditor(typeof(CameraWork))]
[CanEditMultipleObjects]
/*! @brief カメラワークのエディタ拡張*/
public class CameraWorkInspector : Editor
{
    SerializedProperty cameraWayPoint;             /*! カメラのポイント*/
    CameraWork t;                                                       /*! 拡張するカメラワークスクリプト*/
    private float selectPos;                                        /*! 現在のプレイヤーパス上の位置*/
    private int scrollSize = 100;                                 /*! スクロールサイズ*/
    private Vector2 scrollPos = Vector3.zero;      /*! スクロースビューの位置*/
    private bool isBaseFoldout=true;                     /*! 基本設定の折りたたみフラグ*/
    private bool isBulkSettingFoldout = true;      /*! 一括設定の折りたたみフラグ*/
    private bool isWaypointFoldout = true;          /*! ポイント情報の折りたたみフラグ*/
    private bool isPreviewFoldout = true;             /*! プレビューの折りたたみフラグ*/


    /*! @brief アクティブ時初期化*/
    private void OnEnable()
    {
        cameraWayPoint = serializedObject.FindProperty("cameraWaypoints");
    }

    /*! @brief インスペクターの表示*/
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        t = target as CameraWork;
        Base();                                     //基本設定
        BulkSetting();                        //一括設定
        CameraInfoSetting();          //ポイント情報設定
        Preview();                               //プレビュー
        EditorUtility.SetDirty(t);
        serializedObject.ApplyModifiedProperties();
    }
    
    /*! @brief 基本設定*/
    void Base()
    {
        isBaseFoldout = EditorGUILayout.Foldout(isBaseFoldout, "基本設定", true);
        if (isBaseFoldout)
        {
            EditorGUILayout.BeginVertical("Box");
            if (!serializedObject.isEditingMultipleObjects)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("target"), new GUIContent("カメラ"));
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("player"), new GUIContent("プレイヤー"));
            EditorGUILayout.Slider(serializedObject.FindProperty("zoomOutDist"), 0f, 10f, new GUIContent("ズームアウトでの加算値"));
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.Separator();
    }

    /*! @brief 一括設定*/
    void BulkSetting()
    {
        isBulkSettingFoldout = EditorGUILayout.Foldout(isBulkSettingFoldout, "一括設定", true);
        if (isBulkSettingFoldout)
        {
            EditorGUILayout.BeginVertical("Box");
            int size = cameraWayPoint.arraySize;
            EditorGUIUtility.labelWidth = 100;
            EditorGUILayout.BeginHorizontal(); //offsetY
            EditorGUILayout.Slider(serializedObject.FindProperty("offsetY"), 1f, 10f, new GUIContent("Y軸オフセット"));
            if (GUILayout.Button("一括設定"))
            {
                for (int i = 0; i < size; i++)
                {
                    cameraWayPoint.GetArrayElementAtIndex(i).FindPropertyRelative("offsetY").floatValue = serializedObject.FindProperty("offsetY").floatValue;
                }
            }
            EditorGUILayout.EndHorizontal(); //offsetY

            EditorGUILayout.BeginHorizontal(); //dist
            EditorGUILayout.Slider(serializedObject.FindProperty("dist"), 5f, 20f, new GUIContent("距離"));
            if (GUILayout.Button("一括設定"))
            {
                for (int i = 0; i < size; i++)
                {
                    cameraWayPoint.GetArrayElementAtIndex(i).FindPropertyRelative("dist").floatValue = serializedObject.FindProperty("dist").floatValue;
                }
            }
            EditorGUILayout.EndHorizontal(); //dist

            EditorGUILayout.BeginHorizontal(); //lookat
            EditorGUILayout.PropertyField(serializedObject.FindProperty("lookAt"), new GUIContent("注視点"));
            if (GUILayout.Button("一括設定"))
            {
                for (int i = 0; i < size; i++)
                {
                    cameraWayPoint.GetArrayElementAtIndex(i).FindPropertyRelative("lookAt").objectReferenceValue = serializedObject.FindProperty("lookAt").objectReferenceValue;
                }
            }
            EditorGUILayout.EndHorizontal(); //lookat

            EditorGUILayout.BeginHorizontal(); //lookOffset
            EditorGUILayout.PropertyField(serializedObject.FindProperty("lookOffset"), new GUIContent("注視点オフセット"));
            if (GUILayout.Button("一括設定"))
            {
                for (int i = 0; i < size; i++)
                {
                    cameraWayPoint.GetArrayElementAtIndex(i).FindPropertyRelative("lookOffset").vector3Value = serializedObject.FindProperty("lookOffset").vector3Value;
                }
            }
            EditorGUILayout.EndHorizontal(); //lookOffset
            EditorGUILayout.EndVertical(); //Box
        }
        EditorGUILayout.Separator();
    }

    /*! @brief プレイヤーパスのポイント間のカメラ設定*/
    void CameraInfoSetting()
    {
        isWaypointFoldout = EditorGUILayout.Foldout(isWaypointFoldout, "ポイント設定", true);
        if (isWaypointFoldout)
        {
            EditorGUILayout.BeginVertical("Box"); //Box1
            int size = cameraWayPoint.arraySize;

            EditorGUILayout.BeginVertical(GUI.skin.box); //skinBox
            scrollSize = EditorGUILayout.IntSlider("スクロールサイズ", scrollSize, 100, 500);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(scrollSize));
            {
                for (int i = 0; i < size; i++)
                {
                    EditorGUILayout.BeginVertical("Box"); //Box2
                    EditorGUILayout.BeginHorizontal();
                    bool view = cameraWayPoint.GetArrayElementAtIndex(i).FindPropertyRelative("inspectorView").boolValue;
                    cameraWayPoint.GetArrayElementAtIndex(i).FindPropertyRelative("inspectorView").boolValue = EditorGUILayout.Foldout(view, "#" + (i + 1), true);
                    if (GUILayout.Button("削除", GUILayout.Width(60)))
                    {
                        DeletePoint(i);
                    }
                    EditorGUILayout.EndHorizontal();
                    if (cameraWayPoint.GetArrayElementAtIndex(i).FindPropertyRelative("inspectorView").boolValue)
                    {
                        EditorGUILayout.PropertyField(cameraWayPoint.GetArrayElementAtIndex(i).FindPropertyRelative("currentPos"), new GUIContent("パス上の位置"));
                        EditorGUILayout.Slider(cameraWayPoint.GetArrayElementAtIndex(i).FindPropertyRelative("offsetY"), 0f, 20f, new GUIContent("Y軸オフセット"));
                        EditorGUILayout.Slider(cameraWayPoint.GetArrayElementAtIndex(i).FindPropertyRelative("dist"), 0f, 20f, new GUIContent("距離"));
                        EditorGUILayout.PropertyField(cameraWayPoint.GetArrayElementAtIndex(i).FindPropertyRelative("cameraVec"), new GUIContent("カメラのある方向"));
                        EditorGUILayout.PropertyField(cameraWayPoint.GetArrayElementAtIndex(i).FindPropertyRelative("lookAt"), new GUIContent("注視点"));
                        EditorGUILayout.PropertyField(cameraWayPoint.GetArrayElementAtIndex(i).FindPropertyRelative("lookOffset"), new GUIContent("注視点オフセット"));
                    }
                    EditorGUILayout.EndVertical(); //Box2
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical(); //skinBox

            CameraPointSetting();
            EditorGUILayout.EndVertical();//Box1
        }
        EditorGUILayout.Separator();
    }

    /*! @brief ポイント設定*/
    void CameraPointSetting()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(new GUIContent("追加", "現在の位置にポイントを追加します。"), EditorStyles.miniButtonLeft, GUILayout.Width(60)))
        {
            if (cameraWayPoint.arraySize == 0)
            {
                t.cameraWaypoints = new List<CameraWaypoint>();
            }
            AddPoint();
        }
        if (GUILayout.Button(new GUIContent("ソート", "リストを昇順に整理します。"), EditorStyles.miniButtonRight, GUILayout.Width(60)))
        {
            t.cameraWaypoints.Sort(CompareByFloat);
        }
        EditorGUILayout.EndHorizontal();
    }

    /*! @brief float型昇順ソート*/
    private static int CompareByFloat(CameraWaypoint a, CameraWaypoint b)
    {
        if (a.currentPos > b.currentPos)
            return 1;
        else if (a.currentPos < b.currentPos)
            return -1;
        else
            return 0;
    }

    /*! @brief ポイントの追加*/
    private void AddPoint()
    {
        CameraWaypoint item = new CameraWaypoint();
        item.currentPos = selectPos;
        item.position = t.transform.position;
        item.rotation = t.transform.rotation;
        item.lookAt = t.player.transform;
        t.cameraWaypoints.Add(item);
    }

    /*! @brief ポイントの削除*/
    private void DeletePoint(int index)
    {
        t.cameraWaypoints.RemoveAt(index);
        if (cameraWayPoint.arraySize == 0)
            t.cameraWaypoints.Clear();
    }

    /*! @brief プレビュー*/
    void Preview()
    {
        isPreviewFoldout = EditorGUILayout.Foldout(isPreviewFoldout, "プレビュー", true);
        if (isPreviewFoldout)
        {
            EditorGUILayout.BeginVertical("Box"); //Box
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 70;
            t.playerPath.currentPos = EditorGUILayout.Slider("座標", t.playerPath.currentPos, 0f, 1f);
            selectPos = t.playerPath.currentPos;
            EditorGUIUtility.labelWidth = 120;
            for (int i = 0; i < targets.Length; i++)
            {
                if (!t.playerPath.updateTransform && t.playerPath.target != null)
                {
                    PlayerPath pmo = t.playerPath;
                    Vector3 position = Vector3.zero;
                    Quaternion rotation = Quaternion.identity;
                    float velocity = 0f;
                    int waypoint = 0;
                    pmo.GetSampledWayPoint(pmo.currentPos, out position, out velocity, out waypoint);
                    pmo.UpdateTarget(position, velocity);
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical(); //Box
        }
        EditorGUILayout.Separator();
    }

}