/*!
 *  @file           SubCharacterController.cs
 *  @brief         サブキャラ操作処理
 *  @date         2017/04/12
 *  @author      金澤信芳
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InputGamePad;

/*! @brief サブキャラクター管理クラス*/
public class SubCharacterController : MonoBehaviour
{
    public float jumpPower = 0f;                         /*! ジャンプ力*/
    public float beThrownPower;                       /*! 投げられる力*/
    public Transform partnerPos;                       /*! 他プレイヤーの位置*/
    public Rigidbody partnerRig;                        /*! 他プレイヤーの当たり判定*/
    public Animator animator;                             /*! アニメーター*/
    public float subPlayerStopPos = 0f;             /*! サブキャラが止まる距離*/
    public float subPlayerDistance = 0f;            /*! サブキャラの近づく距離*/
    public static Collider subScissor;                   /*! 挟むよう当たり判定*/
    public Camera mainCamera;                           /*! カメラ*/
    public CameraWork camerawork;                 /*! カメラワーク管理クラス*/
    public MainCharacterController player;      /*! メインキャラクター管理クラス*/
    public Transform mainLeftHand;                  /*! メインキャラクターの左手*/
    public Transform mainRightHand;               /*! メインキャラクターの右手*/
    public float lookAngle = 0f;                            /*! サブキャラの向く角度*/
    
    private GameObject nearGimmick = null;  /*! ギミック*/
    private float Ltrg;                                              /*! 左トリガー*/
    private Vector3 nearGimmickPos;                /*! 最も近いギミックの座標*/
    private Vector3 throwAngle;                         /*! メインプレイヤーとカーソルの角度*/
    private Rigidbody rigidBody;                         /*! リジッドボディ*/
    private Vector3 rot;                                          /*! 向いている方向*/
    private GameObject cursor;                          /*! カーソル取得*/

    private enum State         /*! サブキャラクターの状態*/
    {
        eFollow,                          //ついていく
        eFastMove,                    //メインキャラクターの元へ瞬時に移動
        eBeCarried,　                //運ばれる
        eBeThrown,　         　//投げられる
        eScissors,                       //鋏む
        eHung,                            //ぶら下がり
        eAim,                               //狙う
    }
    private State state;         /*! サブキャラクターの状態*/

    /*! @brief   初期化*/
    void Start()
    {
        cursor = GameObject.Find("cursor");
        rigidBody = GetComponent<Rigidbody>();
        rot.y = partnerPos.transform.eulerAngles.y + lookAngle;
        transform.localRotation = Quaternion.Euler(rot);

        state = State.eFollow;
        animator = GetComponent<Animator>();
        partnerPos = GameObject.FindGameObjectWithTag("Player").transform;
        mainCamera.depthTextureMode = DepthTextureMode.Depth;
    }

    /*! @brief   物理演算系更新*/
    private void FixedUpdate()
    {
        Move();
    }

    /*! @brief   移動*/
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
            case State.eBeCarried:
                CarryByMainPlayer();
                break;
            case State.eBeThrown:
                BeThrown();
                break;
            case State.eHung:
                HungingMove();
                break;
        }
    }

    /*! @brief プレイヤーの元へ高速移動*/
    void FastMove()
    {
        animator.SetBool("isDash", true);
        transform.position = Vector3.Lerp(transform.position, partnerPos.position, 0.5f);
        if (Vector3.Distance(transform.position, partnerPos.position) < 1f)
        {
            animator.SetBool("isDash", false);
            state = State.eFollow;
        }
    }

    /*! @brief   サブキャラクターの移動*/
    void FollowMove()
    {
        Vector3 mainPlayerPos = partnerPos.position;
        Vector3 Direction = mainPlayerPos - transform.position;
        float Distance = Direction.sqrMagnitude;
        Direction.y = 0f;

        //回転
        FollowRotation();

        //メインプレイヤーのキャラが一定以上でなければ、サブキャラは近寄らない
        if (Distance >= subPlayerDistance + subPlayerStopPos)
        {
            // 目的地を算出
            Vector3 targetPos = partnerPos.TransformPoint(new Vector3(0.5f, -1f, -1.0f));
            Vector3 velocity = Vector3.zero;

            // 移動
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, 0.2f);
            animator.SetBool("isWalk", true);
        }
        else
        {
            animator.SetBool("isWalk", false);
        }

        if (GamePad.GetLeftStickAxis(false, GamePad.Stick.AxisY) > 0)
        {
            state = State.eFastMove;
        }
    }

    /*! @brief 通常移動の向き*/
    void FollowRotation()
    {
        Vector3 lookatPos = partnerPos.position + camerawork.GetCameraVec();
        lookatPos.y = transform.position.y;
        transform.LookAt(lookatPos);
    }

    /*! @brief サブプレイヤーのステートを運ばれるに変更*/
    public void SetStateBeCarried()
    {
        state = State.eBeCarried;
    }

    /*! @brief メインプレイヤーに運ばれる*/
    void CarryByMainPlayer()
    {
        rigidBody.velocity = Vector3.zero;

        //位置
        Vector3 pos = (mainLeftHand.position + mainRightHand.position) / 2;
        transform.position = pos;

        //回転
        rot = transform.localEulerAngles;
        if (MainCharacterController.isLookFront)
        {
            rot.y = partnerPos.transform.eulerAngles.y + lookAngle;
        }
        else
        {
            rot.y = partnerPos.transform.eulerAngles.y - lookAngle;
        }
        transform.localRotation = Quaternion.Euler(rot);

        animator.SetBool("isScissorUp", true);
        
        rigidBody.useGravity = !player.isSublayerCarry;
        if (!player.isSublayerCarry)
        {
            state = State.eBeThrown;
        }
    }

    /*! @brief 投げられる*/
    void BeThrown()
    {
        throwAngle = cursor.GetComponent<CursorMove>().throwPos- transform.position;
        rigidBody.AddForce(throwAngle * beThrownPower);
        state = State.eFollow;
    }

    /*! @brief ぶら下がりにおける移動*/
    void HungingMove()
    {
        if (Ltrg < 0.2 && nearGimmick != null)
        {
            MainCharacterController.isSubScissor = false;
            nearGimmick = null;
            state = State.eFollow;
            return;
        }

        rigidBody.velocity = Vector3.zero;
        Vector3 pos = nearGimmickPos;
        pos.y -= (transform.localScale.y + nearGimmick.transform.localScale.y);
        transform.position = pos;
        Vector3 lookat = transform.position + camerawork.GetCameraVec();
        transform.LookAt(lookat);
    }
    
    /*! @brief 衝突した瞬間検知*/
    void ChildOnTriggerEnter(Collider col)
    {
        animator.SetBool("isScissorUp", false);
    }

    /*! @brief   衝突の継続を検知*/
    void ChildOnTriggerStay(Collider col)
    {
        subScissor = col;
        //触れているギミック取得
        if (state != State.eHung && col.transform.tag == "Hook" && Ltrg > 0.2)
        {
            nearGimmick = subScissor.gameObject;
            nearGimmickPos = nearGimmick.transform.position;
            state = State.eHung;
        }
        else if (state != State.eHung && col.transform.tag == "Goal" && Ltrg > 0.2)
        {
            nearGimmick = subScissor.gameObject;
            nearGimmickPos = transform.position;
            
            state = State.eHung;
            mainCamera.depth = -5; // ゴールのカメラに切り替え
            GameObject.Find("SceneManager").GetComponent<SceneControl>().AddClearScene();
        }
    }

    /*! @brief 衝突から離れたのを検知*/
    void ChildOnTriggerExit(Collider col) { }
}