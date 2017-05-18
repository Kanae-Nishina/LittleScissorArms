/*
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
    /*public宣言*/
    public float jumpPower = 0f;                      //ジャンプ力
    public float throwPower = 0f;                     //投げる力
    public int rollUpPower = 0;                         //巻き上げる力
    public float lookAngle = 0f;                        //キャラクターの向く角度
    public LayerMask groundLayer;                   //地面のレイヤー
    public Transform subCharaPos;                     //他プレイヤーの位置
    public Collider subCharaCol;                         //他プレイヤーの当たり判定
    public Rigidbody subCharaRig;                     //他プレイヤーの当たり判定
    public Animator animator;
    public PlayerPath playerPath;                     //プレイヤーの移動軌跡
    public static bool isCarry = false;                //メインプレイヤー運んでいるか
    public static bool isLookFront = true;        //前を見ているか
    public static Collider mainScissor;
    public float hookShotRange;

    /*private宣言*/
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

    private enum State
    {
        eNormal = 0,    //通常

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
        subCharaCol.GetComponent<Collider>();

        cursor = GameObject.Find("cursor");

        state = State.eNormal;
        animator = GetComponent<Animator>();
        subCharaPos = GameObject.FindGameObjectWithTag("SubPlayer").transform;
    }

    /* @brief   物理演算系更新*/
    private void FixedUpdate()
    {
        //Action();
        Move();
    }

    /* @brief 更新*/
    private void LateUpdate()
    {
        if (playerPath)
            transform.position += playerPath.GetAddPotision();
    }

    /* @brief   移動*/
    void Move()
    {
        Stick = GamePad.GetLeftStickAxis(false);
        Ltrg = GamePad.GetTrigger(GamePad.Trigger.LeftTrigger, false);
        Rtrg = GamePad.GetTrigger(GamePad.Trigger.RightTrigger, false);

        //注視向き補整
        NormalRotation();

        switch (state)
        {
            case State.eNormal:
                NormalMove();
                break;
        }
    }

    /* @brief   メインキャラクターの移動*/
    void NormalMove()
    {
        //ジャンプ
        if (isAbleJump && GamePad.GetButtonUp(GamePad.Button.Jump))
        {
            animator.SetBool("isNormalJump", true);
            Jump();
        }

        //第一引数、-1～1　第二引数、倍率(早くなる)
        playerPath.SetInput(Stick.x, 1);

        //カーソルの出現フラグ
        cursor.SetActive(isCarry);

        //キャラの注視方向
        if (Stick.x > 0.01 && !isLookFront)
        {
            isLookFront = true;
        }
        if (Stick.x < -0.01 && isLookFront)
        {
            isLookFront = false;
        }

        //キャラクターの中央から足元にかけて、接地判定用のラインを引く
        Vector3 newPos = transform.position - (transform.up * 0.5f);
        isAbleJump = Physics.Linecast(transform.position, newPos, groundLayer); //Linecastが判定するレイヤー 
        Debug.DrawLine(transform.position, newPos, Color.cyan);

        // サブキャラを持ち上げる
        if (isCarry)
        {
            animator.SetBool("isScissorsBack", true);
            nearGimmick.transform.position = gameObject.transform.FindChild("Alli").transform.position;
        }


        if (SubCharacterController.subScissor != null)
        {
            if (SubCharacterController.subScissor.transform.tag == "Hook" && Ltrg > 0.8 && Stick.y > 0.8f)
            {
                HookShot();                
            }
        }
        Debug.Log(SubCharacterController.subScissor.transform.tag);

    }

#if false
    /* @brief 移動の補正処理*/
    void CorrectionMove()
    {
        Vector3 correctionPos = transform.position;
        if (posList[targetNum - 1].x == posList[targetNum].x)
        {
            correctionPos.x = posList[targetNum].x;
        }
        else if (posList[targetNum - 1].z == posList[targetNum].z)
        {
            correctionPos.z = posList[targetNum].z;
        }
        transform.position = correctionPos;
    }

    /* @brief ターゲット座標と方向ベクトルチェック*/
    void CheckTarget(float inputX)
    {
        Vector3 target = posList[targetNum];
        target.y = transform.position.y;
        float rightDist = Vector3.Distance(transform.position, target);
        target = posList[targetNum - 1];
        target.y = transform.position.y;
        float leftDist = Vector3.Distance(transform.position, target);
        if (inputX >= 0 && rightDist <= moveSpeed)
        {
            if (targetNum < posList.Count && nowDirNum < dirList.Count)
            {
                ++nowDirNum;
                ++targetNum;
            }
        }
        else if (inputX < 0 && leftDist <= moveSpeed)
        {
            if (targetNum > 0 && nowDirNum > 0)
            {
                --nowDirNum;
                --targetNum;
            }
        }
    }
#endif

    /* @brief   ジャンプ*/
    void Jump()
    {
        rigidBody.AddForce(Vector3.up * jumpPower);
        isAbleJump = false;
    }

    /* @brief   フックショット*/
    void HookShot()
    {
        Debug.Log("フックショット発動中");

        // 重力オフ
        rigidBody.velocity = Vector3.zero;

        //二点の距離を算出
        float dist = Vector3.Distance(subCharaPos.transform.position, transform.position);

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

        playerPath.SetInput(dir, rollUpPower);
        //Debug.Log(subCharaPos.transform.position.x - transform.position.x);
        //Debug.Log(dir);

        // 目標へ移動
        //transform.position = Vector3.MoveTowards(transform.position, subCharaPos.transform.position, Time.deltaTime * rollUpPower);
    }

    /* @brief 回転補整*/
    void NormalRotation()
    {
        Vector3 rot = transform.FindChild("body").gameObject.transform.eulerAngles;
        // 正面から角度を加減して進行方向へ向く
        if (isLookFront)
        {
            rot.y = lookAngle;
        }
        else
        {
            rot.y = lookAngle * -1;
        }

        transform.FindChild("body").gameObject.transform.rotation = Quaternion.Euler(rot);
    }

    /* @brief   メインプレイヤーからカーソルへの角度算出して投げる*/
    void ThrowAim(Vector3 player, Vector3 cursor)
    {
        throwAngle.x = cursor.x - player.x;
        throwAngle.y = cursor.y - player.y;
        subCharaRig.AddForce(throwAngle * throwPower);
    }

    /* @brief   プレイヤーが衝突した*/
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

            // サブキャラを投げる
            ThrowAim(transform.position, cursor.transform.position);
            animator.SetBool("isScissorsBack", false);
            animator.SetBool("isLeave", true);
        }

        //歩くモーション
        if (Stick.x != 0f)
        {
            animator.SetBool("isWalk", true);
        }
        else if (Stick.x == 0f)
        {
            animator.SetBool("isWalk", false);
        }

        //フックショット
        //if (SubCharacterController.subScissor.transform.tag != null)
        //{
        //    if (SubCharacterController.subScissor.transform.tag == "Hook" && Ltrg > 0.8 && Stick.y > 0.8f)
        //    {
        //        HookShot();

        //        //方向ベクトル算出
        //        Vector3 vec = (subCharaPos.transform.position - transform.position).normalized;
        //        playerPath.SetInput(vec.x, 5);

        //        //Debug.Log(Vector3.Distance(transform.position, subCharaPos.transform.position));
        //    }
        //}
    }

    /* @brief   Clip部分が衝突していない*/
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
