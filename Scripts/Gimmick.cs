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

public class Gimmick : MonoBehaviour
{

    public bool isGimmick = false;
    public enum GimmickType
    {
        none,
        animation,
        animator,
        blockDistraction,
        boat,
        drawerMove,
    }
    public GimmickType[] type;

    public Animation animation;
    public Animator animator;
    public Vector3[] position;
    public MeshRenderer batteryMesh;
    public float moveAbleDist;

    private MainCharacterController player;
    private int gimmickNum = 0;
    private int positionNum = -1;
    private bool isHandHit = false;
    private Vector3 startPos;
    private void Start()
    {
        if (animation != null)
        {
            animation.Stop();
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
    }

    private void Update()
    {
        if (isGimmick)
        {
            GimmickInvocation();
        }
    }

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

    void AnimationPlay()
    {
        animation.Play();
        isGimmick = false;
    }

    void AnimatorPlay()
    {
        animator.enabled = true;
    }

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

    void BoatMove()
    {
        //Vector3 pre = transform.parent.localPosition;
        //pre.y = 0f;
        transform.parent.localPosition = Vector3.Lerp(transform.parent.localPosition, position[positionNum], 0.02f);
        //Vector3 aft = transform.parent.localPosition;
        //aft.y = 0f;
        //float diff = Vector3.Distance(pre, aft);

    }

    void SetBattery(Transform battery)
    {
        if (gimmickNum == 0)
        {
            //Destroy(battery.gameObject);
            battery.gameObject.SetActive(false);
            batteryMesh.enabled = true;
            isGimmick = true;
        }
    }

    void DrawerMove()
    {
        if (Vector3.Distance(startPos, transform.position) >= moveAbleDist)
            isGimmick = false;
        Vector3 own = new Vector3(0f, transform.position.y, 0f);
        Vector3 ply = new Vector3(0f, player.transform.position.y, 0f);
        float dist = Vector3.Distance(own, ply);
        if (dist < 1.0f && (player.GetTrigger(InputGamePad.GamePad.Trigger.RightTrigger) > 0.8f))
            transform.position += player.playerPath.GetAddPotision();
    }

    public void AddGimmickNumber()
    {
        ++gimmickNum;
        if (gimmickNum >= type.Length)
        {
            gimmickNum = type.Length - 1;
        }
    }

    public void AddPositionNumber()
    {
        ++positionNum;
        if (positionNum >= position.Length)
        {
            positionNum = position.Length - 1;
            isGimmick = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.name == "battery")
        {
            SetBattery(other.transform);
        }
    }

}
