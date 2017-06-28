/*!
 * @file        ChildCollision.cs
 * @brief      子オブジェクトの衝突判定を親オブジェクトへ送る
 * @date      2017/03/14
 * @author   仁科香苗
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*! @brief 子オブジェクトの衝突検知*/
public class ChildCollision : MonoBehaviour
{
    public GameObject parent;      /*! 親オブジェクト*/
    
    /*! @brief   衝突検知*/
    private void OnTriggerEnter(Collider col)
    {
        parent.SendMessage("ChildOnTriggerEnter", col);
    }

    /*! @brief   衝突検知継続*/
    private void OnTriggerStay(Collider col)
    {
        parent.SendMessage("ChildOnTriggerStay", col);
    }
    
    /*! @brief   衝突離れ検知*/
    private void OnTriggerExit(Collider col)
    {
        parent.SendMessage("ChildOnTriggerExit", col);
    }
}
