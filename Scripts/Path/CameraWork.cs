/*!
 * @file CameraWork.cs
 * @brief カメラワーク処理
 * @date 2017/04/19
 * @author 仁科香苗
 */
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;

[ExecuteInEditMode]
[InitializeOnLoad]
#endif

/*! @brief カメラワーククラス*/
public class CameraWork : MonoBehaviour
{
    public PlayerPath playerPath;                                              /*! プレイヤーの移動パス*/
    public MainCharacterController player;                            /*! プレイヤー管理クラス*/
    public SubCharacterController subPlayer;                       /*! サブプレイヤー管理暮らし*/
    public Transform target;                                                        /*! パスに沿わせる対象のトランスフォーム*/
    public List<CameraWaypoint> cameraWaypoints;        /*! カメラの方向リスト*/
    public float zoomOutDist = 5f;                                             /*! ズームアウトの距離*/
    public float speed = 0.1f;                                                         /*! 移動速度(0~1)*/
    public float offsetY = 1f;                                                           /*! Y軸のオフセット*/
    public float dist = 5f;                                                                 /*! 距離*/
    public Transform lookAt;                                                        /*! 注視対象*/
    public Vector3 lookOffset;                                                      /*! 注視点のオフセット*/

    private CameraWaypoint point;                                           /*! カメラのポイント*/

    /*! @brief アクティブ時の初期化*/
    void OnEnable()
    {
        playerPath = GameObject.Find("PlayerPath").GetComponent<PlayerPath>();
#if UNITY_EDITOR
        if (!Application.isPlaying)
            EditorApplication.update += FixedUpdate;
#endif
    }

    /*! @brief 非アクティブ時の処理*/
    void OnDisable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            EditorApplication.update -= FixedUpdate;
#endif
    }

    /*! @brief 更新*/
    void FixedUpdate()
    {
        UpdateTarget();
    }

    #region     更新関係
    /*! @brief 移動更新*/
    public void UpdateTarget()
    {
        point = CameraDirection(playerPath.currentPos);
        if (!player.GetIsPendulum())
        {
            NormalMove(point);
        }
        else
        {
            PendulumLookAtPosition(point);
        }
    }

    /*! @brief 通常移動*/
    void NormalMove(CameraWaypoint point)
    {
        Vector3 newPos = target.position;
        Vector3 lookat = Vector3.zero;

        if (cameraWaypoints.Count == 0)
            return;

        Vector3 zoom = Vector3.zero;
        if (player.isSublayerCarry)
        {
            zoom = CameraZoomOut(playerPath.target.position);
        }

        newPos = (point.lookAt.position + point.cameraVec * point.dist) + zoom;
        newPos.y += point.offsetY;
        target.position = Vector3.Lerp(target.position, newPos, 0.1f);
        lookat = point.lookAt.position + point.lookOffset;
        target.LookAt(lookat);
    }

    /*! @brief ズームアウト*/
    Vector3 CameraZoomOut(Vector3 pos)
    {
        Vector3 pp = pos;
        Vector3 dir = Vector3.Normalize(target.position - pp);

        return (dir * zoomOutDist);
    }

    /*! @brief 振り子状態の時座標*/
    void PendulumLookAtPosition(CameraWaypoint point)
    {
        if (SceneManager.GetActiveScene().name == "Title") return;　　//タイトルでは移動しない
        Vector3 lookat = Vector3.zero;
        Vector3 zoom = Vector3.zero;
        Vector3 newPos = player.GetFulcrumPosition();
        newPos.y -= player.GetRadius() / 2;
        lookat = newPos;
        newPos +=point.cameraVec*point.dist;
        
        target.position = Vector3.Lerp(target.position, newPos, 0.05f);
        target.LookAt(lookat);
    }

    /*! @brief プレイヤーの位置によるカメラのポイント情報*/
    CameraWaypoint CameraDirection(float pos)
    {
        if (cameraWaypoints.Count == 1)
        {
            return cameraWaypoints[0];
        }
        for (int i = 1; i < cameraWaypoints.Count; i++)
        {
            if ((cameraWaypoints[i - 1].currentPos <= pos) && (cameraWaypoints[i].currentPos > pos))
            {
                return cameraWaypoints[i - 1];
            }
        }
        return cameraWaypoints[cameraWaypoints.Count - 1];
    }

    /*! @brief 現在のカメラのある方向ベクトル取得*/
    public Vector3 GetCameraVec()
    {
        if(point==null)
        {
            return Vector3.zero;
        }
        return point.cameraVec;
    }
    #endregion
}