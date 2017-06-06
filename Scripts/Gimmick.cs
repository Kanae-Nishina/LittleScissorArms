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

public class Gimmick :MonoBehaviour
{
    public bool isGimmick = false;
    class GimmickEvent : UnityEvent{ };
    GimmickEvent gimmickEvent;
    private void Start()
    {
        gimmickEvent = new GimmickEvent();
        gimmickEvent.AddListener(GimmickInvocation);
    }

    private void Update()
    {
        if(isGimmick)
        {
            gimmickEvent.Invoke();
        }
    }

    void GimmickInvocation()
    {
        GetComponent<Animation>().enabled = true;
        GetComponent<Animation>().Play();
        isGimmick = false;
    }


}
