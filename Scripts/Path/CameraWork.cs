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
using System;

#if UNITY_EDITOR
using UnityEditor;

[ExecuteInEditMode]
[InitializeOnLoad]
#endif

/* @brief カメラワーククラス*/
public class CameraWork : MonoBehaviour
{
    [Serializable]
    //ポイント通過時のイベント
    public class WaypointChangedEvent : UnityEvent<int> { }

    public PlayerPath playerPath;                                               //プレイヤーの移動
    public MainCharacterController player;                              //プレイヤー
    public Color pathColor = Color.white;                                //Editor上のパスカラー
    public Waypoint[] waypoints = new Waypoint[] { };       //パスを定義するポイント                                                       
    public Transform target;                                                           //パスに沿わせる対象のトランスフォーム
    public Transform globalLookAt = null;                               //パス全体における対象物の注視対象

    public bool loop = false;                                                           //パスをループさせるかどうか
    public bool updateTransform = true;                                    //アニメーション中の変換の更新フラグ
    public bool presampledPath = false;                                     //等速移動フラグ

    private int _lastPassedWayponint;   //イベント管理の為の最終ポイント

    //public AnimationCurve offsetY;
    //public AnimationCurve dist;
    private float preInput = 1f;
    public float offsetY;
    public float dist;
    public float zoomOutDist = 5f;
    public float speed = 0.1f;
    public float globalFollowPathBias = 0.001f;                     //パスに沿う移動の偏り(0の方が高い)
    public float velocityBias = .1f;                                                //移動速度の偏り補正
    public float currentPos;                                                          //現在の補間位置(0~1)
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

    //アクティブ時の初期化
    void OnEnable()
    {
        playerPath = GameObject.Find("PlayerPath").GetComponent<PlayerPath>();
        //int lengh = playerPath.waypoints.Length;
        //for (int i = 0; i < lengh; i++)
        //{
        //    offsetY.AddKey(i, playerPath.waypoints[i].position.y+1f);
        //    dist.AddKey(i, 10.0f);
        //}
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
        //等速移動する場合はサンプリング処理
        if (presampledPath)
            UpdatePathSamples();

    }

    //更新
    void LateUpdate()
    {
        //DoUpdate();
        UpdateTarget(Vector3.zero, Quaternion.identity);
    }

    //フレーム単位の更新処理
    void DoUpdate()
    {
        //ポイントが無ければ通らない
        if (waypoints.Length == 0)
            return;

        {
            float input = 0f;
            if (playerPath.GetAddPotision() != Vector3.zero)
                input = playerPath.GetInput();

            float advance = (speed * velocityBias * _lastVelocity * input);

            // Advance
            currentPos += advance;
            //currentPos += advance;

            if (currentPos >= 1f)
            {
                if (loop)
                {
                    currentPos -= 1f;
                }
                else
                {
                    currentPos = 1f;
                    Pause(); // Thanks to Leon
                }
            }
            else if (currentPos <= 0f)
            {
                if (loop)
                {
                    currentPos += 1f;
                }
                else
                {
                    currentPos = 0f;
                    Pause(); // Thanks to Leon
                }
            }

            if (UpdateTransform || Application.isPlaying)
                UpdateTarget();

        }
    }

    #region     更新関係

    //パスに沿う対象の更新
    /// <param name="position">更新する座標</param>
    /// <param name="rotation">更新する回転</param>
    public void UpdateTarget(Vector3 position, Quaternion rotation)
    {
        Vector3 newPos = Vector3.zero;
#if false
        newPos = transform.TransformPoint(position);
        Quaternion newRot = transform.rotation * rotation;


        if (player.isCarry)
        {
            newPos +=  CameraZoomOut();
        }
#else
        //int waypoint = GetWaypointFromPos(playerPath.currentPos);
        //float time = playerPath.currentPos * (playerPath.waypoints.Length - 1);
        float dir = playerPath.GetInputOnly();
        if (dir == 0)
            dir = preInput;
        else
            preInput = dir;

        newPos = globalLookAt.position + globalLookAt.right * dir * dist;
        if (player.isCarry)
        {
            newPos += CameraZoomOut();
        }
#endif
        target.position = Vector3.Lerp(target.position, newPos, 0.1f);
        target.LookAt(globalLookAt);
        //target.rotation = newRot;
    }

