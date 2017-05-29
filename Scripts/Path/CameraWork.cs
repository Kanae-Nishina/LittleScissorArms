/*
 * @file CameraWork.cs
 * @brief カメラワーク処理
 * @date 2017/04/19
 * @author 仁科香苗
 * @note 参考:PathMagic(https://www.assetstore.unity3d.com/jp/#!/content/47769)
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

/* @brief カメラワーククラス*/
public class CameraWork : MonoBehaviour
{
    public PlayerPath playerPath;                                              //プレイヤーの移動
    public MainCharacterController player;                            //プレイヤー
    public Transform target;                                                        //パスに沿わせる対象のトランスフォーム

    private float preInput = 1f;
    public float zoomOutDist = 5f;
    public float speed = 0.1f;

    //アクティブ時の初期化
    void OnEnable()
    {
        playerPath = GameObject.Find("PlayerPath").GetComponent<PlayerPath>();


#if UNITY_EDITOR
        if (!Application.isPlaying)
            EditorApplication.update += LateUpdate;
#endif
    }

    //非アクティブ時の処理
    void OnDisable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            EditorApplication.update -= LateUpdate;
#endif
    }

    //更新前初期化
    void Start()
    {
    }

    //更新
    void LateUpdate()
    {
        UpdateTarget();
    }

    #region     更新関係

    //パスに沿う対象の更新
    public void UpdateTarget()
    {
        Vector3 newPos = Vector3.zero;
        float dir = playerPath.GetInputOnly();
        if (dir == 0)
            dir = preInput;
        else
            preInput = dir;

        int currenspos = playerPath.GetCurrentWaypoint();
        //newPos = globalLookAt.position + playerPath.waypoints[currenspos].moveOffset;
        Waypoint point = playerPath.waypoints[currenspos];
        newPos = point.lookAt.position + player.transform.right * dir * point.dist;
        newPos.y += point.offsetY;
        if (player.isCarry)
        {
            newPos += CameraZoomOut();
        }
        target.position = Vector3.Lerp(target.position, newPos, 0.1f);
        target.LookAt(point.lookAt.position+ point.lookOffset);
        //target.LookAt(globalLookAt.position + playerPath.waypoints[currenspos].lookOffset);
        //target.rotation = newRot;
    }

    //チビキャラ鋏み状態の時、カメラを引く
    Vector3 CameraZoomOut()
    {
        Vector3 pp = playerPath.target.position;
        Vector3 dir = Vector3.Normalize(target.position - pp);

        return (dir * zoomOutDist);
    }

    #endregion

}