/*
 * @file PlayerPath.cs
 * @brief プレイヤーの移動パス
 * @date 2017/04/14
 * @author 仁科香苗
 * @note 参考:PathMagic(https://www.assetstore.unity3d.com/jp/#!/content/47769)
 */
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System;

#if UNITY_EDITOR
using UnityEditor;

[ExecuteInEditMode]
[InitializeOnLoad]
#endif

/* @brief パスの処理 */
public class PlayerPath : MonoBehaviour
{
    [Serializable]
    //ポイント通過時のイベント
    public class WaypointChangedEvent : UnityEvent<int> { }

    public Color pathColor = Color.white;                                //Editor上のパスカラー
    public Waypoint[] waypoints = new Waypoint[] { };       //パスを定義するポイント
    public Transform target;                                                        //パスに沿わせる対象のトランスフォーム

    public bool loop = false;                                                           //パスをループさせるかどうか
    public bool updateTransform = true;                                    //アニメーション中の変換の更新フラグ
    private int _lastPassedWayponint;                                   //イベント管理の為の最終ポイント

    public float speed = 0.1f;                                                      //移動スピード
    public float globalFollowPathBias = 0.001f;                     //パスに沿う移動の偏り(0の方が高い)
    public float velocityBias = .1f;                                                //移動速度の偏り補正
    public float currentPos;                                                          //現在の補間位置(0~1)
    public float currentNextPos = 0f;                                        //次の補間位置
    public float totalDistance = 0;                                             //総距離
    private float _lastVelocity = 1.0f;                                         //最後のアニメーション速度のキャッシュ
    public WaypointChangedEvent waypointChanged;    //最後のポイント通過時のイベント

    //サンプリング。数が多いほど精度が高くなる代わりに、パフォーマンスに影響がある。
    public int samplesNum = 100;                                            //等速移動の為のサンプリング精度(値が高いほど精度は高い)
    public int[] waypointSamples = null;                               //サンプリングにおけるポイント
    public float[] velocitySamples = null;                              //サンプリング移動速度
    public float[] samplesDistances = null;                           //サンプリングにおけるポイント間の距離
    public Vector3[] positionSamples = null;                        //サンプリング座標。
    public Quaternion[] rotationSamples = null;                 //サンプリングの回転

    private float inputX = 0;               //スティック入力
    private float moveMag = 0f;        //移動の大きさ
    private Vector3 addPosition;        //加算される方向ベクトル

    /* @brief アクティブ時の初期化*/
    void OnEnable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            EditorApplication.update += FixedUpdate;
#endif
    }

    /* @brief 非アクティブ時の処理*/
    void OnDisable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            EditorApplication.update -= FixedUpdate;
