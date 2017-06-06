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

public class MainCharacterController : MonoBehaviour
{
    /*!public宣言*/
    public float jumpPower = 0f;                      //ジャンプ力
    public float throwPower = 0f;                     //投げる力
    public int rollUpPower = 0;                         //巻き上げる力
    public float lookAngle = 0f;                        //キャラクターの向く角度
    public int moveSpeed = 0;                         //移動量
    public LayerMask groundLayer;                   //地面のレイヤー
    public Transform subCharaPos;                     //他プレイヤーの位置
    public Collider subCharaCol;                         //他プレイヤーの当たり判定
    public Rigidbody subCharaRig;                     //他プレイヤーの当たり判定
    public Animator animator;
    public PlayerPath playerPath;                     //プレイヤーの移動軌跡
    public bool isCarry = false;                         //メインプレイヤー運んでいるか
    public static bool isLookFront = true;        //前を見ているか
    public static bool isSubScissor;                //サブキャラがはさんでいるか
    public static Collider mainScissor;
    public float hookShotRange;
    public float freeNum; // デバッグ用数値

    /*!private宣言*/
    private GameObject nearGimmick = null;      //ギミック
    private bool isAbleJump = true;                 //ジャンプ可能フラグ
    private Vector2 Stick;                                  //左スティックの入力値
    private float Ltrg;
    private float Rtrg;
    private Vector3 nearGimmickPos;                //最も近いギミックの座標
    private Vector3 throwAngle;                       //メインプレイヤーとカーソルの角度
    private Rigidbody rigidBody;
    private GameObject cursor;                        //カーソル取得
    private int dir;
    private float dist;
    private bool HookShotInitFlg = true;
    private float DistX;
    private float DistY;
    private bool initTarzanFlg;
    private float tarzanDistX;
    private float tarzanDistZ;

    private float nowTime;
    private float afterTime;
    private float countX;
    private float countY;
    private bool isSwingFront;
    private bool high;
    private float timeY = 0.04f;
    private float tarzanDistY = 0.0f;
    private Vector3 tarzanHigh;

    //振子運動関係=======
    public float windUpPower=1f;                                   //巻き上げる力
    private float radius;                                                   //半径
    [Range(0f, 5f)] public float minRadius=1f;              //半径最小値
    private float maxRadius;                                          //半径最大値
    [Range(0f, 1f)] public float acceleration=1f;            //加速力
    private float addAccele;                                            //角度加算値
    [Range(0f, 10f)] public float firstAcceleSpeed=0.1f;      //初速保存
    private float accelSpeed;                                               //速度
    [Range(0f, 0.1f)] public float gravityAccele=0.5f;                 //重力加速度
    private bool isPendulum = false;                                //振り子フラグ
    private float swingAngle;                                           //振り幅
    private bool pendulumDirX = false;                                          //振り子の方向
    private float preAngleDeg;                                      //更新前の振子の角度
    private float newAngleDeg;                                             //更新後の振子の角度
    private Vector3 fulcrum;                                            //支点
    private Vector3 prePos;
    //=======

    private enum State
    {
        eNormal = 0,    //通常

        // イベントステート
        eAction,
        eScissors,          //鋏む
        eHung,              //ぶら下がり
        eAim,                //狙う
        eBlowAway,                   //飛んでいる状態
    }
    State state;
    private object newPos;

    /*! @brief   初期化*/
    void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
        subCharaCol.GetComponent<Collider>();

        cursor = GameObject.Find("cursor");

        state = State.eNormal;
        animator = GetComponent<Animator>();
        subCharaPos = GameObject.FindGameObjectWithTag("SubPlayer").transform;

