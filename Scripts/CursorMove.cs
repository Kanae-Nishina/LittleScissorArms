/*!
 *  @file           CursorMove.cs
 *  @brief         カーソル移動処理
 *  @date         2017/04/29
 *  @author      金澤信芳
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*! @brief カーソル移動処理*/
public class CursorMove : MonoBehaviour
{
    public float moveSpeed = 1f; /*! 移動スピード*/
    public float rotSpeed = 2f;     /*! 回転スピード*/
    public float radius = 5f;           /*! 半径*/

    [System.NonSerialized]
    public Vector3 throwPos;        /*! 投げる時のカーソルの座標*/
    
    private float time;  /*! 時間*/

    /*! @brief アクティブ時初期化*/
    private void Start()
    {
        time = 0f;
    }

    /*! @brief   物理演算系更新*/
    void FixedUpdate()
    {
        //移動
        Move();
        //回転
        Rotation();
    }

    /*! @brief 移動*/
    void Move()
    {
        Vector3 newPos = Vector3.zero;
        newPos.z += radius * Mathf.Abs(Mathf.Sin(time * moveSpeed));
        newPos.y += radius * Mathf.Abs(Mathf.Cos(time * moveSpeed));
        time += Time.deltaTime;
        transform.localPosition = newPos;
    }

    /*! @brief 回転*/
    void Rotation()
    {
        transform.Rotate(new Vector3(0f, 0f, rotSpeed));
    }
}
