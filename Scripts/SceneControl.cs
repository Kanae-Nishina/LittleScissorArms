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

public class SceneControl : MonoBehaviour
{
    public float fadeTime = 1f;                   //フェードにかける時間
    public string[] sceneName;
    public AudioClip[] bgm;
    int sceneNumber = 0;

    AudioSource audio;

    Dictionary<string, AudioClip> sceneSoundDic;
    List<GameObject> dontDestroy;      //Sシーンをまたいでも消さないオブジェクト

    [SerializeField]
    FadeControl fade = null;

    static SceneControl instanceThis = null;

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
            //DontDestroyOnLoad(child);
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

    private void Start()
    {
        audio = GetComponent<AudioSource>();
        audio.clip = sceneSoundDic[sceneName[sceneNumber]];
        audio.Play();
    }

    /*! @brief 更新*/
    private void Update()
    {
        if (GamePad.GetButtonDown(GamePad.Button.Start) || Input.GetKeyDown(KeyCode.A))
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
            audio.Stop();
            audio.clip = sceneSoundDic[sceneName[sceneNumber]];
            audio.Play();
            fade.FadeOut(fadeTime, () => { });
            if (sceneName[sceneNumber] ==  "Maingame")
            {
                Application.LoadLevelAdditive("Clear");
            }

        });
    }
}
