/*
 * @file FadeControl.cs
 * @brief フェード管理クラス
 * @date    2017/05/17
 * @author 仁科香苗
 */
using System.Collections;
using UnityEngine.Assertions;
using UnityEngine;

public class FadeControl : MonoBehaviour
{
    FadeBase fade;
    float cutoutRange;      //フェードする範囲

    /* @brief 更新前初期化 */
    void Start()
    {
        Init();
    }

    /* @brief 変更時のセット(Editor上のみ)*/
    private void OnValidate()
    {
        Init();
    }

    /* @brief 共通初期化*/
    void Init()
    {
        fade = GetComponent<FadeBase>();
        fade.range = cutoutRange;
    }

    /* @brief フェードイン*/
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

    /* @brief フェードアウト*/
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

    /* @brief フェードイン呼び出し*/
    public Coroutine FadeIn(float time,System.Action action)
    {
        StopAllCoroutines();
        return StartCoroutine(FadeInCoroutine(time, action));
    }
    public Coroutine FadeIn(float time)
    {
        return FadeIn(time, null);
    }

    /* @brief フェードアウト呼び出し*/
    public Coroutine FadeOut(float time,System.Action action)
    {
        StopAllCoroutines();
        return StartCoroutine(FadeOutCoroutine(time, action));
    }
    public Coroutine FadeOut(float time)
    {
        return FadeOut(time, null);
    }
}
