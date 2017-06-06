/*
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
public class CameraWorkInspector : Editor
{
    SerializedProperty cameraWayPoint;
    CameraWork t;
    private float selectPos;
    private int selectPoint;
    int scrollSize = 5;
    Vector2 scrollPos = Vector3.zero;

    /* @brief アクティブ時初期化*/
    private void OnEnable()
    {
        cameraWayPoint = serializedObject.FindProperty("cameraWaypoints");
    }

    /* @brief インスペクターの表示*/
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        t = target as CameraWork;

        #region 基本設定
        EditorGUILayout.BeginVertical("Box");
        Base();
        EditorGUILayout.EndVertical();
        EditorGUILayout.Separator();
        #endregion

        #region  一括設定
        EditorGUILayout.BeginVertical("Box");
        BulkSetting();
        EditorGUILayout.EndVertical();
        EditorGUILayout.Separator();
        #endregion

        #region ポイントの設定
        EditorGUILayout.BeginVertical("Box");
        CameraInfoSetting();
        CameraPointSetting();
        EditorGUILayout.EndVertical();
        EditorGUILayout.Separator();
        #endregion

        #region プレビュー
        EditorGUILayout.BeginVertical("Box");
        Preview();
        EditorGUILayout.EndVertical();
        EditorGUILayout.Separator();
        #endregion

        EditorUtility.SetDirty(t);    //更新

        serializedObject.ApplyModifiedProperties();
    }

    /* @brief 基本設定*/
    void Base()
    {
        if (!serializedObject.isEditingMultipleObjects)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("target"), new GUIContent("カメラ"));
        }
        EditorGUILayout.PropertyField(serializedObject.FindProperty("player"), new GUIContent("プレイヤー"));
        EditorGUILayout.Slider(serializedObject.FindProperty("zoomOutDist"), 1f, 10f, new GUIContent("ズームアウトでの加算値"));
    }

    /* @brief 一括設定*/
    void BulkSetting()
    {
        int size = cameraWayPoint.arraySize;
        EditorGUIUtility.labelWidth = 100;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.Slider(serializedObject.FindProperty("offsetY"), 1f, 10f, new GUIContent("Y軸オフセット"));
        if (GUILayout.Button("一括設定"))
        {
            for (int i = 0; i < size; i++)
            {
                cameraWayPoint.GetArrayElementAtIndex(i).FindPropertyRelative("offsetY").floatValue = serializedObject.FindProperty("offsetY").floatValue;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.Slider(serializedObject.FindProperty("dist"), 5f, 20f, new GUIContent("距離"));
        if (GUILayout.Button("一括設定"))
        {
            for (int i = 0; i < size; i++)
            {
                cameraWayPoint.GetArrayElementAtIndex(i).FindPropertyRelative("dist").floatValue = serializedObject.FindProperty("dist").floatValue;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("lookAt"), new GUIContent("注視点"));
        if (GUILayout.Button("一括設定"))
        {
            for (int i = 0; i < size; i++)
            {
                cameraWayPoint.GetArrayElementAtIndex(i).FindPropertyRelative("lookAt").objectReferenceValue = serializedObject.FindProperty("lookAt").objectReferenceValue;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("lookOffset"), new GUIContent("注視点オフセット"));
        if (GUILayout.Button("一括設定"))
        {
            for (int i = 0; i < size; i++)
            {
                cameraWayPoint.GetArrayElementAtIndex(i).FindPropertyRelative("lookOffset").vector3Value = serializedObject.FindProperty("lookOffset").vector3Value;
            }
        }
        EditorGUILayout.EndHorizontal();

    }

    /* @brief プレイヤーパスのポイント間のカメラ設定*/
    void CameraInfoSetting()
    {
        //ポイントが無かったら情報をセットしない
        int size = cameraWayPoint.arraySize;
        if (size == 0) return;
        
        //scrollPos = EditorGUILayout.BeginScrollView(scrollPos,"Box");
        {
            for (int i = 0; i < size; i++)
            {
                EditorGUILayout.BeginVertical("Box");
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
                EditorGUILayout.EndVertical();
            }
        }
        //EditorGUILayout.EndScrollView();
    }

    /* @brief ポイント設定*/
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

    /* @brief float型昇順ソート*/
    private static int CompareByFloat(CameraWaypoint a, CameraWaypoint b)
    {
        if (a.currentPos > b.currentPos)
            return 1;
        else if (a.currentPos < b.currentPos)
            return -1;
        else
            return 0;
    }

    /* @brief ポイントの追加*/
    private void AddPoint()
    {
        CameraWaypoint item = new CameraWaypoint();
        item.currentPos = selectPos;
        item.lookAt = t.player.transform;
        t.cameraWaypoints.Add(item);
    }

    /* @brief ポイントの削除*/
    private void DeletePoint(int index)
    {
        t.cameraWaypoints.RemoveAt(index);
        if (cameraWayPoint.arraySize == 0)
            t.cameraWaypoints.Clear();
    }

    /* @brief プレビュー*/
    void Preview()
    {
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
    }

}