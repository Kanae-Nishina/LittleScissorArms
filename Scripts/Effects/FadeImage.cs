/*
 * @file FadeImage.cs
 * @brief フェードするイメージクラス
 * @date    2017/05/17
 * @author 仁科香苗
 */
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeImage : UnityEngine.UI.Graphic, FadeBase
{
    [SerializeField]
    private Texture maskTexture = null;

    [SerializeField, Range(0f, 1f)]
    private float cutoutRange;

    /* @brief フェード範囲*/
    public float range
    {
        get { return cutoutRange; }
        set
        {
            cutoutRange = value;
            UpdateMaskCutOut(cutoutRange);
        }
    }

    // Use this for initialization
    protected override void Start()
    {
        base.Start();
        UpdateMaskTexture(maskTexture);
    }

    /* @brief マスクの更新*/
    private void UpdateMaskCutOut(float range)
    {
        enabled = true;
        material.SetFloat("_Range", 1f - range);
        if (range <= 0f)
        {
            this.enabled = false;
        }
    }

    /* @brief マスクのテクスチャ更新*/
    private void UpdateMaskTexture(Texture texture)
    {
        material.SetTexture("_MaskTex", texture);
        material.SetColor("_Color", color);
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        UpdateMaskCutOut(range);
        UpdateMaskTexture(maskTexture);
    }
#endif
}
