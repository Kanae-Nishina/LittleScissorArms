///*
// *  @file           PlayersMove.cs
// *  @brief         プレイヤー移動処理
// *  @date         2017/04/12
// *  @author      金澤信芳
// */

//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using InputGamePad;

//public class PlayersMove : MonoBehaviour
//{
//    /*public宣言*/
//    //public int firstNum;                                     //最初にいる座標番号
//    //public float moveSpeed = 0f;                      //移動速度
//    public float jumpPower = 0f;                      //ジャンプ力
//    public float throwPower = 0f;                     //投げる力
//    public float rollUpPower = 0f;                     //巻き上げる力
//    public float lookAngle = 0f;                        //キャラクターの向く角度
//    public LayerMask groundLayer;                   //地面のレイヤー
//    public bool isMainPlayer = true;                 //メインキャラクターか(falseの場合サブキャラ)
//    public Transform partnerPos;                     //他プレイヤーの位置
//    public Collider partnerCol;                         //他プレイヤーの当たり判定
//    public Rigidbody partnerRig;                     //他プレイヤーの当たり判定
//    public Animator animatorMainPlayer;
//    public Animator animatorSubPlayer;


//    public float subPlayerStopPos = 0f;             //サブキャラが止まる距離
//    public float subPlayerDistance = 0f;            //サブキャラの近づく距離
//    public PlayerPath playerPath;                     //プレイヤーの移動軌跡

//    public static bool isMainPlayerCarry = false;                //メインプレイヤー運んでいるか
//    public static bool isSubPlayerCarry = false;                //サブプレイヤー運んでいるか

//    public static bool isLookFront = true;        //前を見ているか


//    /*private宣言*/
//    private GameObject nearGimmick = null;      //ギミック

//    //private bool isGimmick = false;                 //ギミックに触れているかどうか
//    private bool isAbleJump = true;                 //ジャンプ可能フラグ
//    //private List<Vector3> posList;                  //軌跡座標のリスト
//    //private List<Vector3> dirList;                   //軌跡による方向ベクトルのリスト
//    //private int nowDirNum = 0;                     //現在の方向ベクトル
//    //private int targetNum = 0;                       //現在目指してる座標番号
//    private Vector2 Stick;                              //左スティックの入力値
//    private float Ltrg;
//    private float Rtrg;
//    private Vector3 nearGimmickPos;                //最も近いギミックの座標
//    private Vector3 throwAngle;                     //メインプレイヤーとカーソルの角度
//    private Rigidbody rigidBody;
//    private GameObject cursor;                      //カーソル取得
//    private static Collider mainScissor;
//    private static Collider subScissor;

//    private enum State
//    {
//        eNormal = 0,    //通常
//        eFollow,            //ついていく

//        // イベントステート
//        eScissors,          //鋏む
//        eHung,              //ぶら下がり
//        eAim,                //狙う
//    }
//    State state;

//    /* @brief   初期化*/
//    void Start ()
//    {
//        rigidBody = GetComponent<Rigidbody>();
//        partnerCol.GetComponent<Collider>();

//        cursor = GameObject.Find("cursor");

//        if (isMainPlayer)
//        {
//            state = State.eNormal;
//            animatorMainPlayer = GetComponent<Animator>();
//            partnerPos = GameObject.FindGameObjectWithTag("SubPlayer").transform;
//        }
//        else
//        {
//            state = State.eFollow;
//            animatorSubPlayer = GetComponent<Animator>();
//            partnerPos = GameObject.FindGameObjectWithTag("Player").transform;
//        }
//    }

//    /* @brief   物理演算系更新*/
//    private void FixedUpdate()
//    {
//        //Action();
//        Move();
//    }

//    /* @brief 更新*/
//    private void LateUpdate()
//    {
//        if (playerPath)
//            transform.position += playerPath.GetAddPotision();
//    }

//    /* @brief   移動*/
//    void Move()
//    {
//        Stick = GamePad.GetLeftStickAxis(false);
//        Ltrg = GamePad.GetTrigger(GamePad.Trigger.LeftTrigger, false);
//        Rtrg = GamePad.GetTrigger(GamePad.Trigger.RightTrigger, false);

//        switch (state)
//        {
//            case State.eNormal:
//                NormalMove();

//                //注視向き補整
//                NormalRotation();
//                break;
//            case State.eFollow:
//                FollowMove();
//                break;
//        }
//    }

