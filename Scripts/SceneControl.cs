/*!
 * @file        SceneControl.cs
 * @brief     シーン管理
 * @date      2017/05/17
 * @author  仁科香苗
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using InputGamePad;

/*! @brief シーン管理クラス*/
public class SceneControl : MonoBehaviour
{
    public float fadeTime = 1f;                   /*! フェードにかける時間*/
    public string[] sceneName;                 /*! 遷移するシーン名*/
    public AudioClip[] bgm;                        /*! そのシーンで再生するBGM*/

    private int sceneNumber = 0;                                                   /*! 現在のシーン番号*/
    private AudioSource audioSorce;                                           /*! オーディオソース*/
    private Dictionary<string, AudioClip> sceneSoundDic;    /*! BGMの名前とBGMの紐づけ*/
    private List<GameObject> dontDestroy;                              /*! シーンをまたいでも消さないオブジェクト*/
    private static SceneControl instanceThis = null;                 /*! シングルトン用クラスのインスタンス*/
    [SerializeField]
    private FadeControl fade = null;            /*! フェード管理クラス*/

    /*! @brief インスタンス取得*/
    static SceneControl instance
    {
        get { return instanceThis ?? (instanceThis = FindObjectOfType<SceneControl>()); }
    }

    /*! @brief 起動時初期化 */
    private void Awake()
    {
        //シングルトン設定
        if (this != instance)
        {
            Destroy(gameObject);
            return;
        }

        //シーンをまたいで消さないオブジェクト設定
        DontDestroyOnLoad(this.gameObject);
        dontDestroy = new List<GameObject>();
        foreach (Transform child in transform)
        {
            dontDestroy.Add(child.gameObject);
        }
        sceneSoundDic = new Dictionary<string, AudioClip>();
        if (sceneName.Length == bgm.Length)
        {
            for (int i = 0; i < sceneName.Length; i++)
            {
                sceneSoundDic.Add(sceneName[i], bgm[i]);
            }
        }
    }

    /*! @brief 更新前初期化*/
    private void Start()
    {
        audioSorce = GetComponent<AudioSource>();
        audioSorce.clip = sceneSoundDic[sceneName[sceneNumber]];
        audioSorce.Play();
    }

    /*! @brief 更新*/
    private void Update()
    {
        if ((GamePad.GetButtonDown(GamePad.Button.Start) || Input.GetKeyDown(KeyCode.A)))
        {
            ++sceneNumber;
            if (sceneNumber == sceneName.Length)
            {
                sceneNumber = 0;
            }
            ChangeScene();
        }
    }


    /*! @brief シーン遷移*/
    public void ChangeScene()
    {
        fade.FadeIn(fadeTime, () =>
        {
            SceneManager.LoadScene(sceneName[sceneNumber]);
            audioSorce.Stop();
            audioSorce.clip = sceneSoundDic[sceneName[sceneNumber]];
            audioSorce.Play();
            fade.FadeOut(fadeTime, () =>{});
        });
    }

    /*! @brief クリアシーンの追加*/
    public void AddClearScene()
    {
        SceneManager.LoadScene("Clear", LoadSceneMode.Additive);
        GameObject.Find("chara_newbig").GetComponent<MainCharacterController>().SetStopState(); //プレイヤーの移動を停止する
    }
}
