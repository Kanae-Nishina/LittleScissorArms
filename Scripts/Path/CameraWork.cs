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

#if UNITY_EDITOR
using UnityEditor;

[ExecuteInEditMode]
[InitializeOnLoad]
#endif

/*! @brief カメラワーククラス*/
public class CameraWork : MonoBehaviour
{
    public PlayerPath playerPath;                                              //プレイヤーの移動
    public MainCharacterController player;                            //プレイヤー
    public SubCharacterController subPlayer;                        //サブプレイヤー
    public Transform target;                                                        //パスに沿わせる対象のトランスフォーム
    public List<CameraWaypoint> cameraWaypoints;

    private float preInput = 1f;
    private Vector3 prePosition;

    
    public float zoomOutDist = 5f;
    public float speed = 0.1f;
    public float offsetY = 1f;
    public float dist = 5f;
    public Transform lookAt;
    public Vector3 lookOffset;

    //アクティブ時の初期化
    void OnEnable()
    {
        playerPath = GameObject.Find("PlayerPath").GetComponent<PlayerPath>();
#if UNITY_EDITOR
        if (!Application.isPlaying)
            EditorApplication.update += FixedUpdate;
#endif
    }

    //非アクティブ時の処理
    void OnDisable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            EditorApplication.update -= FixedUpdate;
#endif
    }

    //更新前初期化
    void Start()
    {
    }

    //更新
    void FixedUpdate()
    {
        UpdateTarget();
    }

    #region     更新関係

    //パスに沿う対象の更新
    public void UpdateTarget()
    {
        Vector3 newPos = target.position;
        Vector3 lookat = Vector3.zero;
        int currenspos = playerPath.GetCurrentWaypoint();

        //Waypoint point = playerPath.waypoints[currenspos];
        if (cameraWaypoints.Count == 0)
            return;

        CameraWaypoint point = CameraDirection(playerPath.currentPos);
        // if (!player.GetIsPendulum())
        {
            float dir = playerPath.GetInputOnly();
            if (dir == 0)
                dir = preInput;
            else
                preInput = dir;

            //newPos = globalLookAt.position + playerPath.waypoints[currenspos].moveOffset;
            if (player.isCarry)
            {
                newPos += CameraZoomOut(playerPath.target.position);
            }

            newPos = point.lookAt.position + point.cameraVec * point.dist;
            //newPos = point.lookAt.position + point.lookAt.right * dir * point.dist;
            newPos.y += point.offsetY;
            lookat = point.lookAt.position + point.lookOffset;
        }
        // else
        //{
        //lookat = PendulumLookAtPosition(player.GetFulcrumPosition(), player.GetRadius());
        //newPos = lookat;
        //     newPos = PendulumLookAtPosition(player.GetFulcrumPosition(), player.GetRadius());
        //newPos += Vector3.back * point.dist;
        //lookat = target.position + Vector3.forward;
        //}
        prePosition = transform.position;
        target.position = Vector3.Lerp(target.position, newPos, 0.1f);
        target.LookAt(lookat);
        //target.LookAt(globalLookAt.position + playerPath.waypoints[currenspos].lookOffset);
        //target.rotation = newRot;
    }

    /*! @brief ズームアウト*/
    Vector3 CameraZoomOut(Vector3 pos)
    {
        Vector3 pp = pos;
        Vector3 dir = Vector3.Normalize(target.position - pp);

        return (dir * zoomOutDist);
    }

    /*! @brief 振り子状態の時座標*/
    Vector3 PendulumLookAtPosition(Vector3 fulcrumPos, float radius)
    {
        Vector3 newPos = fulcrumPos;
        newPos.y -= radius;
        return newPos;
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
    #endregion


}