    //チビキャラ鋏み状態の時、カメラを引く
    Vector3 CameraZoomOut()
    {
        Vector3 pp = playerPath.target.position;
        Vector3 dir = Vector3.Normalize(target.position - pp);

        return (dir * zoomOutDist);
    }

    //現在のポイント番号における、パスに沿う対象の更新
    public void UpdateTarget()
    {
        Vector3 position = Vector3.zero;
        Quaternion rotation = Quaternion.identity;
        float velocity = 1.0f;
        int waypoint = 0;

        if (presampledPath)
        {
            sampledPositionAndRotationAndVelocityAndWaypointAtPos(currentPos, out position, out rotation, out velocity, out waypoint);
        }
        else
        {
            position = computePositionAtPos(currentPos);
            rotation = computeRotationAtPos(currentPos);
            velocity = computeVelocityAtPos(currentPos);
            waypoint = GetWaypointFromPos(currentPos);
        }
        _lastVelocity = velocity;
        UpdateTarget(position, rotation);

        // Fire waypointChanged if is the case
        if (waypoint != _lastPassedWayponint)
        {
            if (waypointChanged != null)
                waypointChanged.Invoke(waypoint);
            if (waypoints[waypoint].reached != null)
                waypoints[waypoint].reached.Invoke();
        }

        _lastPassedWayponint = waypoint;

    }

    #endregion

    //パスがあればパスに沿った移動処理
    public void Play()
    {
        if (waypoints.Length == 0)
            return;
        //_lastVelocity = waypoints [computeVelocityAtPos (currentPos)].velocity;
    }

    //移動処理の一時停止
    public void Pause()
    {
        if (waypoints.Length == 0)
            return;
    }

    //移動処理の巻き戻し
    public void Rewind()
    {
        if (waypoints.Length == 0)
            return;
        currentPos = 0f;
    }

    //移動処理の停止
    public void Stop()
    {
        if (waypoints.Length == 0)
            return;
        currentPos = 0f;

        UpdateTarget(computePositionAtPos(currentPos), computeRotationAtPos(currentPos));
    }

    //サンプリングによるポイント情報の取得
    /// <param name="pos">0~1におけるポイント</param>
    /// <param name="position">座標(Vector3)</param>
    /// <param name="rotation">回転</param>
    /// <param name="velocity">速度</param>
    /// <param name="waypoint">ポイント</param>
    public void sampledPositionAndRotationAndVelocityAndWaypointAtPos(float pos, out Vector3 position, out Quaternion rotation, out float velocity, out int waypoint)
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

