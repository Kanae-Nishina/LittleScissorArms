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
    public float beThrownPower;                       /*! 投げられる力*/
    public float moveStopDist = 0f;             /*! サブキャラが止まる距離*/
    public GameObject mainChara;            /*! メインキャラクター*/
    [System.NonSerialized]
    public static bool isScissor;           /*! 鋏み*/

    [SerializeField]
    private Transform mainLeftHand;                  /*! メインキャラクターの左手*/
    [SerializeField]
    private Transform mainRightHand;               /*! メインキャラクターの右手*/
    private Transform mainCharaTrans;                       /*! 他プレイヤーの位置*/
    private MainCharacterController mainCharaController;      /*! メインキャラクター管理クラス*/
    private CameraWork camerawork;                 /*! カメラワーク管理クラス*/
    private GameObject nearGimmick = null;  /*! ギミック*/
    private Vector3 nearGimmickPos;                /*! 最も近いギミックの座標*/
    private float leftTrigger;                                              /*! 左トリガー*/
    private Rigidbody rigidBody;                         /*! リジッドボディ*/
    private Animator animator;                             /*! アニメーター*/
                                                           // private Vector3 rot;                                          /*! 向いている方向*/
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
        camerawork = GameObject.Find("CameraWork").GetComponent<CameraWork>();
        rigidBody = GetComponent<Rigidbody>();

        state = State.eFollow;
        animator = GetComponent<Animator>();
        mainCharaTrans = mainChara.transform;
        mainCharaController = mainChara.GetComponent<MainCharacterController>();
    }

    /*! @brief   物理演算系更新*/
    private void FixedUpdate()
    {
        Move();
    }

    /*! @brief   移動*/
    void Move()
    {
        leftTrigger = GamePad.GetTrigger(GamePad.Trigger.LeftTrigger, false);

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
        transform.position = Vector3.Lerp(transform.position, mainCharaTrans.position, 0.5f);
        if (Vector3.Distance(transform.position, mainCharaTrans.position) < 1f)
        {
            animator.SetBool("isDash", false);
            state = State.eFollow;
        }
    }

    /*! @brief   サブキャラクターの移動*/
    void FollowMove()
    {
        Vector3 dir = mainCharaTrans.position - transform.position;
        float dist = dir.sqrMagnitude;

        //メインプレイヤーのキャラが一定以上でなければ、サブキャラは近寄らない
        if (dist >= moveStopDist * moveStopDist)
        {
            // 目的地を算出
            Vector3 targetPos = mainCharaTrans.TransformPoint(new Vector3(0.5f, -1f, -1.0f));
            Vector3 velocity = Vector3.zero;

            // 移動
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, 0.2f);
            animator.SetBool("isWalk", true);
        }
        else
        {
            animator.SetBool("isWalk", false);
        }

        //回転
        FollowRotation();

        if (GamePad.GetLeftStickAxis(false, GamePad.Stick.AxisY) > 0)
        {
            state = State.eFastMove;
        }
    }

    /*! @brief 通常移動の向き*/
    void FollowRotation()
    {
        Vector3 lookatPos = mainCharaTrans.position + camerawork.GetCameraVec();
        lookatPos.y = transform.position.y;
        transform.LookAt(lookatPos);
    }

    /*! @brief サブプレイヤーのステートを運ばれるに変更*/
    public void SetStateBeCarried()
    {
        animator.SetBool("isWalk", false);
        animator.SetBool("isScissorUp", true);
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
        Vector3 rot = transform.localEulerAngles;
        rot.y = mainCharaTrans.transform.eulerAngles.y;
        transform.localEulerAngles = rot;
        
        rigidBody.useGravity = false;
        if (!mainCharaController.isSublayerCarry)
        {
            state = State.eBeThrown;
        }
    }

    /*! @brief 投げられる*/
    void BeThrown()
    {
        rigidBody.useGravity = true;
        Vector3 throwAngle = cursor.GetComponent<CursorMove>().throwPos - transform.position;
        rigidBody.AddForce(throwAngle * beThrownPower);
        state = State.eFollow;
        animator.SetBool("isScissorUp", false);
    }

    /*! @brief ぶら下がりにおける移動*/
    void HungingMove()
    {
        if (leftTrigger < 0.2 && nearGimmick != null)
        {
            nearGimmick = null;
            isScissor = false;
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
        //animator.SetBool("isScissorUp", false);
    }

    /*! @brief   衝突の継続を検知*/
    void ChildOnTriggerStay(Collider col)
    {
        //触れているギミック取得
        if (state != State.eHung && col.transform.tag == "Hook" && leftTrigger > 0.2)
        {
            nearGimmick = col.gameObject;
            nearGimmickPos = nearGimmick.transform.position;
            state = State.eHung;
            isScissor = true;
        }
        else if (state != State.eHung && col.transform.tag == "Goal" && leftTrigger > 0.2)
        {
            nearGimmick = col.gameObject;
            nearGimmickPos = transform.position;

            state = State.eHung;
            //mainCamera.depth = -5; // ゴールのカメラに切り替え
            GameObject.Find("SceneManager").GetComponent<SceneControl>().AddClearScene();
        }
    }

    /*! @brief 衝突から離れたのを検知*/
    void ChildOnTriggerExit(Collider col) { }
}