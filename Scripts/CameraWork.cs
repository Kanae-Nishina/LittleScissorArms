/*
 * @file CameraWork.cs
 * @brief カメラワーク処理
 * @date 2017/04/19
 * @author 仁科香苗
 */
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/* @brief ハンドル情報*/
[System.Serializable]
public class CameraHandlePoint
{
    public Vector3 position;                        //座標
    public Quaternion rotation;                 //回転
    public Vector3 handlePrev;                  //前ハンドル方向
    public Vector3 handleNext;                  //次ハンドル方向
    public bool chained;                                //パス曲線のハンドルが対照かどうか
    public bool showPoints;                         //インスペクターに情報を描画するかどうか
    public float nextPerSecond;                  //速度
    public float nextDistance;                      //次のハンドルまでの距離   

    //コンストラクタ
    public CameraHandlePoint(Vector3 pos, Quaternion rot, float perSecond)
    {
        position = pos;
        rotation = rot;
        handlePrev = Vector3.back;
        handleNext = Vector3.forward;
        chained = true;
        showPoints = false;
        perSecond = 0f;
        nextDistance = 0f;
        nextPerSecond = perSecond;
    }
    public CameraHandlePoint(Vector3 pos, Quaternion rot, Vector3 prev, Vector3 next, float time, float dist)
    {
        position = pos;
        rotation = rot;
        handlePrev = prev;
        handleNext = next;
        nextDistance = dist;
        nextPerSecond = time;
    }
}

/* @brief カメラワーククラス*/
public class CameraWork : MonoBehaviour
{
    public bool useMainCamera = true;                                                                               //初期設定のMainCameraを使用するかどうか
    public Camera selectedCamera;                                                                                            //使用するカメラを任意に設定
    public bool isLookAtTarget = false;                                                                               //注視点設定があるかどうか
    public Transform lookAtTarget;                                                                                      //注視点
    public Vector3 lookAtOffset;                                                                                            //注視点のオフセット
    public List<CameraHandlePoint> points = new List<CameraHandlePoint>();     //ハンドルリスト
    public PathVisual visual;                                                                                                       //パスの色
    public bool alwaysShow = true;                                                                                          //パスを可視化するかどうか
    public bool perSecond_unification;                                                                                  //全てのハンドル間で速度統一するかどうか
    public float perSecond;                                                                                                         //速度
    public bool samePlayerPerSecond;                                                                                    //プレイヤーと同じ速度にするかどうか
    public int firstWayPoint;                                                                                                       //最初の地点

    PlayerPath playerPath;
    private int currentWaypointIndex;               //現在のハンドル
    private float currentTimeInWaypoint;         //補完する時間
    private float allDistance;                                  //総距離
    private Transform playerTransform;            //プレイヤーのトランスフォーム


    /* @brief 更新前初期化*/
    void Start()
    {
        playerPath = GameObject.Find("PlayerTrajectory").GetComponent<PlayerPath>();

        //移動速度設定
        if (samePlayerPerSecond) perSecond = playerPath.perSecond;
        SetAllPerSecond();
        OutputPathInfomation();
        InputPathInfomation();
        if (points.Count > playerPath.points.Count) Debug.LogError("CameraPath Point count more than PlayerPath Point Count");

        //カメラの設定
        if (Camera.main == null || (!useMainCamera & selectedCamera == null)) Debug.LogError("Not Camera!");
        if (useMainCamera || selectedCamera == null) selectedCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
        if (isLookAtTarget && lookAtTarget == null)
        {
            isLookAtTarget = false;
            Debug.LogError("Not LookAtTarget!");
        }

        //再生
        MovePath();
    }

    /* @brief パス情報の出力 */
    void OutputPathInfomation()
    {
        string pathName = Application.dataPath + "/ExternalData/" + SceneManager.GetActiveScene().name + "_Camera.txt";
        StreamWriter sw = new StreamWriter(pathName, false);
        int count = 0;
        //sw.WriteLine(SceneManager.GetActiveScene().name);
        foreach (var p in points)
        {
            sw.WriteLine("#" + count + "#");
            sw.WriteLine(p.position);
            sw.WriteLine(p.rotation);
            sw.WriteLine(p.handlePrev);
            sw.WriteLine(p.handleNext);
            sw.WriteLine(p.nextDistance);
            sw.WriteLine(p.nextPerSecond);
            ++count;
        }
        sw.Flush();
        sw.Close();
    }

    /* @brief パス情報の読み込み */
    void InputPathInfomation()
    {
        string pathName = Application.dataPath + "/ExternalData/" + SceneManager.GetActiveScene().name + "_Camera.txt";
        StreamReader sr = new StreamReader(pathName);
        points.Clear();
        string line;
        while ((line = sr.ReadLine()) != null)
        {
            Vector3 pos = playerPath.StringToVector3(sr.ReadLine());
            Quaternion rot = playerPath.StringToQuaternion(sr.ReadLine());
            Vector3 prev = playerPath.StringToVector3(sr.ReadLine());
            Vector3 next = playerPath.StringToVector3(sr.ReadLine());
            float dist = float.Parse(sr.ReadLine());
            float time = float.Parse(sr.ReadLine());
            CameraHandlePoint point = new CameraHandlePoint(pos, rot, prev, next, time, dist);
            points.Add(point);
        }
    }

    /* @brief コルーチン再生*/
    void MovePath()
    {
        StopAllCoroutines();
        StartCoroutine(FollowPath());
    }