                if (globalLookAt != null)
                {
                    rotation = Quaternion.LookRotation(transform.InverseTransformPoint(globalLookAt.position) - position);
                }
                else
                {
                    rotation = Quaternion.Lerp(rotationSamples[i - 1], rotationSamples[i], interpFactor);
                }

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
        rotation = rotationSamples[samplesNum - 1];
        velocity = velocitySamples[samplesNum - 1];
        waypoint = waypoints.Length - 1;
    }

    //パスのサンプリングの更新
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
            rotationSamples[i] = computeRotationAtPos(curPos);
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
        rotationSamples[samplesNum - 1] = computeRotationAtPos(loop ? 0f : 1f);
        velocitySamples[samplesNum - 1] = computeVelocityAtPos(loop ? 0f : 1f);
        waypointSamples[samplesNum - 1] = GetWaypointFromPos(loop ? 0f : 1f);

        samplesDistances[samplesNum - 1] = Vector3.Distance(positionSamples[samplesNum - 1], positionSamples[samplesNum - 2]);

        // increment total distance;
        totalDistance += samplesDistances[samplesNum - 1];
    }

    //ポイントでの注視における回転
    /// <param name="index">ポイント番号</param>
    public Quaternion GetWaypointRotation(int index)
    {
        if (index < 0)
            index += (waypoints.Length);
        index %= waypoints.Length;
        return (waypoints[index].lookAt != null ? Quaternion.LookRotation(transform.InverseTransformPoint(waypoints[index].lookAt.position) - waypoints[index].position) : Quaternion.Euler(waypoints[index].rotation));
    }

    //現在のポイント番号取得
    public int GetCurrentWaypoint()
    {
        return GetWaypointFromPos(currentPos);
    }

    //ポイント位置から座標取得
    /// <param name="pos">Position.</param>
    public int GetWaypointFromPos(float pos)
    {
        float step = 1f / (float)(waypoints.Length - (loop ? 0 : 1));
        int wp = (Mathf.FloorToInt(pos / step)) % (waypoints.Length);
        if (wp < 0)
            wp += waypoints.Length;
        return wp;
    }

    //進行方向の取得
    /// <param name="index">The pos at which calculate the face forward orientation</param>
    public Quaternion GetFaceForwardForPos(float pos)
    {
        Quaternion rot;
        if (waypoints.Length <= 1)
            rot = Quaternion.identity;
        else
        {
            if (loop)
            {

                if (pos <= 0)
                    pos = 1 + pos;

                Vector3 p1 = computePositionAtPos((pos + globalFollowPathBias) % 1f);
                Vector3 p2 = computePositionAtPos(pos);

                rot = Quaternion.LookRotation(p1 - p2, Vector3.up);
            }
            else
            {

                float step = Mathf.Clamp01(pos + globalFollowPathBias);

                Vector3 p1 = computePositionAtPos(step);
                Vector3 p2 = computePositionAtPos(pos);

                if (p1 == p2)
                {
                    p1 = waypoints[waypoints.Length - 1].outTangent;
                    p2 = waypoints[waypoints.Length - 1].inTangent;
                }

                rot = Quaternion.LookRotation(p1 - p2, Vector3.up);
            }
        }

        return rot;
    }

    #region 計算

    //特定位置での回転計算
    /// <param name="pos">アニメーション位置</param>
    public Quaternion computeRotationAtPos(float pos)
    {
        if (globalLookAt != null)
        {
            return Quaternion.LookRotation(transform.InverseTransformPoint(globalLookAt.position) - computePositionAtPos(pos));
        }

        if (waypoints.Length < 1)
            return Quaternion.identity;
        else if (waypoints.Length == 1)
            return GetWaypointRotation(0);
        else
        {

            if (pos >= 1)
            {
                if (loop)
                {
                    return GetWaypointRotation(0);
                }
                else
                {
                    return GetWaypointRotation(waypoints.Length - 1);
                }
            }

            float step = 1f / (float)(waypoints.Length - (loop ? 0 : 1));
            int posWaypoint = GetWaypointFromPos(pos);
            float posOffset = pos - (posWaypoint * step);
            float stepPos = posOffset / step;

            Quaternion p = GetWaypointRotation(posWaypoint);
            Quaternion prevP = GetWaypointRotation(posWaypoint - 1);
            Quaternion nextP = GetWaypointRotation(posWaypoint + 1);
            Quaternion nextNextP = GetWaypointRotation(posWaypoint + 2);

            return PathUtility.QuaternionBezier(p, prevP, nextP, nextNextP, stepPos);
        }
    }

    //特定位置での座標計算
    /// <param name="pos">アニメーション位置</param>
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

    //特定位置での速度計算
    /// <param name="pos">アニメーション位置</param>
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

    //ポイントの取得とセット
    public Waypoint[] Waypoints
    {
        get { return waypoints; }
        set
        {
            waypoints = value;
            if (presampledPath)
                UpdatePathSamples();
            UpdateTarget();
        }
    }

    //パス全体における注視対象の取得と設定
    public Transform GlobalLookAt
    {
        get { return globalLookAt; }
        set { globalLookAt = value; }
    }

    //パスに沿う移動の偏りの取得と設定
    public float GlobalFollowPathBias
    {
        get { return globalFollowPathBias; }
        set { globalFollowPathBias = value; }
    }

    //パスをループさせるかどうかの取得と設定
    public bool Loop
    {
        get { return loop; }
        set { loop = value; }
    }

    //移動速度の偏り補正の取得と設定
    public float VelocityBias
    {
        get { return velocityBias; }
        set { velocityBias = value; }
    }

    //現在のアニメーション位置の取得と設定
    public float CurrentPos
    {
        get { return currentPos; }
        set
        {
            currentPos = value;
            UpdateTarget();
        }
    }

    //最後のポイント通過時のイベントの取得と設定
    public WaypointChangedEvent WaypointChanged
    {
        get { return waypointChanged; }
        set { waypointChanged = value; }
    }

    //等速移動フラグの取得
    public bool PresampledPath
    {
        get { return presampledPath; }
    }

    //等速移動の為のサンプリング精度の取得
    public int SamplesNum
    {
        get { return samplesNum; }
    }

    //イベント管理の為の最終ポイントの取得
    public int LastPassedWayponint
    {
        get { return _lastPassedWayponint; }
    }

    //サンプリング座標の取得
    public Vector3[] PositionSamples
    {
        get { return positionSamples; }
    }

    //サンプリングの回転の取得
    public Quaternion[] RotationSamples
    {
        get { return rotationSamples; }
    }

    //サンプリング移動速度の取得
    public float[] VelocitySamples
    {
        get { return velocitySamples; }
    }

    //サンプリングにおけるポイントの取得
    public int[] WaypointSamples
    {
        get { return waypointSamples; }
    }

    //サンプリングにおけるポイント間の距離取得
    public float[] SamplesDistances
    {
        get { return samplesDistances; }
    }

    //総距離の取得
    public float TotalDistance
    {
        get { return totalDistance; }
    }

    //アニメーション中の変換の更新フラグの取得と設定
    public bool UpdateTransform
    {
        get { return updateTransform; }
        set { updateTransform = value; }
    }

    #endregion

