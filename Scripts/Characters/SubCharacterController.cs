/*
 *  @file           SubCharacterController.cs
 *  @brief         サブキャラ操作処理
 *  @date         2017/04/12
 *  @author      金澤信芳
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InputGamePad;

public class SubCharacterController : MonoBehaviour
{
    /*public宣言*/
    public float jumpPower = 0f;                      //ジャンプ力
    public Transform partnerPos;                     //他プレイヤーの位置
    public Collider partnerCol;                         //他プレイヤーの当たり判定
    public Rigidbody partnerRig;                     //他プレイヤーの当たり判定
    public Animator animator;
    public float subPlayerStopPos = 0f;             //サブキャラが止まる距離
    public float subPlayerDistance = 0f;            //サブキャラの近づく距離
    public static Collider subScissor;

    /*private宣言*/
    private GameObject nearGimmick = null;          //ギミック
    private bool isAbleJump = true;                        //ジャンプ可能フラグ
    private float Ltrg;
    private Vector3 nearGimmickPos;                     //最も近いギミックの座標
    private Vector3 throwAngle;                            //メインプレイヤーとカーソルの角度
    private Rigidbody rigidBody;
    private bool isSubPlayerCarry = false;              //サブプレイヤー運んでいるか

    #region 仁科追記
    //振子運動関係=======
    public float windUpPower;                                   //巻き上げる力
    private float radius;                                                   //半径
    [Range(0f, 5f)] public float minRadius;              //半径最小値
    private float maxRadius;                                          //半径最大値
    [Range(0f, 1f)] public float acceleration;            //加速力
    private float addAccele;                                            //角度加算値
    [Range(0f, 10f)] public float firstAcceleSpeed;      //初速保存
    private float accelSpeed;                                               //速度
    [Range(0f, 1f)] public float gravityAccele;                 //重力加速度
    private bool isPendulum = false;                                //振り子フラグ
    private float swingAngle;                                           //振り幅
    private bool pendulumDirX = false;                                          //振り子の方向
    private Vector3 prePosition = Vector3.zero;           //前フレームの加算値
    //=======
    //=======
    public PlayerPath playerPath;
    //=======
    #endregion

    private enum State
    {
        eFollow,            //ついていく
        eFastMove,
        // イベントステート
        eScissors,          //鋏む
        eHung,              //ぶら下がり
        eAim,                //狙う
    }
    State state;

    /* @brief   初期化*/
    void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
        partnerCol.GetComponent<Collider>();

        state = State.eFollow;
        animator = GetComponent<Animator>();
        partnerPos = GameObject.FindGameObjectWithTag("Player").transform;
    }

    /* @brief   物理演算系更新*/
    private void FixedUpdate()
    {
        //Action();
        Move();
    }

    /* @brief   移動*/
    void Move()
    {
        Ltrg = GamePad.GetTrigger(GamePad.Trigger.LeftTrigger, false);

        switch (state)
        {
            case State.eFollow:
                FollowMove();
                break;
            case State.eFastMove:
                FastMove();
                break;
            case State.eHung:
                HungingMove();
                break;
        }

    }

    /* @brief プレイヤーの元へ高速移動*/
    void FastMove()
    {
        transform.position = Vector3.Lerp(transform.position, partnerPos.position, 0.5f);
        if(Vector3.Distance(transform.position,partnerPos.position)<1f)
        {
            state = State.eFollow;
        }
    }

    /* @brief   サブキャラクターの移動*/
    void FollowMove()
    {
#if true
        Vector3 mainPlayerPos = partnerPos.position;
        Vector3 Direction = mainPlayerPos - transform.position;
        float Distance = Direction.sqrMagnitude;
        Direction.y = 0f;


        //メインプレイヤーのキャラが一定以上でなければ、サブキャラは近寄らない
        if (Distance >= subPlayerDistance + subPlayerStopPos)
        {
            // 目的地を算出
            Vector3 targetPos = partnerPos.TransformPoint(new Vector3(0.5f, -1f, -1.0f));
            Vector3 velocity = Vector3.zero;

            // 移動
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, 0.2f);
        }
        //else if (Distance > subPlayerDistance + subPlayerStopPos + 2f)
        //{
        //    animatorSubPlayer.SetBool("isJump", true);
        //    Jump(); // あぁ^～心がぴょんぴょんするんじゃぁ^～
        //}
        else
        {
            //animator.SetBool("isWalk", false);
        }
        if (GamePad.GetLeftStickAxis(false).y > 0.9f)
        {
            state = State.eFastMove;
        }
