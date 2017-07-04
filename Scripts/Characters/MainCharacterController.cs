/*!
 *  @file           MainCharacterController.cs
 *  @brief         メインキャラ操作処理
 *  @date         2017/04/12
 *  @author      金澤信芳
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InputGamePad;

/*! @brief メインキャラクター管理クラス*/
public class MainCharacterController : MonoBehaviour
{
    [Range(6.0f, 20.0f)] public float jumpPower = 0f;                      /*! ジャンプ力*/
    public int rollUpPower = 0;                         /*! 巻き上げる力*/
    public float lookAngle = 0f;                         /*! キャラクターの向く角度*/
    private float moveSpeed = 0f;                    /*! 移動量*/
    public float normalSpeed = 1f;                    /*! 通常移動速度*/
    public float dashSpeed = 2f;                        /*! ダッシュ時移動速度*/
    public LayerMask groundLayer;                 /*! 地面のレイヤー*/
    public GameObject subPlayer;                    /*! サブキャラクターオブジェクト*/
    public Animator animator;                           /*! アニメーター*/
    public PlayerPath playerPath;                     /*! プレイヤーの移動軌跡*/
    public static bool isLookFront = true;       /*! 前を見ているか*/
    public static Collider mainScissor;              /*! 挟み判定をするコライダー*/
    public bool isSubPlayerCarry = false;           /*! サブキャラ運んでいるか*/
    public Transform leftHand;                          /*! 左手*/
    public Transform rightHand;                       /*! 右手*/
    public enum State                                          /*! 行動状態*/
    {
        eNormal = 0,       //通常
        eStop,                   //停止
        eAction,               //振り子
        eScissors,            //鋏む
        eHung,                 //ぶら下がり
        eAim,                    //狙う
        eBlowAway,       //飛んでいる状態
    }
    private State state;        /*! 行動状態*/

    private Rigidbody subPlayerRig;                            /*! サブキャラクターのリジッドボディ*/
    private bool isItemCarry = false;                            /*! アイテムを運んでいるか*/
    private bool isCarry = false;                                     /*! 何かを運んでいるか*/
    private GameObject nearGimmick = null;           /*! ギミック*/
    private bool isAbleJump = true;                             /*! ジャンプ可能フラグ*/
    private Vector2 Stick;                                                /*! 左スティックの入力値*/
    private bool rightTrigger;                                                      /*! 右トリガーの入力値*/
    private Rigidbody rigidBody;                                 /*! 自身のリジッドボディ*/
    private GameObject cursor;                                   /*! カーソルオブジェクト*/
    private CameraWork cameraWork;                     /*! カメラワーク管理クラス*/
    private const float lookDist = 5f;                          /*! プレイヤーの注視距離*/
    private Vector3 preLookAt;                                   /*! 更新前の注視座標*/
    private float prePathPos;                                       /*! 更新前のパス上の位置*/

    // 振子運動関係
    public float windUpPower = 1f;                                                              /*! 巻き上げる力*/
    private float radius;                                                                                    /*! 半径*/
    [Range(0f, 5f)] public float minRadius = 1f;                                        /*! 半径最小値*/
    private float maxRadius;                                                                          /*! 半径最大値*/
    [Range(0f, 1f)] public float acceleration = 1f;                                     /*! 加速力*/
    private float addAccele;                                                                            /*! 角度加算値*/
    [Range(0f, 10f)] public float firstAcceleSpeed = 0.1f;                      /*! 初速保存*/
    private float accelSpeed;                                                                          /*! 速度*/
    [Range(0f, 0.1f)] public float gravityAccele = 0.5f;                           /*! 重力加速度*/
    private bool isPendulum = false;                                                            /*! 振り子フラグ*/
    private float swingAngle;                                                                         /*! 振り幅*/
    private bool pendulumDirX = false;                                                       /*! 振り子の方向*/
    private float preAngleDeg;                                                                      /*! 更新前の振子の角度*/
    private float newAngleDeg;                                                                    /*! 更新後の振子の角度*/
    private Vector3 fulcrum;                                                                          /*! 支点*/
    private Vector3 prePos;                                                                           /*! 更新前座標*/
    private float posDiff;                                                                                 /*! 座標の差分*/
    const float pendulumMag = 10f;                                                           /*! 振り子時の移動倍率*/
    public float blowAwayPower = 10;                                                       /*! 水平軸の吹っ飛び力*/
    public float blowAwayUpPow = 1000f;                                               /*! 吹っ飛び上方への力*/
    [Range(0f, 90f)] public float no_blowAwayDegMin = 45f;          /*! 吹っ飛び時の上方へ飛ばない最小の角度*/
    [Range(91f, 180f)] public float no_blowAwayDegMax = 135f;    /*! 吹っ飛び時の上方へ飛ばない最大の角度*/
    private bool isHangFront;                                                                       /*! 前方方向へぶら下がっているかどうか*/
    //

