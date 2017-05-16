/*
 * @file        ChildCollision.cs
 * @brief      子オブジェクトの衝突判定を親オブジェクトへ送る
 * @date      2017/03/14
 * @author   仁科香苗
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChildCollision : MonoBehaviour
{

    /*private宣言*/
    public GameObject parent;      //親オブジェクト

    // Use this for initialization
    void Start()
    {
    }

    /* @brief   物理演算系更新*/
    private void FixedUpdate()
    {
    }

    /* @brief   衝突検知*/
    private void OnTriggerStay(Collider col)
    {
        parent.SendMessage("ChildOnTriggerStay", col);
    }

    //private void OnCollisionEnter(Collision collision)
    //{
    //    if (GetComponent<Rigidbody>())
    //    {
    //        parent.SendMessage("ChildOnCollisionEnter", collision);
    //    }
    //}

    /* @brief   衝突離れ検知*/
    private void OnTriggerExit(Collider col)
    {
        parent.SendMessage("ChildOnTriggerExit", col);
    }

    //private void OnCollisionExit(Collision collision)
    //{
    //    if (GetComponent<Rigidbody>())
    //    {
    //        parent.SendMessage("ChildOnCollisionExit", collision);
    //    }
    //}
}
