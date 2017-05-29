using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System;

[Serializable]
//ポイント情報
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

    public float offsetY;
    public float distance;

    public Vector3 position;                                  //ポイント座標
    public Vector3 rotation;                                  //オイラー角での回転
    public Transform lookAt;                                //そのポイントにおける注視点

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

    #region ゲッターとセッター
    //ポイントの取得と設定
    public Vector3 Position
    {
        get { return position; }
        set { position = value; }
    }

    //回転の取得と設定
    public Vector3 Rotation
    {
        get { return rotation; }
        set { rotation = value; }
    }

    //注視点設定
    public Transform LookAt
    {
        get { return lookAt; }
        set { lookAt = value; }
    }

    //入力ベジェの接線取得と設定
    public Vector3 InTangent
    {
        get { return inTangent; }
        set
        {
            inTangent = value;
            if (symmetricTangents)
                outTangent = -inTangent;
        }
    }

    //出力ベジェの接線の取得と設定
    public Vector3 OutTangent
    {
        get { return outTangent; }
        set
        {
            outTangent = value;
            if (symmetricTangents)
                inTangent = -outTangent;
        }
    }

    //入出力ベジェの接線の対称フラグの取得と設定
    public bool SymmetricTangents
    {
        get { return symmetricTangents; }
        set { symmetricTangents = value; }
    }

    //入りの速度タイプの取得と設定
    public VelocityVariation InVariation
    {
        get { return inVariation; }
        set { inVariation = value; }
    }

    //出る時の速度タイプの取得と設定
    public VelocityVariation OutVariation
    {
        get { return outVariation; }
        set { outVariation = value; }
    }

    //速度の取得と設定
    public float Velocity
    {
        get { return velocity; }
        set { velocity = value; }
    }

    //ポイント到達時のイベントの取得と設定
    public ReachedEvent Reached
    {
        get { return reached; }
        set { reached = value; }
    }
    #endregion
}
