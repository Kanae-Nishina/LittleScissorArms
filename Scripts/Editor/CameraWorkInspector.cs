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
    //一括設定の為の一時保存用
    private float offsetY = 0f;                                             //Y軸オフセット
    private float dist = 17.5f;                                               //注視点との距離
    private Transform lookat = null;                                //注視対象
    private Vector3 lookatOffset = Vector3.zero;       //注視点とのオフセット

    /* @brief インスペクターの表示*/
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
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
        CameraInfoSetting(t);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Separator();
        #endregion

        #region プレビュー
        EditorGUILayout.BeginVertical("Box");
       // Preview(t);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Separator();
        #endregion

        EditorUtility.SetDirty(t);    //更新
    }


    /* @brief 基本設定*/
    void Base(CameraWork t)
    {
        if (!serializedObject.isEditingMultipleObjects)
        {
            t.target = (Transform)EditorGUILayout.ObjectField("カメラ", t.target, typeof(Transform), true);
        }
        t.player = (MainCharacterController)EditorGUILayout.ObjectField("プレイヤー", t.player, typeof(MainCharacterController), true);
        EditorGUIUtility.labelWidth = 90;
        EditorGUILayout.LabelField("ズームアウトでの加算値");
        t.zoomOutDist = EditorGUILayout.Slider("", t.zoomOutDist, 1f, 10f);
    }

    /* @brief 一括設定*/
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

    /* @brief プレイヤーパスのポイント間のカメラ設定*/
    void CameraInfoSetting(CameraWork t)
    {
        //プレイヤーパスのポイントが無かったら情報をセットしない
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
                t.playerPath.waypoints[i].cameraVec= EditorGUILayout.Vector3Field("カメラのある方向", t.playerPath.waypoints[i].cameraVec);
                t.playerPath.waypoints[i].lookAt = (Transform)EditorGUILayout.ObjectField("注視点", t.playerPath.waypoints[i].lookAt, typeof(Transform), true);
                t.playerPath.waypoints[i].lookOffset = EditorGUILayout.Vector3Field("注視点オフセット", t.playerPath.waypoints[i].lookOffset);
            }
            EditorGUILayout.EndVertical();
        }
    }

    /* @brief ポイント設定*/
    void CameraPointSetting(CameraWork t)
    {

    }

    /* @brief プレビュー*/
    void Preview(CameraWork t)
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUIUtility.labelWidth = 70;
        t.playerPath.currentPos=EditorGUILayout.Slider("座標",t.playerPath.currentPos, 0f, 1f);
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
                pmo.sampledPositionAndVelocityAndWaypointAtPos(pmo.currentPos, out position, out velocity, out waypoint);
                pmo.UpdateTarget(position, velocity);
            }
        }

        EditorGUILayout.EndHorizontal();
    }
  
}