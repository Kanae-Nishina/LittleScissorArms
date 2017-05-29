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


    private enum State
    {
        eFollow,            //ついていく

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
        }
    }

    /* @brief   サブキャラクターの移動*/
    void FollowMove()
    {
        Vector3 mainPlayerPos = partnerPos.position;
        Vector3 Direction = mainPlayerPos - transform.position;
        float Distance = Direction.sqrMagnitude;
        Direction.y = 0f;

        // オブジェクトをはさむ
        if (isSubPlayerCarry)
        {
            rigidBody.velocity = Vector3.zero;
            transform.position = nearGimmick.transform.position;
        }

        //メインプレイヤーのキャラが一定以上でなければ、サブキャラは近寄らない
        if (Distance >= subPlayerDistance + subPlayerStopPos)
        {
            // 目的地を算出
            Vector3 targetPos = partnerPos.TransformPoint(new Vector3(0.5f, 1.0f, -1.0f));
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
    }

    /* @brief   ジャンプ*/
    void Jump()
    {
        rigidBody.AddForce(Vector3.up * jumpPower);
        isAbleJump = false;
    }

    /* @brief   プレイヤーが衝突した*/
    void ChildOnTriggerStay(Collider col)
    {
        subScissor = col;

        //触れているギミック取得
        if (subScissor.transform.tag == "Hook" && Ltrg > 0.8)
        {
            nearGimmick = subScissor.gameObject;
            nearGimmickPos = nearGimmick.transform.position;

            isSubPlayerCarry = true;
        }

        //ギミックを離す
        else if (Ltrg < 0.2 && nearGimmick != null)
        {
            MainCharacterController.isSubScissor = false;
            isSubPlayerCarry = false;
            nearGimmickPos = transform.position * -2f;
            nearGimmick = null;
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