    /* @brief パスに沿って移動*/
    IEnumerator FollowPath()
    {
        //開始地点の設定
        currentWaypointIndex = firstWayPoint;
        while (currentWaypointIndex < points.Count)
        {
            //補間値初期化
            if (currentTimeInWaypoint >= 0f)
                currentTimeInWaypoint = 0f;
            else
                currentTimeInWaypoint = 1f + currentTimeInWaypoint;
            float timePerSegment = points[currentWaypointIndex].nextPerSecond;
            while (currentTimeInWaypoint < 1f && currentTimeInWaypoint >= 0f)
            {
                //補間値設定
                float input = 0f;
                if (playerPath.GetAddPotision() != Vector3.zero)
                {
                    input = playerPath.GetInput();
                }

                currentTimeInWaypoint += (Time.deltaTime / timePerSegment) * input;
                Vector3 newPos = GetBezierPosition(currentWaypointIndex, currentTimeInWaypoint);
                selectedCamera.transform.position = newPos;

                //カメラの回転更新
                Quaternion rot;
                if (isLookAtTarget)
                    rot = Quaternion.LookRotation(((lookAtTarget.transform.position + lookAtOffset) - selectedCamera.transform.position).normalized);
                else
                    rot = Quaternion.LookRotation(Vector3.forward);
                selectedCamera.transform.rotation = rot;
                yield return 0;
            }

            //ハンドル更新
            if (currentTimeInWaypoint >= 1f)
                ++currentWaypointIndex;
            else if (currentTimeInWaypoint < 0f)
                --currentWaypointIndex;
            //最終ハンドルに到達
            if (currentWaypointIndex == points.Count)
                currentWaypointIndex = points.Count - 1;
            else if (currentWaypointIndex == -1)
            {
                currentWaypointIndex = 0;
                currentTimeInWaypoint = 0f;
            }
        }
        //コルーチン停止
        StopAllCoroutines();
    }

    /* @brief 全体の移動速度統一設定*/
    void SetAllPerSecond()
    {
        int pointIndex = 0;
        float pointTime = 0f;
        Vector3 pos = points[pointIndex].position;
        float timePerSegment = 1f / points.Count;

        while (pointIndex < points.Count)
        {
            float dist = 0f;
            pointTime = 0f;
            while (pointTime < 1f && pointTime >= 0f)
            {
                pointTime += Time.deltaTime / timePerSegment;
                Vector3 newPos = GetBezierPosition(pointIndex, pointTime);
                dist += Vector3.Distance(pos, newPos);
                pos = newPos;
            }
            points[pointIndex].nextDistance = dist;
            points[pointIndex].nextPerSecond = dist / (samePlayerPerSecond ? perSecond : points[pointIndex].nextPerSecond);
            ++pointIndex;
            if (pointIndex == points.Count) break;
        }
    }

#if true
    /* @brief ベジエ曲線状の座標取得*/
    Vector3 GetBezierPosition(int pointIndex, float time)
    {
        int nextIndex = GetNextIndex(pointIndex);
        Vector3 currentNext = points[pointIndex].position + points[pointIndex].handleNext;
        Vector3 nextPrev = points[nextIndex].position + points[nextIndex].handlePrev;
        Vector3 newPos =
            Vector3.Lerp(
                Vector3.Lerp(
                    Vector3.Lerp(points[pointIndex].position, currentNext, time),
                    Vector3.Lerp(currentNext, nextPrev, time), time),
                Vector3.Lerp(
                    Vector3.Lerp(currentNext, nextPrev, time),
                    Vector3.Lerp(nextPrev, points[nextIndex].position, time), time), time);

        return newPos;
    }

    /* @brief 次のハンドル取得*/
    int GetNextIndex(int index)
    {
        if (index == points.Count - 1)
            return 0;
        return ++index;
    }
#endif 

#if UNITY_EDITOR
    /* @brief シーンビュー上にパス描画*/
    public void OnDrawGizmos()
    {
        //描画対象があるとき又は常に描画するとき
        if (UnityEditor.Selection.activeGameObject == gameObject || alwaysShow)
        {
            //ハンドルの数が2つ以上のときのみ
            if (points.Count >= 2)
            {
                //ハンドルの数未満描画
                for (int i = 0; i < points.Count; i++)
                {
                    //最終ハンドルまでベジエ曲線を描画
                    if (i < points.Count - 1)
                    {
                        var index = points[i];
                        var indexNext = points[i + 1];
                        UnityEditor.Handles.DrawBezier(index.position, indexNext.position, index.position + index.handleNext,
                            indexNext.position + indexNext.handlePrev, ((UnityEditor.Selection.activeGameObject == gameObject) ? visual.pathColor : visual.inactivePathColor), null, 5);
                    }
#if false
                    else
                    {
                        var index = points[i];
                        var indexNext = points[0];
                        UnityEditor.Handles.DrawBezier(index.position, indexNext.position, index.position + index.handleNext,
                            indexNext.position + indexNext.handlePrev, ((UnityEditor.Selection.activeGameObject == gameObject) ? visual.pathColor : visual.inactivePathColor), null, 5);
                    }
#endif
                }
            }

#if false
            //オブジェクトに隠れてもパスが見えるよう設定
            for (int i = 0; i < points.Count; i++)
            {
                var index = points[i];
                //ハンドルの座標と回転を行列に代入
                Gizmos.matrix = Matrix4x4.TRS(index.position, index.rotation, Vector3.one);
                //行列の初期化
                Gizmos.matrix = Matrix4x4.identity;
            }
#endif
        }
    }
#endif
}