/*!
 * @file MotionEvent.cs
 * @brief モーション依存の処理
 * @date 2017/07/04
 * @author 仁科香苗
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*! @brief モーション依存の処理*/
public class MotionEvent : MonoBehaviour
{
    public List<AudioClip> seList = new List<AudioClip>();                                                          /*! SEリスト*/
    private Dictionary<string, AudioClip> seDic = new Dictionary<string, AudioClip>();    /*! SE名との紐づけ*/
    private AudioSource audioSource;                                                                                               /*! オーディオソース*/
    private SubCharacterController subCharaCon;

    /*! @brief 初期化*/
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        for (int i = 0; i < seList.Count; i++)
        {
            seDic.Add(seList[i].name, seList[i]);
        }
        subCharaCon = GameObject.Find("SubPlayer").GetComponent<SubCharacterController>();
    }

    /*! @brief 再生*/
    public void OnePlay(string name)
    {
        audioSource.clip = seDic[name];
        audioSource.Play();
    }

    /*! @brief ジャンプ*/
    public void Jump(float power)
    {
        transform.parent.GetComponent<Rigidbody>().AddForce(Vector3.up*power);
    }

    /*! @brief サブキャラステート変更*/
    public void ChangeSubStateToCarry()
    {
        subCharaCon.GetComponent<SubCharacterController>().SetStateBeCarried();
    }
}
