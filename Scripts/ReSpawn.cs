/*!
 *  @file           ReSpawn.cs
 *  @brief         リスポン処理
 *  @date         2017/05/126
 *  @author      仁科香苗
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*! @brief リスポン処理*/
public class ReSpawn : MonoBehaviour
{
    public float respawnPos;
    public float height;
    public PlayerPath playerPath;
    public float fadeTime = 1f;                   //フェードにかける時間
    [SerializeField]
    FadeControl fade = null;

    private void Start() { }

    /*! @brief 衝突判定*/
    private void OnTriggerEnter(Collider other)
    {

        if (other.transform.tag == "Player")
        {
            fade.FadeIn(fadeTime, () =>
            {
                playerPath.Respawn(respawnPos, height);
                fade.FadeOut(fadeTime, () =>
                {
                });
            });
        }
    }
}
