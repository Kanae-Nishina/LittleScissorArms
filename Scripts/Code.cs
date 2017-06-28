/*!
 *  @file           Code.cs
 *  @brief        コードの描画処理
 *  @date         2017/06/21
 *  @author      仁科香苗
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*! @brief コードの描画処理*/
public class Code : MonoBehaviour
{
    public Transform mainPlayer;            /*! メインプレイヤー*/
    public Vector3 offsetMain;                  /*! メインプレイヤーから出るコードのオフセット*/
    public Transform subPlayer;               /*! サブプレイヤー*/
    public Vector3 offsetSub;                    /*! メインプレイヤーから出るコードのオフセット*/
    private LineRenderer lineRender;    /*! コード描画の為のラインレンダラ*/

    /*! @brief 初期化*/
    void Start()
    {
        lineRender = GetComponent<LineRenderer>();
        lineRender.positionCount = 2;
    }

    /*! @brief 更新*/
    void Update()
    {
        lineRender.SetPosition(0, mainPlayer.position + offsetMain);
        lineRender.SetPosition(1, subPlayer.position + offsetSub);
    }
}
