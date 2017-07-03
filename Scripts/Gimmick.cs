/*!
 * @file Gimmick.cs
 * @brief ギミック管理クラス
 * @date 2017/05/25
 * @author 仁科香苗
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/*! @brief ギミック管理クラス*/
public class Gimmick : MonoBehaviour
{
    public bool isGimmick = false;  /*! ギミック発動フラグ*/
    public enum GimmickType        /*! ギミックタイプ*/
    {
        none,                           //なし
        animation,                 //アニメーション
        animator,                   //アニメーター
        blockDistraction,     //積み木破壊
        boat,                            //ボート移動
        drawerMove,            //引き出し移動
    }
    public GimmickType[] type;                           /*! 発動するギミックタイプ*/

    public Animation gimickAnimation;            /*! 再生するアニメーション*/
    public Animator animator;                             /*! 再生するアニメーター*/
    public Vector3[] position;                               /*! 移動する座標*/
    public GameObject battery;                           /*! ボート用電池(運ぶもの)*/
    public MeshRenderer batteryMesh;           /*! ボート用電池のメッシュ(ボートに設置済のもの)*/
    public float moveAbleDist;                             /*! 引き出し用移動可能距離*/

    private MainCharacterController player;     /*! メインキャラクター*/
    private int gimmickNum = 0;                           /*! 現在発動しているギミック番号*/
    private int positionNum = -1;                           /*! 現在の座標番号*/
    private Vector3 startPos;                                 /*! 初期座標*/
    private Vector3 batteryStartPos;                  /*! 電池の初期座標(運ぶもの)*/

    /*! @brief 初期化 */
    private void Start()
    {
        if (gimickAnimation != null)
        {
            gimickAnimation.Stop();
        }
        if (animator != null)
        {
            animator.enabled = false;
        }
        if (batteryMesh != null)
        {
            batteryMesh.enabled = false;
        }
        player = GameObject.Find("chara_newbig").GetComponent<MainCharacterController>();
        startPos = transform.position;
        if (battery)
        {
            startPos = transform.parent.localPosition;
            batteryStartPos = battery.transform.position;
        }
    }

    /*! @brief 更新*/
    private void Update()
    {
        //フラグがたったらギミック発動
        if (isGimmick)
        {
            GimmickInvocation();
        }
    }

    /*! @brief ギミック発動処理*/
    void GimmickInvocation()
    {
        switch (type[gimmickNum])
        {
            case GimmickType.animation:
                AnimationPlay();
                break;
            case GimmickType.animator:
                AnimatorPlay();
                break;
            case GimmickType.blockDistraction:
                BlockDistraction();
                break;
            case GimmickType.boat:
                BoatMove();
                break;
            case GimmickType.drawerMove:
                DrawerMove();
                break;
        }
    }

    /*! @brief アニメーション再生*/
    void AnimationPlay()
    {
        gimickAnimation.Play();
        isGimmick = false;
    }

    /*! @brief アニメーター再生*/
    void AnimatorPlay()
    {
        animator.enabled = true;
    }

    /*! @brief 積み木破壊*/
    void BlockDistraction()
    {
        if (player.GetState() != MainCharacterController.State.eBlowAway)
        {
            isGimmick = false;
        }
        else
        {
            Destroy(GetComponent<BoxCollider>());
            ++gimmickNum;
        }
    }

    /*! @brief ボート移動*/
    void BoatMove()
    {
        Vector3 pre = transform.parent.localPosition;
        pre.y = 0f;
        transform.parent.localPosition = Vector3.Lerp(transform.parent.localPosition, position[positionNum], 0.02f);
        Vector3 aft = transform.parent.localPosition;
        aft.y = 0f;
        player.SetStopState();
        float distance = Vector3.Distance(pre, aft);
        if (distance <= 0.1f)
            player.playerPath.currentPos = 0.938f;
    }

    /*! @brief 電池をボートにセット*/
    void SetBattery()
    {
        if (gimmickNum == 0)
        {
            battery.gameObject.SetActive(false);
            batteryMesh.enabled = true;
            isGimmick = true;
        }
    }

    /*! @brief 引き出し移動*/
    void DrawerMove()
    {
        if (Vector3.Distance(startPos, transform.position) >= moveAbleDist)
        {
            isGimmick = false;
        }
        if (player.playerPath.GetInputOnly() > 0)
        {
            return;
        }
        Vector3 own = new Vector3(0f, transform.position.y, 0f);
        Vector3 ply = new Vector3(0f, player.transform.position.y, 0f);
        float dist = Vector3.Distance(own, ply);
        bool trigger = player.GetRightTrigger();
        if (dist < 1.0f && trigger)
        {
            transform.position += player.playerPath.GetAddPotision();
            player.transform.LookAt(transform.position);
            player.GetComponent<Animator>().SetBool("isScissors", true);
        }
        else if(!trigger)
            player.GetComponent<Animator>().SetBool("isScissors", false);
    }

    /*! @brief 発動ギミック番号の加算*/
    public void AddGimmickNumber()
    {
        ++gimmickNum;
        if (gimmickNum >= type.Length)
        {
            gimmickNum = type.Length - 1;
        }
    }

    /*! @brief 座標番号の加算*/
    public void AddPositionNumber()
    {
        ++positionNum;
        if (positionNum >= position.Length)
        {
            positionNum = position.Length - 1;
            isGimmick = false;
            player.SetNormalState();
        }
    }

    /*! @brief ボートの初期化*/
    public void BoatInit()
    {
        isGimmick = false;
        gimmickNum = 0;
        positionNum = 0;
        battery.gameObject.SetActive(true);
        battery.transform.position = batteryStartPos;
        batteryMesh.enabled = false;
        transform.parent.localPosition = startPos;
        animator.Rebind();
        animator.enabled = false;
    }

    /*! @brief 衝突検知*/
    private void OnTriggerEnter(Collider other)
    {
        //電池のセット
        if (other.transform.name == "battery")
        {
            SetBattery();
        }
    }

    /*! @brief 衝突離れ検知*/
    private void OnTriggerExit(Collider other)
    {
        //引きだしギミック解除
        if(type[gimmickNum]==GimmickType.drawerMove&&other.transform.tag=="Player")
        {
            isGimmick = false;
        }
    }
}
