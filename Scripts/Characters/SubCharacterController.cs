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
    public float jumpPower=500f;                /*! ジャンプ力*/
    public float beThrownPower;                       /*! 投げられる力*/
    public float moveStopDist = 1f;             /*! サブキャラが止まる距離*/
    public float normalMoveSpeed = 1f;                /*! 通常移動速度*/
    public float dashSpeed = 1f;                /*! ダッシュ移動速度*/
    public float followOffsetY;                 /*! 追従処理の*/
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
    private bool leftTrigger;                                              /*! 左トリガー*/
    private float moveSpeed;                            /*! 移動速度*/
    private Rigidbody rigidBody;                         /*! リジッドボディ*/
    private Animator animator;                             /*! アニメーター*/
                                                           // private Vector3 rot;                                          /*! 向いている方向*/
    private GameObject cursor;                          /*! カーソル取得*/
    private List<Vector3> posRootList;     /*! メインキャラクターの通ったルート座標*/
    private const float betweenPosDist = 4f;  /*! ルート座標間の距離*/

    private enum State         /*! サブキャラクターの状態*/
    {
        eFollow,                          //ついていく
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
        mainCharaTrans = mainChara.transform;
        mainCharaController = mainChara.GetComponent<MainCharacterController>();
        posRootList = new List<Vector3>();
        posRootList.Add(mainCharaTrans.position);
        rigidBody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        state = State.eFollow;
    }

    /*! @brief   物理演算系更新*/
    private void FixedUpdate()
    {
        InputController();
        Move();
    }

    /*! @brief 入力検知*/
    void InputController()
    {
        leftTrigger = GamePad.GetTrigger(GamePad.Trigger.LeftTrigger, false);
    }

    /*! @brief   移動*/
    void Move()
    {
        //ルート更新
        AddRootList();

        switch (state)
        {
            case State.eFollow:
                FollowMove();
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

    /*! @brief   サブキャラクターの移動*/
    void FollowMove()
    {
        //ルートリストの先頭座標が一番古い座標
        Vector3 oldPos = posRootList[0];
        transform.position = Vector3.Lerp(transform.position, oldPos, 0.1f);

        //ルートリストの更新
        RemoveRootList();
#if false
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
#endif

        //回転
        FollowRotation();
    }

    /*! @brief ルートリストに座標追加*/
    void AddRootList()
    {
        //リストの最後が一番新しいルート座標
        Vector3 lastPos = posRootList[posRootList.Count - 1];
        float dist = Vector3.Distance(mainCharaTrans.position, lastPos);
        if (dist >= betweenPosDist)
        {
            //リストに追加
            Vector3 addPos = mainCharaTrans.position;
            addPos.y += followOffsetY;
            posRootList.Add(addPos);
        }
    }

    /*! @brief ルートリストの座標削除*/
    void RemoveRootList()
    {
        //一番古いルートリスト座標に近づいたら削除
        Vector3 oldPos = posRootList[0];
        float dist = Vector3.Distance(transform.position, oldPos);
        if (dist < 1.5f && posRootList.Count > 1)
        {
            posRootList.RemoveAt(0);
        }
    }

    /*! @brief ジャンプ*/
    void Jump()
    {

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
        if (nearGimmick != null)
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

    /*! @brief */

    /*! @brief 衝突した瞬間検知*/
    void ChildOnTriggerEnter(Collider col)
    {
        //animator.SetBool("isScissorUp", false);
    }

    /*! @brief   衝突の継続を検知*/
    void ChildOnTriggerStay(Collider col)
    {
        //触れているギミック取得
        if (col.transform.tag == "Hook")
        {
            nearGimmick = col.gameObject;
            nearGimmickPos = nearGimmick.transform.position;
            state = State.eHung;
        }
        else if (state != State.eHung && col.transform.tag == "Goal")
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