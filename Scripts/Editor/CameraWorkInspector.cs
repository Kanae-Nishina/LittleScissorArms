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

//カメラワークのエディタ拡張
[CustomEditor(typeof(CameraWork))]
[CanEditMultipleObjects]
public class CameraWorkInspector : Editor
{  
    private float offsetY = 0f;
    private float dist = 17.5f;
    private Transform lookat = null;
    private Vector3 lookatOffset = Vector3.zero;
    

    //アクティブ時のイベント
    void OnEnable()
    {
    }

    //インスペクターの表示
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUIUtility.labelWidth = 120;
        var t = target as CameraWork;

        #region 基本設定
        EditorGUILayout.BeginVertical("Box");
        Base(t);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Separator();
        #endregion

        #region  一括設定
        EditorGUILayout.BeginVertical("Box");
        BulkSetting(t);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Separator();
        #endregion 

        #region 個々の設定
        EditorGUILayout.BeginVertical("Box");
        GetPlayerPathPoint(t);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Separator();
        #endregion
        EditorUtility.SetDirty(t); //インスペクター上のGUI変更メソッド

    }

    //基本設定
    void Base(CameraWork t)
    {
        if (!serializedObject.isEditingMultipleObjects)
            t.target = (Transform)EditorGUILayout.ObjectField("カメラ", t.target, typeof(Transform), true);
        t.player = (MainCharacterController)EditorGUILayout.ObjectField("プレイヤー", t.player, typeof(MainCharacterController), true);
        EditorGUIUtility.labelWidth = 90;

        EditorGUILayout.LabelField("ズームアウトでの加算値");
        t.zoomOutDist = EditorGUILayout.Slider("", t.zoomOutDist, 1f, 10f);

    }

    //一括設定
    void BulkSetting(CameraWork t)
    {
        EditorGUILayout.BeginHorizontal();
        offsetY = EditorGUILayout.Slider("Y軸オフセット", offsetY, 0f, 10f);
        if(GUILayout.Button("一括設定"))
        {
            for(int i=0;i<t.playerPath.waypoints.Length;i++)
            {
                t.playerPath.waypoints[i].offsetY = offsetY;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        dist = EditorGUILayout.Slider("距離", dist, 5f, 20f);
        if (GUILayout.Button("一括設定"))
        {
            for (int i = 0; i < t.playerPath.waypoints.Length; i++)
            {
                t.playerPath.waypoints[i].dist = dist;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        lookat = (Transform)EditorGUILayout.ObjectField("注視点", lookat, typeof(Transform), true);
        if (GUILayout.Button("一括設定"))
        {
            for (int i = 0; i < t.playerPath.waypoints.Length; i++)
            {
                t.playerPath.waypoints[i].lookAt = lookat;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        lookatOffset = EditorGUILayout.Vector3Field("注視点オフセット", lookatOffset);
        if (GUILayout.Button("一括設定"))
        {
            for (int i = 0; i < t.playerPath.waypoints.Length; i++)
            {
                t.playerPath.waypoints[i].lookOffset = lookatOffset;
            }
        }
        EditorGUILayout.EndHorizontal();

    }

    //プレイヤーパスのポイント情報取得
    void GetPlayerPathPoint(CameraWork t)
    {
        int amount = t.playerPath.waypoints.Length;
        if (amount == 0) return;

        //EditorGUILayout.PropertyField(prop, true);
        for (int i = 0; i < amount; i++)
        {
            EditorGUILayout.BeginVertical("Box");
            t.playerPath.waypoints[i].inspectorView = EditorGUILayout.Foldout(t.playerPath.waypoints[i].inspectorView, "#" + (i + 1), true);
            if (t.playerPath.waypoints[i].inspectorView)
            {
                t.playerPath.waypoints[i].offsetY = EditorGUILayout.Slider("Y軸オフセット", t.playerPath.waypoints[i].offsetY, 0f, 20f);
                t.playerPath.waypoints[i].dist = EditorGUILayout.Slider("距離", t.playerPath.waypoints[i].dist, 0f, 20f);
                t.playerPath.waypoints[i].lookAt = (Transform)EditorGUILayout.ObjectField("注視点", t.playerPath.waypoints[i].lookAt, typeof(Transform), true);
                t.playerPath.waypoints[i].lookOffset = EditorGUILayout.Vector3Field("注視点オフセット", t.playerPath.waypoints[i].lookOffset);
            }
            EditorGUILayout.EndVertical();
        }
    }
}