#endif
    }

    /* @brief 更新前初期化*/
    void Start()
    {
        //サンプリング処理
        UpdatePathSamples();
    }

    /*  @brief 固定更新*/
    void FixedUpdate()
    {
        if (!Application.isPlaying)
        {
            DoUpdate();
        }
    }

    /* @brief 移動ベクトル*/
    public Vector3 GetAddPotision()
    {
        DoUpdate();
        return addPosition;
    }

    /* @brief かかる時間取得*/
    public float GetTimePerSegment()
    {
        return _lastVelocity;
    }

    /* @brief 入力値セット */
    public void SetInput(float input, float mag)
    {
        inputX = input;
        moveMag = mag;
    }

    /* @brief 入力による移動量取得*/
    public float GetInput()
    {
        float input = 0f;
        if (inputX > 0f)
            input = 1f * moveMag;
        else if (inputX < 0f)
            input = -1f * moveMag;
        return input;
    }

    /* @brief ただの入力取得*/
    public int GetInputOnly()
    {
        int input = 0;
        if (inputX > 0) input = 1;
        else if (inputX < 0) input = -1;
        return input;
    }

    /* @brief フレーム単位の更新処理*/
    void DoUpdate()
    {
        //ポイントが無ければ通らない
        if (waypoints.Length == 0)
            return;
        
        //移動状態
        float advance = (speed * velocityBias * _lastVelocity * GetInput());
        currentNextPos = currentPos + advance;
        if (currentNextPos >= 1f)
        {
            if (loop)
            {
                currentNextPos -= 1f;
            }
            else
            {
                currentNextPos = 1f;
            }
        }
        else if (currentNextPos < 0f)
        {
            if (loop)
            {
                currentNextPos += 1f;
            }
            else
            {
                currentNextPos = 0f;
            }
        }
        
        if (updateTransform || Application.isPlaying)
            UpdateTarget();
    }

    /* @brief リスポンの位置*/
    public void Respawn(float pos,float height)
    {
        currentNextPos = pos;
        UpdateTarget();
        target.position = new Vector3(0f, height, 0f);
    }

    #region     更新関係

    /* @brief パスに沿う対象の更新*/
    public void UpdateTarget(Vector3 position, float vel)
    {
        if (target != null)
        {
            if (!Application.isPlaying)
            {
                target.position = transform.TransformPoint(position);
            }

            Vector3 newPos = transform.TransformPoint(position);
            addPosition = newPos - target.position;
            addPosition.y = 0f;
            Vector3 dir = addPosition;
            Vector3 pivot = target.transform.position;
            pivot.y -= target.transform.localScale.y/2;
            Debug.DrawRay(pivot, dir * target.transform.localScale.x, Color.red);
            if (!Physics.Raycast(pivot, dir, target.transform.localScale.x))
            {
                currentPos = currentNextPos;
                _lastVelocity = vel;
            }
            else
            {
                addPosition = Vector3.zero;
            }

        }
    }

    /* @brief 現在のポイント番号における、パスに沿う対象の更新*/
    public void UpdateTarget()
    {
        Vector3 position = Vector3.zero;
        Quaternion rotation = Quaternion.identity;
        float velocity = 1.0f;
        int waypoint = 0;

        sampledPositionAndVelocityAndWaypointAtPos(currentNextPos, out position, out velocity, out waypoint);

        UpdateTarget(position, velocity);

        if (waypoint != _lastPassedWayponint)
        {
            if (waypointChanged != null)
                waypointChanged.Invoke(waypoint);
            if (waypoints[waypoint].reached != null)
                waypoints[waypoint].reached.Invoke();
        }

        _lastPassedWayponint = waypoint;

#if UNITY_EDITOR
        if (target == null)
            SceneView.RepaintAll();
#endif
    }

    #endregion

    /* @brief サンプリングによるポイント情報の取得*/
    public void sampledPositionAndVelocityAndWaypointAtPos(float pos, out Vector3 position, out float velocity, out int waypoint)
    {
        float refDistance = pos * totalDistance;

        float d = 0f;
        for (int i = 1; i < samplesNum; i++)
        {
            d += samplesDistances[i];
            if (d >= refDistance)
            {
                float interpFactor = 1f - (d - refDistance) / samplesDistances[i];
                position = Vector3.Lerp(positionSamples[i - 1], positionSamples[i], interpFactor);

                velocity = Mathf.Lerp(velocitySamples[i - 1], velocitySamples[i], interpFactor);

                if (pos >= 1)
                {
                    if (loop)
                        waypoint = 0;
                    else
                        waypoint = waypoints.Length - 1;
                }
                else
                {

                    waypoint = waypointSamples[i - 1];
                }

                return;
            }
        }

        position = positionSamples[samplesNum - 1];
        velocity = velocitySamples[samplesNum - 1];
        waypoint = waypoints.Length - 1;
    }

    /* @brief パスのサンプリングの更新*/
    public void UpdatePathSamples()
    {
        totalDistance = 0f;
        float curPos = 0f;

        positionSamples = new Vector3[samplesNum];
        rotationSamples = new Quaternion[samplesNum];
        samplesDistances = new float[samplesNum];
        velocitySamples = new float[samplesNum];
        waypointSamples = new int[samplesNum];

        if (waypoints.Length == 0)
            return;

        for (int i = 0; i < samplesNum - 1; i++)
        {

            positionSamples[i] = computePositionAtPos(curPos);
            velocitySamples[i] = computeVelocityAtPos(curPos);
            waypointSamples[i] = GetWaypointFromPos(curPos);


            if (i == 0)
                samplesDistances[i] = 0;
            else
                samplesDistances[i] = Vector3.Distance(positionSamples[i], positionSamples[i - 1]);

            // increment total distance;
            totalDistance += samplesDistances[i];

            // increment pos
            curPos += (1f / ((float)samplesNum - 1));
        }
        positionSamples[samplesNum - 1] = computePositionAtPos(loop ? 0f : 1f);
        velocitySamples[samplesNum - 1] = computeVelocityAtPos(loop ? 0f : 1f);
        waypointSamples[samplesNum - 1] = GetWaypointFromPos(loop ? 0f : 1f);

        samplesDistances[samplesNum - 1] = Vector3.Distance(positionSamples[samplesNum - 1], positionSamples[samplesNum - 2]);

        // increment total distance;
        totalDistance += samplesDistances[samplesNum - 1];
    }

    /* @brief 現在のポイント番号取得*/
    public int GetCurrentWaypoint()
    {
        return GetWaypointFromPos(currentPos);
    }

    /* @brief ポイント位置から座標取得*/
    public int GetWaypointFromPos(float pos)
    {
        float step = 1f / (float)(waypoints.Length - (loop ? 0 : 1));
        int wp = (Mathf.FloorToInt(pos / step)) % (waypoints.Length);
        if (wp < 0)
            wp += waypoints.Length;
        return wp;
    }

    /* @brief 座標から最も近いポイント位置を取得*/
    public float GetCurrentPosFromPosition(Vector3 pos)
    {
        //Y軸は考慮しない
        Vector3 nowPos = pos;
        for(int i=1;i<samplesNum;i++)
        {
            Vector3 preDist = nowPos-positionSamples[i-1];
            preDist.y = 0f;
            float pre_now = Vector3.Magnitude(preDist);
            Vector3 aftDist = nowPos-positionSamples[i];
            aftDist.y = 0f;
            float aft_now = Vector3.Magnitude(aftDist);
            
            if(pre_now<=aft_now)
            {
                //return samplesPos[i -1];
            }
        }
        return 0;
    }

    #region 計算

    /* @brief 特定位置での座標計算*/
    public Vector3 computePositionAtPos(float pos)
    {
        if (waypoints.Length < 1)
            return Vector3.zero;
        else if (waypoints.Length == 1)
            return waypoints[0].position;
        else if (waypoints.Length >= 1 && pos == 0)
            return waypoints[0].position;
        else
        {

            if (pos >= 1)
            {
                if (loop)
                {
                    return waypoints[0].position;
                }
                else
                {
                    return waypoints[waypoints.Length - 1].position;
                }
            }

            float step = 1f / (float)(waypoints.Length - (loop ? 0 : 1));
            int posWaypoint = GetWaypointFromPos(pos);
            float posOffset = pos - (posWaypoint * step);
            float stepPos = posOffset / step;

            return PathUtility.Vector3Bezier(
                waypoints[(posWaypoint) % (waypoints.Length)].position,
                waypoints[(posWaypoint) % (waypoints.Length)].outTangent + waypoints[(posWaypoint) % (waypoints.Length)].position,
                waypoints[(posWaypoint + 1) % (waypoints.Length)].inTangent + waypoints[(posWaypoint + 1) % (waypoints.Length)].position,
                waypoints[(posWaypoint + 1) % (waypoints.Length)].position,
                stepPos);
        }
    }

    /* @brief 特定位置での速度計算*/
    public float computeVelocityAtPos(float pos)
    {
        if (waypoints.Length < 1)
            return 1;
        else if (waypoints.Length == 1)
            return waypoints[0].velocity;
        else
        {
            if (pos >= 1)
            {
                if (loop)
                    return waypoints[0].velocity;
                else
                    return waypoints[waypoints.Length - 1].velocity;
            }

            float step = 1f / (float)(waypoints.Length - (loop ? 0 : 1));
            int posWaypoint = GetWaypointFromPos(pos);
            float posOffset = pos - (posWaypoint * step);
            float stepPos = posOffset / step;

            Waypoint wp1 = waypoints[(posWaypoint) % (waypoints.Length)];
            Waypoint wp2 = waypoints[(posWaypoint + 1) % (waypoints.Length)];

            float control1;
            if (wp1.outVariation == Waypoint.VelocityVariation.Fast)
                control1 = wp2.velocity;
            else if (wp1.outVariation == Waypoint.VelocityVariation.Medium)
                control1 = Mathf.Lerp(wp1.velocity, wp2.velocity, 0.5f);
            else
                control1 = wp1.velocity;

            float control2;
            if (wp2.inVariation == Waypoint.VelocityVariation.Fast)
                control2 = wp1.velocity;
            else if (wp2.inVariation == Waypoint.VelocityVariation.Medium)
                control2 = Mathf.Lerp(wp1.velocity, wp2.velocity, 0.5f);
            else
                control2 = wp2.velocity;

            return PathUtility.FloatBezier(
                wp1.velocity,
                control1,
                control2,
                wp2.velocity,
                stepPos);
        }
    }

    #endregion

    #region  get/set関数

    /* @brief イベント管理の為の最終ポイントの取得*/
    public int LastPassedWayponint
    {
        get { return _lastPassedWayponint; }
    }
    #endregion

