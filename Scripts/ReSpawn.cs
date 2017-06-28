/*!
 *  @file           ReSpawn.cs
 *  @brief         リスポン処理
 *  @date         2017/05/26
 *  @author      仁科香苗
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;

/*! @brief リスポン処理*/
public class ReSpawn : MonoBehaviour
{
    public float respawnPos;                     /*! パス上のリスポンする座標*/
    public float height;                                /*! リスポンする高さ*/
    public PlayerPath playerPath;            /*! プレイヤーの移動パス*/
    public float fadeTime = 1f;                   /*! フェードにかける時間*/

    [Serializable]
    public class Event : UnityEvent { };    /*! イベント*/
    public Event events;                              /*! リスポン時のイベント*/

    [SerializeField]
    private FadeControl fade = null;        /*! フェード管理クラス*/

    /*! @brief 衝突判定*/
    private void OnTriggerEnter(Collider other)
    {
        //プレイヤーが触れたらリスポン
        if (other.transform.tag == "Player")
        {
            //フェードイン
            fade.FadeIn(fadeTime, () =>
            {
                events.Invoke();    //リスポン時のイベント処理
                playerPath.Respawn(respawnPos, height); //リスポン地点設定
                fade.FadeOut(fadeTime, () =>{});                //フェードアウト
            });
        }
    }
}