#else 
#endif
    }

    /* @brief ぶら下がりにおける移動*/
    void HungingMove()
    {
        // オブジェクトをはさむ
        //if (isSubPlayerCarry)
        //{
        rigidBody.velocity = Vector3.zero;
        transform.position = nearGimmickPos;
        //}
    }

    #region 振子
    /* @brief 振子運動の設定 */
    void PendulumSetting()
    {
        radius = Vector3.Distance(transform.position, partnerPos.position);
        maxRadius = radius;
        Vector3 targetDir = transform.position - partnerPos.position;
        pendulumDirX = (targetDir.x <= targetDir.z) ? true : false;
        Vector3 right = (partnerPos.position + Vector3.right) - partnerPos.position;
        swingAngle = Vector3.Angle(targetDir, right);
        addAccele = 0;
        accelSpeed = firstAcceleSpeed;
        rigidBody.useGravity = false;
        partnerRig.useGravity = false;
        partnerRig.isKinematic = true;
    }

    /* @brief 巻き上げ*/
    void Hoisting()
    {
        radius -= windUpPower * GamePad.GetLeftStickAxis(true).y;
        if (radius <= minRadius)
        {
            radius = minRadius;
        }
        else if (radius >= maxRadius)
        {
            radius = maxRadius;
        }
    }

    /* @brief 振り子の加速*/
    void PendulumAcceleration()
    {
        addAccele -= acceleration * GamePad.GetLeftStickAxis(true, GamePad.Stick.AxisX);
        const float maxAddAngle = 3f;
        const float minAddAngle = -3f;
        if (addAccele >= maxAddAngle)
        {
            addAccele = maxAddAngle;
        }
        else if (addAccele <= minAddAngle)
        {
            addAccele = minAddAngle;
        }
    }

    /* @brief 振子状態における移動*/
    void Pendulum()
    {
        //巻き上げ
        Hoisting();
        //振り子の加速
        PendulumAcceleration();
        //支点
        Vector3 fulcrum = partnerPos.position;
        fulcrum.y *= -1f;

        //現在の位置
        var rad = swingAngle * Mathf.Deg2Rad;
        var preRad = rad;
        var px = fulcrum.x + Mathf.Cos(rad) * radius;
        var py = fulcrum.y + Mathf.Sin(rad) * radius;
        var pz = fulcrum.z + Mathf.Cos(rad) * radius;

        //重力移動量を反映した位置
        var vx = px - fulcrum.x;
        var vy = py - fulcrum.y;
        var vz = pz - fulcrum.z;
        var t = -(vy * gravityAccele) / (vx * vx + vy * vy);
        var gx = px + t * vx;
        var gy = py + gravityAccele + t * vy;
        var gz = pz + t * vz;

        //2つの位置の角度差
        var rx = Mathf.Atan2(gy - fulcrum.y, gx - fulcrum.x) * Mathf.Rad2Deg;
        var rz = Mathf.Atan2(gy - fulcrum.y, gz - fulcrum.z) * Mathf.Rad2Deg;

        //角度差を角速度に加算
        const float fullAngle = 360f;
        const float halfAngle = 180f;
        var sub = rx - swingAngle;
        {
            sub -= Mathf.Floor(sub / fullAngle) * fullAngle;
            if (sub <= halfAngle)
                sub += fullAngle;
            if (sub > halfAngle)
                sub -= fullAngle;
            accelSpeed += sub;
        }
        var subZ = rz - swingAngle;
        {
            subZ -= Mathf.Floor(subZ / fullAngle) * fullAngle;
            if (subZ <= halfAngle)
                subZ += fullAngle;
            if (subZ > halfAngle)
                subZ -= fullAngle;
            //accelSpeed += subZ;
        }
        //角度に角加算と加速力を加算
        swingAngle += accelSpeed + addAccele;
        //新しい位置
        rad = swingAngle * Mathf.Deg2Rad;
        px = fulcrum.x + Mathf.Cos(rad) * radius;
        py = (fulcrum.y + Mathf.Sin(rad) * radius) * -1f;
        pz = fulcrum.z + Mathf.Cos(rad) * radius;

        //高さは振子運動を使用
        Vector3 tempPos = transform.position;
        tempPos.y = py;
        transform.position = tempPos;

        //水平軸はパスに添わせる
        Vector3 vec = new Vector3(px, 0f, pz) - transform.position;
        float diff = (pendulumDirX) ? vec.x : vec.z;
        //playerPath.SetInput(1, diff * pendulumMag);
    }
    #endregion

    /* @brief   ジャンプ*/
    void Jump()
    {
        rigidBody.AddForce(Vector3.up * jumpPower);
        isAbleJump = false;
    }

    /* @brief 衝突した瞬間検知*/
    void ChildOnTriggerEnter(Collider col)
    {
    }

    /* @brief   プレイヤーが衝突している*/
    void ChildOnTriggerStay(Collider col)
    {
        subScissor = col;
        //触れているギミック取得
        if (state != State.eHung && col.transform.tag == "Hook" && Ltrg > 0.2)
        {
            nearGimmick = subScissor.gameObject;
            nearGimmickPos = transform.position;
            state = State.eHung;
        }
        //ギミックを離す
        else if (Ltrg < 0.2 && nearGimmick != null)
        {
            MainCharacterController.isSubScissor = false;
            //isSubPlayerCarry = false;
            //nearGimmickPos = transform.position * -2f;
            nearGimmick = null;
            state = State.eFollow;
            //Destroy(gameObject.AddComponent<FixedJoint>())
        }
    }

    /* @brief   Clip部分が衝突していない*/
    void ChildOnTriggerExit(Collider col)
    {
        //触れていたオブジェクトがギミックの時
        if (col.transform.tag == "SubPlayer")
        {
            //Debug.Log("離れた");
        }
    }

}
