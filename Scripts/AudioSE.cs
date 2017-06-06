/*!
 * @file AudioSE.cs
 * @brief サウンド再生処理
 * @date 2017/05/18
 * @author 仁科香苗
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioSE : MonoBehaviour
{
    public List<AudioClip> seList = new List<AudioClip>();
    private Dictionary<string, AudioClip> seDic = new Dictionary<string, AudioClip>();
    private AudioSource audioSource;

    // Use this for initialization
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        for (int i = 0; i < seList.Count; i++)
        {
            seDic.Add(seList[i].name, seList[i]);
        }
    }

    /*! @brief ただの再生*/
    public void OnePlay(string name)
    {
        audioSource.clip = seDic[name];
        audioSource.Play();
    }

    /*! @brief 重ねて再生*/
    public void OneShotPlay(string name)
    {
        audioSource.PlayOneShot(seDic[name]);
    }
    
}
