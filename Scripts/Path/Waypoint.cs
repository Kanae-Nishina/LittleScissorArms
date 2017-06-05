/*
 * @file Waypoint.cs
 * @brief ポイント情報クラス
 * @date 2017/05/25
 * @author 仁科香苗
 * @note 参考:PlayerPath(https://www.assetstore.unity3d.com/jp/#!/content/47769)
 */
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System;

[Serializable]
/* @brief ポイント情報*/
public class Waypoint
{
    [Serializable]
    //ポイント到達時のイベント
    public class ReachedEvent : UnityEvent { }

    //速度変化の際の補間タイプ
    public enum VelocityVariation
    {
        Slow,
        Medium,
        Fast
    }
    public bool inspectorView = false;                   //インスペクターに描画するかどうか
    public float offsetY = 0f;                                   //Y軸移動のオフセット
    public float dist = 17.5f;                                     //距離
    public Vector3 lookOffset;                              //注視のオフセット
    public Vector3 cameraVec;                               //カメラのある方向
    public Transform lookAt;                                //そのポイントにおける注視点


    public Vector3 position;                                  //ポイント座標
    public Vector3 rotation;                                  //オイラー角での回転

    public Vector3 inTangent;                               //入力ベジェの接線
    public Vector3 outTangent;                           //出力ベジェの接線
    public bool symmetricTangents;                  //入出力ベジェの接線の対称フラグ

    public float velocity;                                         //速度
    public VelocityVariation inVariation;            //入りの速度のタイプ
    public VelocityVariation outVariation;         //出る時の速度タイプ

    public ReachedEvent reached;                      //ポイント到達時のイベント

    //初期化
    public Waypoint()
    {
        position = Vector3.zero;
        rotation = Vector3.zero;
        velocity = 1f;
        outTangent = Vector3.forward;
        inTangent = -Vector3.forward;
        symmetricTangents = true;
        inVariation = VelocityVariation.Medium;
        outVariation = VelocityVariation.Medium;
        reached = null;
    }
}

/* @brief カメラポイント情報*/
[Serializable]
public class CameraWaypoint
{
    public bool inspectorView;                   //インスペクターに描画するかどうか
    public float currentPos;
    public float offsetY;                                   //Y軸移動のオフセット
    public float dist;                                     //距離
    public Vector3 lookOffset;                              //注視のオフセット
    public Vector3 cameraVec;                               //カメラのある方向
    public Transform lookAt;                                //そのポイントにおける注視点

    /* @brief 初期化*/
    public CameraWaypoint()
    {
        inspectorView = false;
        offsetY = 0f;
        dist = 17.5f;
        lookOffset = Vector3.zero;
        cameraVec = Vector3.zero;
        lookAt = null;
    }
}
