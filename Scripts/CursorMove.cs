/*!
 *  @file           CursorMove.cs
 *  @brief         カーソル移動処理
 *  @date         2017/04/29
 *  @author      金澤信芳
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorMove : MonoBehaviour
{
    /*!public宣言*/
    public float moveSpeed = 1f; //移動スピード
    public float rotSpeed = 2f;     //回転スピード
    public float radius = 5f; //半径
    //public float limitAngle = 0f; // 逆回転時の限界角度
    //public Vector3 cursorPos;

    /*!private宣言*/
    //private Vector3 targetAngle; // カーソルの角度
    //private Vector3 axis; // 回転角度
    //private bool lookTmp; // フラグの一時保存
    private float time;  //時間

    /*! @brief アクティブ時初期化*/
    private void OnEnable()
    {
        time = 0f;
    }

    /*! @brief   初期化*/
    void Start()
    {
        //フラグ保存
       // lookTmp = MainCharacterController.isLookFront;
    }

    /*! @brief   物理演算系更新*/
    void FixedUpdate()
    {
        //移動
        Move();
        //回転
        Rotation();

#if false
        // カーソル表示フラグ
        //	プレイヤーを中心に自分を現在の上方向に、毎秒angle分だけ回転する。
        //targetAngle = transform.TransformDirection(Vector3.up);
        // targetAngle.z = transform.localEulerAngles.z;

        //targetに、"Sample"の名前のオブジェクトのコンポーネントを見つけてアクセスする
        Transform Player = GameObject.FindGameObjectWithTag("Player").transform;

        if(MainCharacterController.isLookFront != lookTmp)
        {
            //Debug.Log("初期化");
            //cursorPos.x *= -1; // カーソルのX軸反転
            //transform.position = Player.position + cursorPos; // カーソル位置初期化

            //transform.localEulerAngles = new Vector3(0f, 0f, 90f); // 角度初期化
            //Debug.Log(transform.localEulerAngles);

            //angleSpeed *= -1;
            lookTmp = MainCharacterController.isLookFront; // フラグ再保存
        }

        if (MainCharacterController.isLookFront == true)
        {
           // if (targetAngle.z >= 100f || targetAngle.z <= 30f)
           // {
           //     angleSpeed *= -1; // 逆回転
            //}
        }
        else
        {
           // if (targetAngle.z >= 150f || targetAngle.z <= 80f)
            //{
            //    angleSpeed *= -1; // 逆回転
           // }
        }

       // // 回転
        //transform.RotateAround(Player.position, targetAngle, angleSpeed * Time.deltaTime * -1);
#endif
    }

    void Move()
    {
        Vector3 newPos = Vector3.zero;
        newPos.z += radius * Mathf.Abs(Mathf.Sin(time * moveSpeed));
        newPos.y += radius * Mathf.Abs(Mathf.Cos(time * moveSpeed));
        time += Time.deltaTime;
        transform.localPosition = newPos;
    }

    void Rotation()
    {
#if false
        Vector3 p = Camera.main.transform.position;
        p.y = transform.position.y;
        transform.LookAt(p);
#else
        transform.Rotate(new Vector3(0f, 0f, rotSpeed));
#endif
    }
}
