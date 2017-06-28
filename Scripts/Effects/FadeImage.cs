/*!
 * @file FadeImage.cs
 * @brief フェードするイメージクラス
 * @date    2017/05/17
 * @author 仁科香苗
* @note 参考:テラシュールブログ(http://tsubakit1.hateblo.jp/entry/20140505/1399289078)
 */
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/*! @brief フェードするイメージクラス*/
public class FadeImage : UnityEngine.UI.Graphic, FadeBase
{
    [SerializeField]
    private Texture maskTexture = null;         /*! マスクテクスチャ*/

    [SerializeField, Range(0f, 1f)]
    private float cutoutRange;                          /*! フェード範囲*/

    /*! @brief フェード範囲*/
    public float range
    {
        get { return cutoutRange; }
        set
        {
            cutoutRange = value;
            UpdateMaskCutOut(cutoutRange);
        }
    }

    /*! 初期化*/
    protected override void Start()
    {
        base.Start();
        UpdateMaskTexture(maskTexture);
    }

    /*! @brief マスクの更新*/
    private void UpdateMaskCutOut(float range)
    {
        enabled = true;
        material.SetFloat("_Range", 1f - range);
        if (range <= 0f)
        {
            this.enabled = false;
        }
    }

    /*! @brief マスクのテクスチャ更新*/
    private void UpdateMaskTexture(Texture texture)
    {
        material.SetTexture("_MaskTex", texture);
        material.SetColor("_Color", color);
    }

#if UNITY_EDITOR
    /*! @brief 変更時のセット(Editor上のみ)*/
    protected override void OnValidate()
    {
        base.OnValidate();
        UpdateMaskCutOut(range);
        UpdateMaskTexture(maskTexture);
    }
#endif
}