//    /* @brief   メインキャラクターの移動*/
//    void NormalMove()
//    {
//        //ジャンプ
//        if (isAbleJump && GamePad.GetButtonUp(GamePad.Button.Jump))
//        {
//            animatorMainPlayer.SetBool("isNormalJump", true);
//            Jump();
//        }

//        //第一引数、-1～1　第二引数、倍率(早くなる)
//        playerPath.SetInput(Stick.x, 1);

//        //カーソルの出現フラグ
//        cursor.SetActive(isMainPlayerCarry);

//        //フックショット
//        if (/*col.transform.tag == "Hook" && */Ltrg > 0.8 && Stick.y > 0.8f)
//        {
//            if (playerPath != null)
//            {
//                Vector3 vec = (partnerPos.transform.position - transform.position).normalized;
//                playerPath.SetInput(vec.x, 5);
//            }

//            if (Vector3.Distance(transform.position, partnerPos.transform.position)> 10)
//            {
//                Debug.Log(Vector3.Distance(transform.position, partnerPos.transform.position));
//                HookShot();
//            }
//        }

//        //キャラの注視方向
//        if (Stick.x > 0.5 && !isLookFront)
//        {
//            isLookFront = true;
//        }
//        if (Stick.x < -0.5 && isLookFront)
//        {
//            isLookFront = false;
//        }

//        //キャラクターの中央から足元にかけて、接地判定用のラインを引く
//        Vector3 newPos = transform.position - (transform.up * 0.5f);
//        isAbleJump = Physics.Linecast(transform.position, newPos, groundLayer); //Linecastが判定するレイヤー 
//        Debug.DrawLine(transform.position, newPos, Color.cyan);

//        // サブキャラを持ち上げる
//        if (isMainPlayerCarry && isMainPlayer)
//        {
//            animatorMainPlayer.SetBool("isScissorsBack", true);
//            nearGimmick.transform.position = gameObject.transform.FindChild("Alli").transform.position;
//        }

//        //左右移動
//        //transform.position += playerPath.GetAddPotision(Stick.x);
//    }

//    /* @brief   サブキャラクターの移動*/
//    void FollowMove()
//    {
//        Vector3 mainPlayerPos = partnerPos.position;
//        Vector3 Direction = mainPlayerPos - transform.position;
//        float Distance = Direction.sqrMagnitude;
//        Direction.y = 0f;

//        // オブジェクトをはさむ
//        if (isSubPlayerCarry && !isMainPlayer)
//        {
//            rigidBody.velocity = Vector3.zero;
//            transform.position = nearGimmick.transform.position;
//        }

//        //メインプレイヤーのキャラが一定以上でなければ、サブキャラは近寄らない
//        if (Distance >= subPlayerDistance + subPlayerStopPos)
//        {
//            // 目的地を算出
//            Vector3 targetPos = partnerPos.TransformPoint(new Vector3(0.5f, 1.0f, -1.0f));
//            Vector3 velocity = Vector3.zero;

//            // 移動
//            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, 0.2f);
//            //animatorSubPlayer.SetBool("isWalk", true);
//        }
//        //else if (Distance > subPlayerDistance + subPlayerStopPos + 2f)
//        //{
//        //    animatorSubPlayer.SetBool("isJump", true);
//        //    Jump(); // あぁ^～心がぴょんぴょんするんじゃぁ^～
//        //}
//        else
//        {
//            //animatorSubPlayer.SetBool("isWalk", false);
//        }
//    }

//#if false
//    /* @brief 移動の補正処理*/
//    void CorrectionMove()
//    {
//        Vector3 correctionPos = transform.position;
//        if (posList[targetNum - 1].x == posList[targetNum].x)
//        {
//            correctionPos.x = posList[targetNum].x;
//        }
//        else if (posList[targetNum - 1].z == posList[targetNum].z)
//        {
//            correctionPos.z = posList[targetNum].z;
//        }
//        transform.position = correctionPos;
//    }

//    /* @brief ターゲット座標と方向ベクトルチェック*/
//    void CheckTarget(float inputX)
//    {
//        Vector3 target = posList[targetNum];
//        target.y = transform.position.y;
//        float rightDist = Vector3.Distance(transform.position, target);
//        target = posList[targetNum - 1];
//        target.y = transform.position.y;
//        float leftDist = Vector3.Distance(transform.position, target);
//        if (inputX >= 0 && rightDist <= moveSpeed)
//        {
//            if (targetNum < posList.Count && nowDirNum < dirList.Count)
//            {
//                ++nowDirNum;
//                ++targetNum;
//            }
//        }
//        else if (inputX < 0 && leftDist <= moveSpeed)
//        {
//            if (targetNum > 0 && nowDirNum > 0)
//            {
//                --nowDirNum;
//                --targetNum;
//            }
//        }
//    }
//#endif