        nowTime = Mathf.Sin(Time.time);
        afterTime = nowTime;
    }

    /*! @brief   物理演算系更新*/
    private void FixedUpdate()
    {
        
        InputController();
        Motion();
        Move();
        
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
        if (playerPath)
        {
            Vector3 add = playerPath.GetAddPotision();
            if (add != Vector3.zero)
            {
                prePos = transform.position;
            }
            transform.position += add;
        }
    }

    /*! @brief   メインキャラクターのアクション*/
    void Action()
    {
#if false
        // 重力オフ
        rigidBody.velocity = Vector3.zero;

        if (initTarzanFlg)
        {
			// フックの真下へ移動
			playerPath.SetInput((subCharaPos.position.x - transform.position.x), moveSpeed * 2);

			if (isLookFront)
			{
				isSwingFront = true;
			}
			else
			{
				isSwingFront = false;
			}
        }
        else
        {
			Tarzan();
		}

		// フックの真下に来たらフラグを折る
		if (subCharaPos.position.x + 0.5 > transform.position.x &&
			subCharaPos.position.x - 0.5 < transform.position.x && 
			initTarzanFlg
			)
		{
            countX = 0; // カウントリセット
            countY = 0;
            initTarzanFlg = false;
            high = true;

            tarzanDistY = Vector3.Distance(subCharaPos.transform.position, transform.position);
            tarzanHigh = transform.position;
        }

        // フックを離した
        if (!isSubScissor)
        {
            state = State.eNormal;
			Jump();
		}
#else
        if (!isPendulum)
        {
            //振り子初期化
            PendulumSetting();
            isPendulum = true;
        }
        Pendulum();

        //振り子の終了
        if (!isSubScissor)
        {
            isPendulum = false;
            rigidBody.useGravity = true;
            subCharaRig.useGravity = true;
            subCharaRig.isKinematic = false;
            state = State.eBlowAway;
        }

#endif
    }

    /*! @brief 振子運動による吹き飛び*/
    void BlowAway()
    {
        Vector3 newPos = transform.position - (transform.up * 3.5f);
        if (Physics.Linecast(transform.position, newPos, groundLayer))
            state = State.eNormal;
    }

    #region　振子運動
    /*! @brief 振子運動の設定*/
    void PendulumSetting()
    {
#if true

        Vector3 vec = transform.position - prePos;
        Debug.Log(vec);
        vec.y = 0f;
        float x = Mathf.Abs(vec.x);
        float z = Mathf.Abs(vec.z);
        if(x>=z)
        {
            pendulumDirX = true;    
        }
        else
        {
            pendulumDirX = false;
        }

        radius = Vector3.Distance(transform.position, subCharaPos.position);
        maxRadius = radius;
        Vector3 targetDir = transform.position - subCharaPos.position;
        //Vector3 dir = Vector3.right;
        //if (!pendulumDirX)
        //{
        //    dir = Vector3.forward;
        //}
        Vector3 dir = Vector3.Normalize(vec);
        swingAngle = Vector3.Angle(targetDir, dir);
        Debug.Log(swingAngle);
        //Debug.Log(targetDir);
        //if ((swingAngle >= 90f && targetDir.x <= targetDir.z)
        //    || (swingAngle < 90f && targetDir.x >= targetDir.z))
        //{
        //    pendulumDirX = true;
        //}
        //else
        //{
        //    pendulumDirX = false;
        //}

        addAccele = 0;
        accelSpeed = firstAcceleSpeed;
        rigidBody.useGravity = false;
        subCharaRig.useGravity = false;
        subCharaRig.isKinematic = true;
#else
        radius = Vector3.Distance(transform.localPosition, subCharaPos.localPosition);
        maxRadius = radius;
        Vector3 targetDir = transform.localPosition - subCharaPos.localPosition;
        Vector3 right = (subCharaPos.localPosition + Vector3.right) - subCharaPos.localPosition;
        swingAngle = Vector3.Angle(targetDir, right);
        addAccele = 0;
        accelSpeed = firstAcceleSpeed;
        rigidBody.useGravity = false;
        subCharaRig.useGravity = false;
        subCharaRig.isKinematic = true;
#endif
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
#if true
        //支点
        fulcrum = subCharaPos.position;
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
        newAngleDeg = rad;
        rad = swingAngle * Mathf.Deg2Rad;
        px = fulcrum.x + Mathf.Cos(rad) * radius;
        py = (fulcrum.y + Mathf.Sin(rad) * radius) * -1f;
         pz = fulcrum.z + Mathf.Cos(rad) * radius;
        //Debug.Log("Cos:" + (Mathf.Cos(rad)));
        //Debug.Log("rad:"+(Mathf.Cos(rad) * radius));
        // Debug.Log("px:" + px);

        //高さは振子運動を使用
        Vector3 tempPos = transform.position;
        tempPos.y = py;
        transform.position = tempPos;
        Vector3 dir = fulcrum - transform.position;

        //水平軸はパスに添わせる
        Vector3 vec = new Vector3(px, 0f, pz) - transform.position;
        float diff = (pendulumDirX) ? vec.x : vec.z;
        const float pendulumMag = 10f;
        //Debug.Log("diff: "+diff);
        float dm = Vector3.Magnitude(new Vector3(px, 0f, pz));
        float tm = Vector3.Magnitude(new Vector3(transform.position.x, 0f, transform.position.z));
        //Debug.Log(dm-tm);
        playerPath.SetInput(1f,diff* pendulumMag);
#endif
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
    #endregion 

    /*! @brief   メインキャラクターの移動*/
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
        cursor.SetActive(isCarry);

        //キャラクターの中央から足元にかけて、接地判定用のラインを引く
        Vector3 newPos = transform.position - (transform.up * 3.5f);

        isAbleJump = Physics.Linecast(transform.position, newPos, groundLayer); //Linecastが判定するレイヤー 

        Debug.DrawLine(transform.position, newPos, Color.cyan);

        // サブキャラを持ち上げる
        if (isCarry)
        {
            animator.SetBool("isScissorsBack", true);
            nearGimmick.transform.position = gameObject.transform.FindChild("Alli").transform.position;
        }

        // ターザン
        if (SubCharacterController.subScissor != null)
        {
            if (SubCharacterController.subScissor.transform.tag == "Hook" && Ltrg > 0.8f && Stick.y > 0.8f)
            {
                isSubScissor = true;
                initTarzanFlg = true;
                //HookShot();
            }
        }
        if (isSubScissor)
        {
            state = State.eAction;
        }
    }

    /*! @brief   コントローラーの入力*/
    void InputController()
    {
        Stick = GamePad.GetLeftStickAxis(false);
        Ltrg = GamePad.GetTrigger(GamePad.Trigger.LeftTrigger, false);
        Rtrg = GamePad.GetTrigger(GamePad.Trigger.RightTrigger, false);
    }

    /*! @brief   アニメーション管理*/
    void Motion()
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
            moveSpeed = 2;
        }
        else
        {
            animator.SetBool("isDash", false);
            moveSpeed = 1;
        }
        #endregion

    }

    /*! @brief   ジャンプ*/
    void Jump()
    {

        animator.SetBool("isNormalJump", true);

        rigidBody.AddForce(Vector3.up * jumpPower);
        isAbleJump = false;
    }

    /*! @brief   フックショット*/
    void HookShot()
    {
        Debug.Log("フックショット発動中");
        if (HookShotInitFlg == true)
        {
            DistX = subCharaPos.transform.position.x - transform.position.x;
            DistY = subCharaPos.transform.position.y - transform.position.y;
            //Debug.Log(DistX);
            //Debug.Log(DistY);

            HookShotInitFlg = false;
        }
        else
        {
            // 重力オフ
            rigidBody.velocity = Vector3.zero;
            //二点の距離を算出
            dist = Vector3.Distance(subCharaPos.transform.position, transform.position);

            if ((subCharaPos.transform.position.x - transform.position.x) <= hookShotRange &&
                (subCharaPos.transform.position.x - transform.position.x) >= hookShotRange * -1)
            {
                dir = 0;
            }
            else if ((subCharaPos.transform.position.x - transform.position.x) > hookShotRange)
            {
                dir = 1;
            }
            else if ((subCharaPos.transform.position.x - transform.position.x) < hookShotRange * -1)
            {
                dir = -1;
            }

            // 目標へ移動
            //x軸移動
            playerPath.SetInput(dir, rollUpPower);

            //y軸移動
            Vector3 newPos = transform.position;
            newPos.y = Mathf.MoveTowards(transform.position.y, subCharaPos.position.y, DistY / ((((playerPath.GetTimePerSegment()) * rollUpPower) * DistY) / DistX));
            transform.position = newPos;
        }
    }