    /*! @brief   初期化*/
    void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
        cursor = GameObject.Find("cursor");
        cameraWork = GameObject.Find("CameraWork").GetComponent<CameraWork>();
        subPlayerRig = subPlayer.GetComponent<Rigidbody>();
        state = State.eNormal;
        //animator = GetComponent<Animator>();
        jumpPower *= 100f;
    }

    /*! @brief   物理演算系更新*/
    private void FixedUpdate()
    {
        InputController();      //コントローラ入力検知
        Move();                         //移動
        Motion();                       //モーション
    }

    /*! @brief   移動*/
    void Move()
    {
        switch (state)
        {
            case State.eNormal:
                NormalMove();
                //注視向き補整
                NormalRotation();
                break;
            case State.eAction:
                Action();
                break;
            case State.eBlowAway:
                BlowAway();
                break;
        }
        //座標更新
        if (playerPath && state != State.eStop)
        {
            Vector3 add = playerPath.GetAddPotision();
            if (add != Vector3.zero)
            {
                prePos = transform.position;
                prePathPos = playerPath.currentPos;
            }
            transform.position += add;
        }
    }

    /*! @brief   メインキャラクターのアクション*/
    void Action()
    {
        if (!isPendulum)
        {
            //振り子初期化
            PendulumSetting();
            isPendulum = true;
        }
        Pendulum();

        //振り子の終了
        if (!SubCharacterController.isScissor)
        {
            isPendulum = false;
            rigidBody.useGravity = true;
            subPlayerRig.useGravity = true;
            animator.SetBool("isHangFront", false);
            animator.SetBool("isHangBack", false);
            animator.SetBool("isLeave", true);
            state = State.eBlowAway;
            if (swingAngle <= no_blowAwayDegMin || swingAngle >= no_blowAwayDegMax)
            {
                rigidBody.AddForce(Vector3.up * blowAwayUpPow);
            }
        }
    }

    /*! @brief 振子運動による吹き飛び*/
    void BlowAway()
    {
        BringItem();
        playerPath.SetInput(1f, posDiff * blowAwayPower);
        Vector3 newPos = transform.position - (transform.up * 3.5f);
        if (Physics.Linecast(transform.position, newPos, groundLayer))
        {
            state = State.eNormal;
        }
    }

    /*! @brief アイテム所持*/
    void BringItem()
    {
        if (isItemCarry && nearGimmick != null)
        {
            Vector3 itemPos = (leftHand.position + rightHand.position) / 2;
            nearGimmick.transform.position = itemPos;
        }
    }

    #region　振子運動
    /*! @brief 振子運動の設定*/
    void PendulumSetting()
    {
        Vector3 vec = transform.position - prePos;
        vec.y = 0f;
        float x = Mathf.Abs(vec.x);
        float z = Mathf.Abs(vec.z);
        if (x >= z)
        {
            pendulumDirX = true;
        }
        else
        {
            pendulumDirX = false;
        }

        radius = Vector3.Distance(transform.position, subPlayer.transform.position);
        maxRadius = radius;
        Vector3 targetDir = transform.position - subPlayer.transform.position;
        Vector3 dir = Vector3.Normalize(vec);
        swingAngle = Vector3.Angle(targetDir, dir);
        addAccele = 0;
        accelSpeed = firstAcceleSpeed;
        rigidBody.useGravity = false;
        subPlayerRig.useGravity = false;
        SetPendulumAnimation();
    }

    /*! @brief 巻き上げ*/
    void Hoisting()
    {
        radius -= windUpPower * Stick.y;
        if (radius <= minRadius)
        {
            radius = minRadius;
        }
        else if (radius >= maxRadius)
        {
            radius = maxRadius;
        }
    }

    /*! @brief 振り子の加速*/
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

    /*! @brief 振子運動*/
    void Pendulum()
    {
        //巻き上げ
        Hoisting();
        //振り子の加速
        PendulumAcceleration();
        //支点
        fulcrum = subPlayer.transform.position;
        fulcrum.y *= -1f;

        //現在の位置
        preAngleDeg = swingAngle;
        var rad = swingAngle * Mathf.Deg2Rad;
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
        float sub = (pendulumDirX) ? (rx - swingAngle) : (rz - swingAngle);
        //float sub =rx - swingAngle;
        sub -= Mathf.Floor(sub / fullAngle) * fullAngle;
        if (sub <= halfAngle)
            sub += fullAngle;
        if (sub > halfAngle)
            sub -= fullAngle;

        accelSpeed += sub;

        //角度に角加算と加速力を加算
        swingAngle += accelSpeed + addAccele;

        //新しい位置
        newAngleDeg = swingAngle;
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
        posDiff = (pendulumDirX) ? vec.x : vec.z;
        playerPath.SetInput(1f, posDiff * pendulumMag);

        //アイテム所持時の更新
        BringItem();
        //アニメ―ション更新
        TransitionAnimationByPendulum();
    }

    /*! @brief 支点の取得*/
    public Vector3 GetFulcrumPosition()
    {
        fulcrum.y *= -1;
        return fulcrum;
    }

    /*! @brief 半径の取得*/
    public float GetRadius()
    {
        return radius;
    }

    /*! @brief 振り子状態かどうかのフラグ取得*/
    public bool GetIsPendulum()
    {
        return isPendulum;
    }

    /*! @brief 振り子のモーション初期設定*/
    void SetPendulumAnimation()
    {
        if (playerPath.currentPos > prePathPos)
        {
            isHangFront = (swingAngle < 90f) ? true : false;
        }
        else
        {
            isHangFront = (swingAngle < 90f) ? false : true;
        }
        animator.SetBool("isHangFront", isHangFront);
        animator.SetBool("isHangBack", !isHangFront);
    }

    /*! @brief 振り子状態の時のアニメーション遷移*/
    void TransitionAnimationByPendulum()
    {
        if (isItemCarry) return;
        if (isHangFront && preAngleDeg < newAngleDeg)
        {
            isHangFront = !isHangFront;
        }
        else if (!isHangFront && preAngleDeg > newAngleDeg)
        {
            isHangFront = !isHangFront;
        }

        animator.SetBool("isHangFront", isHangFront);
        animator.SetBool("isHangBack", !isHangFront);
    }
    #endregion 

    /*! @brief メインキャラクターの移動*/
    void NormalMove()
    {
        //横移動
        playerPath.SetInput(Stick.x, moveSpeed);

        //ジャンプ
        if (isAbleJump && GamePad.GetButtonDown(GamePad.Button.Jump))
        {
            Jump();
        }

        //カーソルの出現フラグ
        cursor.SetActive(isSubPlayerCarry);

        //キャラクターの中央から足元にかけて、接地判定用のラインを引く
        Vector3 newPos = transform.position - (transform.up * 3.5f);

        isAbleJump = Physics.Linecast(transform.position, newPos, groundLayer); // Linecastが判定するレイヤー 

        if (isSubPlayerCarry)
        {
            // サブキャラを持ち上げる
            animator.SetBool("isScissorsUp", true);
        }
        else if (isItemCarry)
        {
            // アイテムを持ち上げる
            animator.SetBool("isScissors", true);
            Vector3 itemPos = (leftHand.position + rightHand.position) / 2;
            nearGimmick.transform.position = itemPos;
        }

        // ギミックを離す
        if (rightTrigger && nearGimmick != null)
        {
            if (isSubPlayerCarry)
            {
                isSubPlayerCarry = false;
                cursor.GetComponent<CursorMove>().throwPos = cursor.transform.position;
                animator.SetBool("isScissorsUp", false);
                animator.SetBool("isLeave", true);
            }
            else if (isItemCarry)
            {
                isItemCarry = false;
                animator.SetBool("isScissors", false);
            }

            isCarry = false;

            nearGimmick = null;
        }

        //振り子
        if (SubCharacterController.isScissor)
        {
            if (Stick.y > 0.8f)
            {
                state = State.eAction;
            }
        }
    }

    /*! @brief サブキャラクターを持ち運ぶ*/
    void BringSubCharactor()
    {
        if (rightTrigger && mainScissor.tag == "SubPlayer")
        {
            isSubPlayerCarry = true;
            animator.SetBool("isScissorsUp", true);
        }
        else if (!rightTrigger && isSubPlayerCarry)
        {
            isSubPlayerCarry = false;
            cursor.GetComponent<CursorMove>().throwPos = cursor.transform.position;
            animator.SetBool("isScissorsUp", false);
            animator.SetBool("isLeave", true);
        }
    }

    /*! @brief 通常移動にリセット*/
    public void SetNormalState()
    {
        state = State.eNormal;
    }

    /*! @brief 停止セット*/
    public void SetStopState()
    {
        state = State.eStop;
    }

    /*! @brief   コントローラーの入力検知*/
    void InputController()
    {
        Stick = GamePad.GetLeftStickAxis(false);
        rightTrigger = GamePad.GetTrigger(GamePad.Trigger.RightTrigger, false);
    }

    /*! @brief トリガーの入力取得*/
    public bool GetRightTrigger()
    {
        return rightTrigger;
    }

    /*! @brief   移動アニメーション*/
    void Motion()
    {
        if (state == State.eNormal)
        {
            #region 歩く
            if (Stick.x != 0f)
            {
                animator.SetBool("isWalk", true);
            }
            else if (Stick.x == 0f)
            {
                animator.SetBool("isWalk", false);
            }
            #endregion
            #region ダッシュ
            if (GamePad.GetButton(GamePad.Button.Dash) && Stick.x != 0f && isAbleJump)
            {
                animator.SetBool("isDash", true);
                moveSpeed = dashSpeed;
            }
            else
            {
                animator.SetBool("isDash", false);
                moveSpeed = normalSpeed;
            }
            #endregion
        }
    }

    /*! @brief   ジャンプ*/
    void Jump()
    {
        if (isItemCarry)
        {
            animator.SetBool("isScissorsJump", true);
        }
        else
        {
            animator.SetBool("isNormalJump", true);
        }
        isAbleJump = false;
    }

    /*! @brief　上に力を加える*/
    public void AddJumpPower()
    {
        rigidBody.AddForce(Vector3.up * jumpPower);
    }

    /*! @brief 回転補整*/
    void NormalRotation()
    {
        Vector3 dir = playerPath.GetAddPotision();
        if (dir == Vector3.zero)
        {
            dir = preLookAt;
        }
        preLookAt = dir;
        if (!isSubPlayerCarry)
        {
            dir *= lookDist;
            dir += cameraWork.GetCameraVec();
        }
        Vector3 lookat = transform.position + dir;
        transform.LookAt(lookat);
    }

    ///*! @brief サブプレイヤーのステートを運ぶに変更*/
    //public void ChangeSubStateToCarry()
    //{
    //    subPlayer.GetComponent<SubCharacterController>().SetStateBeCarried();
    //}

    /*! @brief 行動状況の取得*/
    public State GetState()
    {
        return state;
    }

    /*! @brief 衝突した瞬間検知*/
    void ChildOnTriggerEnter(Collider col)
    {
        if (col.tag == "Gimmick")　//ギミックに触れたらギミック発動
        {
            col.GetComponent<Gimmick>().isGimmick = true;
        }
    }

    /*! @brief   衝突を継続検知*/
    void ChildOnTriggerStay(Collider col)
    {
        mainScissor = col;
    }

    /*! @brief   衝突から離れたかどうかの検知*/
    void ChildOnTriggerExit(Collider col)
    {
        mainScissor = null;
    }
}

