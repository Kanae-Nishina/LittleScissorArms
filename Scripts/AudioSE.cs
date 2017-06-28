/*!
 * @file AudioSE.cs
 * @brief サウンド再生処理
 * @date 2017/05/18
 * @author 仁科香苗
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*! @brief SE再生処理*/
public class AudioSE : MonoBehaviour
{
    public List<AudioClip> seList = new List<AudioClip>();                                                          /*! SEリスト*/
    private Dictionary<string, AudioClip> seDic = new Dictionary<string, AudioClip>();    /*! SE名との紐づけ*/
    private AudioSource audioSource;                                                                                               /*! オーディオソース*/

   /*! @brief 初期化*/
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        for (int i = 0; i < seList.Count; i++)
        {
            seDic.Add(seList[i].name, seList[i]);
        }
    }

    /*! @brief 再生*/
    public void OnePlay(string name)
    {
        audioSource.clip = seDic[name];
        audioSource.Play();
    }
}