//    /* @brief   ジャンプ*/
//    void Jump()
//    {
//        rigidBody.AddForce(Vector3.up * jumpPower);
//        isAbleJump = false;
//    }

//    /* @brief   フックショット*/
//    void HookShot()
//    {
//        Debug.Log("フックショット発動中");

//        // 重力オフ
//        rigidBody.velocity = Vector3.zero;

//        // 目標へ移動
//        transform.position = Vector3.MoveTowards(transform.position, partnerPos.transform.position, Time.deltaTime * rollUpPower);
//    }

//    /* @brief 回転補整*/
//    void NormalRotation()
//    {
//        //eulurAngleZ = transform.eulerAngles.z;
//        //Vector3 lookPos = transform.position;
//        // lookPos += new Vector3(1 * dirList[nowDirNum].z, 0f, -1 * dirList[nowDirNum].x);
//        //transform.LookAt(lookPos);

//        Vector3 rot = transform.FindChild("body").gameObject.transform.eulerAngles;
//        // 正面から角度を加減して進行方向へ向く
//        if (isLookFront)
//        {
//            rot.y = lookAngle;
//        }
//        else
//        {
//            rot.y = lookAngle * -1;
//        }

//        transform.FindChild("body").gameObject.transform.rotation = Quaternion.Euler(rot);
//    }

//    /* @brief   メインプレイヤーからカーソルへの角度算出して投げる*/
//    void ThrowAim(Vector3 player, Vector3 cursor)
//    {
//        throwAngle.x = cursor.x - player.x;
//        throwAngle.y = cursor.y - player.y;
//        partnerRig.AddForce(throwAngle * throwPower);
//    }

//    /* @brief   プレイヤーが衝突した*/
//    void ChildOnTriggerStay(Collider col)
//    {
//        if (state == State.eNormal)
//        {
//            //if (col.transform.tag == "SubPlayer")
//            //{
//            //    Debug.Log("Rで持てる");
//            //}

//            //触れているギミック取得
//            if (col.transform.tag == "SubPlayer" && Rtrg > 0.8)
//            {
//                nearGimmick = col.gameObject;
//                nearGimmickPos = transform.position * -2f;
//                isMainPlayerCarry = true;
//                partnerRig.velocity = Vector3.zero;
//                //scissorsPos = transform.position;
//                //hungDist = Vector3.Distance(transform.position, nearGimmickPos);
//            }
//            //ギミックを離す
//            else if (Rtrg < 0.2 && nearGimmick != null)
//            {
//                isMainPlayerCarry = false;
//                nearGimmickPos = transform.position * -2f;
//                nearGimmick = null;

//                // サブキャラを投げる
//                ThrowAim(transform.position, cursor.transform.position);
//                animatorMainPlayer.SetBool("isScissorsBack", false);
//                animatorMainPlayer.SetBool("isLeave",true);
//            }

//            //歩くモーション
//            if (Stick.x > 0.5 || Stick.x < -0.5)
//            {
//                animatorMainPlayer.SetBool("isWalk", true);
//            }
//            else if (Stick.x < 0.2 || Stick.x > -0.2)
//            {
//                animatorMainPlayer.SetBool("isWalk", false);
//            }
            
//        }
//        else if (state == State.eFollow)
//        {
//            //触れているギミック取得
//            if (col.transform.tag == "Hook" && Ltrg > 0.8)
//            {
//                nearGimmick = col.gameObject;
//                nearGimmickPos = nearGimmick.transform.position;

//                if (!isSubPlayerCarry)
//                {
//                    //gameObject.AddComponent<FixedJoint>();
//                }
//                isSubPlayerCarry = true;

//            }

//            //ギミックを離す
//            else if (Ltrg < 0.2 && nearGimmick != null)
//            {
//                isSubPlayerCarry = false;
//                nearGimmickPos = transform.position * -2f;
//                nearGimmick = null;
//                //Destroy(gameObject.AddComponent<FixedJoint>())
//            }
//        }
//    }

//    /* @brief   Clip部分が衝突していない*/
//    void ChildOnTriggerExit(Collider col)
//    {
//        //触れていたオブジェクトがギミックの時
//        if (col.transform.tag == "SubPlayer")
//        {
//            //Debug.Log("離れた");
//        }
//    }

//}