#if UNITY_EDITOR
    //エディタ上にパスの描画
    //void OnDrawGizmos()
    //{
    //    if (!gameObject.Equals(Selection.activeGameObject))
    //    {
    //        Matrix4x4 mat = Handles.matrix;  // Thanks to Leon
    //        Handles.matrix = transform.localToWorldMatrix;

    //        for (int i = 0; i < waypoints.Length; i++)
    //        {

    //            if (i > 0)
    //            {
    //                Handles.DrawBezier(
    //                    waypoints[i - 1].position,
    //                    waypoints[i].position,
    //                    waypoints[i - 1].position + waypoints[i - 1].outTangent,
    //                    waypoints[i].position + waypoints[i].inTangent,
    //                    pathColor,
    //                    null,
    //                    2f);
    //            }
    //            else
    //            {
    //                if (loop)
    //                {
    //                    Handles.DrawBezier(
    //                        waypoints[waypoints.Length - 1].position,
    //                        waypoints[0].position,
    //                        waypoints[waypoints.Length - 1].position + waypoints[waypoints.Length - 1].outTangent,
    //                        waypoints[0].position + waypoints[0].inTangent, pathColor,
    //                        null,
    //                        2f);
    //                }
    //            }
    //        }

    //        //// Selection button
    //        //if (waypoints.Length > 0)
    //        //{
    //        //    Gizmos.DrawIcon(transform.TransformPoint(Waypoints[0].position), "path.png", true);
    //        //}

    //        Handles.matrix = mat;  // Thanks to Leon
    //    }
    //}
#endif
}