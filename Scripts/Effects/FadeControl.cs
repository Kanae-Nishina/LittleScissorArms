/*!
 * @file FadeControl.cs
 * @brief フェード管理クラス
 * @date    2017/05/17
 * @author 仁科香苗
* @note 参考:テラシュールブログ(http://tsubakit1.hateblo.jp/entry/20140505/1399289078)
 */
using System.Collections;
using UnityEngine.Assertions;
using UnityEngine;

/*! @brief フェード管理クラス*/
public class FadeControl : MonoBehaviour
{
    private FadeBase fade;              /*! フェードベース*/
    private float cutoutRange;      /*! フェードする範囲*/

    /*! @brief 更新前初期化 */
    void Start()
    {
        Init();
    }

    /*! @brief 変更時のセット(Editor上のみ)*/
    private void OnValidate()
    {
        Init();
    }

    /*! @brief 共通初期化*/
    void Init()
    {
        fade = GetComponent<FadeBase>();
        fade.range = cutoutRange;
    }

    /*! @brief フェードイン*/
    IEnumerator FadeInCoroutine(float time,System.Action action)
    {
        var endFrame = new WaitForEndOfFrame();
        while(cutoutRange<time)
        {
            cutoutRange += Time.unscaledDeltaTime;
            fade.range = cutoutRange;
            yield return endFrame;
        }
        cutoutRange = 1f;
        fade.range = cutoutRange;
        if(action!=null)
        {
            action();
        }
    }

    /*! @brief フェードアウト*/
    IEnumerator FadeOutCoroutine(float time,System.Action action)
    {
        var endFrame = new WaitForEndOfFrame();
        cutoutRange = time;
        while(cutoutRange>0f)
        {
            cutoutRange -= Time.unscaledDeltaTime;
            fade.range = cutoutRange;
            yield return endFrame;
        }
        cutoutRange = 0f;
        fade.range = cutoutRange;
        if(action!=null)
        {
            action();
        }
    }

    /*! @brief フェードイン呼び出し*/
    public Coroutine FadeIn(float time,System.Action action)
    {
        StopAllCoroutines();
        return StartCoroutine(FadeInCoroutine(time, action));
    }

    /*! @brief フェードイン呼び出し*/
    public Coroutine FadeIn(float time)
    {
        return FadeIn(time, null);
    }

    /*! @brief フェードアウト呼び出し*/
    public Coroutine FadeOut(float time,System.Action action)
    {
        StopAllCoroutines();
        return StartCoroutine(FadeOutCoroutine(time, action));
    }

    /*! @brief フェードイン呼び出し*/
    public Coroutine FadeOut(float time)
    {
        return FadeOut(time, null);
    }
}