#if true
    /*! @brief   ターザン*/
    void Tarzan()
    {
        // ターザン
        int dir;
        if (isSwingFront)
        {
            countX += 0.04f;
        }
        else
        {
            countX -= 0.04f;
        }

        nowTime = Mathf.Sin(countX);

        // メインキャラとサブキャラの二点の距離間を保存
        tarzanDistX = subCharaPos.position.x - transform.position.x;
        tarzanDistZ = subCharaPos.position.z - transform.position.z;

        // 1フレーム前の数値の比較
        if (nowTime >= afterTime)
        {
            isLookFront = true;
            dir = 1;
        }
        else
        {
            isLookFront = false;
            dir = -1;
        }

        // 縦移動

        //  ２点間の角度を求める
        Vector2 abst = subCharaPos.position - tarzanHigh;
        float toAngle = -Mathf.Atan2(abst.y, abst.x);

        transform.position = subCharaPos.position + new Vector3(dist * Mathf.Cos(toAngle), dist * Mathf.Sin(toAngle));

        #region 振り子運動
        //if (Mathf.Sin(countX) < 0)
        //{
        //    tarzanHigh.y = transform.position.y + (tarzanDistY) * Mathf.Sin(countX) / 5.5f;

        //}
        //else
        //{
        //    tarzanHigh.y = transform.position.y - (tarzanDistY) * Mathf.Sin(countX) / 5.5f;
        //}

        //tarzanHigh.y = transform.position.y + (tarzanDistX * Mathf.Sin(countX)) / 30 * -1;
        //tarzanHigh.y = (subCharaPos.position.y - (tarzanDistX) * Mathf.Sin(countX)) - subCharaPos.position.y / 2;
        #endregion

        // 横移動
        //playerPath.SetInput(dir, 2);

        //Debug.Log(Vector2.Distance(subCharaPos.transform.position, transform.position));

        // 更新
        afterTime = nowTime;
    }