#if UNITY_EDITOR
    /* @brief エディタ上にパスの描画*/
    void OnDrawGizmos()
    {
        if (!gameObject.Equals(Selection.activeGameObject))
        {
            Matrix4x4 mat = Handles.matrix; 
            Handles.matrix = transform.localToWorldMatrix;

            for (int i = 0; i < waypoints.Length; i++)
            {

                if (i > 0)
                {
                    Handles.DrawBezier(
                        waypoints[i - 1].position,
                        waypoints[i].position,
                        waypoints[i - 1].position + waypoints[i - 1].outTangent,
                        waypoints[i].position + waypoints[i].inTangent,
                        pathColor,
                        null,
                        2f);
                }
                else
                {
                    if (loop)
                    {
                        Handles.DrawBezier(
                            waypoints[waypoints.Length - 1].position,
                            waypoints[0].position,
                            waypoints[waypoints.Length - 1].position + waypoints[waypoints.Length - 1].outTangent,
                            waypoints[0].position + waypoints[0].inTangent, pathColor,
                            null,
                            2f);
                    }
                }
            }

            //// Selection button
            //if (waypoints.Length > 0)
            //{
            //    Gizmos.DrawIcon(transform.TransformPoint(Waypoints[0].position), "path.png", true);
            //}

            Handles.matrix = mat;  // Thanks to Leon
        }
    }
#endif
}