#endif

    /*! @brief 回転補整*/
    void NormalRotation()
    {
        Vector3 rot = transform.FindChild("body").gameObject.transform.localEulerAngles;

        transform.LookAt(transform.position + playerPath.GetAddPotision());

        // 正面から角度を加減して進行方向へ向く

        //キャラの注視方向
        if (Stick.x > 0.01 && !isLookFront)
        {
            isLookFront = true;
        }
        if (Stick.x < -0.01 && isLookFront)
        {
            isLookFront = false;
        }
        if (isLookFront)
        {
            rot.y = lookAngle;
        }
        else
        {
            rot.y = lookAngle * -1;
        }

        transform.FindChild("body").gameObject.transform.localRotation = Quaternion.Euler(rot);
    }

    /*! @brief   メインプレイヤーからカーソルへの角度算出して投げる*/
    void ThrowAim(Vector3 player, Vector3 cursor)
    {
        throwAngle = cursor - player;
        subCharaRig.AddForce(throwAngle * throwPower);
    }

    /*! @brief 自身が振り子の支点になる*/
    void Hook()
    {

    }

    /*! @brief 衝突した瞬間検知*/
    void ChildOnTriggerEnter(Collider col)
    {

    }

    /*! @brief   プレイヤーが衝突した*/
    void ChildOnTriggerStay(Collider col)
    {
        mainScissor = col;

        //if (col.transform.tag == "SubPlayer")
        //{
        //    Debug.Log("Rで持てる");
        //}

        //触れているギミック取得
        if (mainScissor.transform.tag == "SubPlayer" && Rtrg > 0.8)
        {
            nearGimmick = mainScissor.gameObject;
            nearGimmickPos = transform.position * -2f;
            isCarry = true;
            subCharaRig.velocity = Vector3.zero;
        }
        //ギミックを離す
        else if (Rtrg < 0.2 && nearGimmick != null)
        {
            isCarry = false;
            nearGimmickPos = transform.position * -2f;
            nearGimmick = null;
            HookShotInitFlg = true;

            // サブキャラを投げる
            ThrowAim(transform.position, cursor.transform.position);
            animator.SetBool("isScissorsBack", false);
            animator.SetBool("isLeave", true);

            isSubScissor = false;
        }
    }

    /*! @brief   Clip部分が衝突していない*/
    void ChildOnTriggerExit(Collider col)
    {
        mainScissor = col;
        //触れていたオブジェクトがギミックの時
        if (mainScissor.transform.tag == "SubPlayer")
        {
            //Debug.Log("離れた");
        }
    }

}
