/*!
 * @file WaterHeight.cs
 * @brief 水面の高さ取得クラス
 * @date 2017/05/19
 * @author  仁科香苗
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterHeight : MonoBehaviour
{
    public float mag = 0.03f;   //浮き沈みの大きさ
    public float diff = 0.2f;        //差分

    [SerializeField]
    MeshRenderer targetMr;      //水面のメッシュレンダラ
    Material mat;                            //水面のマテリアル情報
    List<Transform> childObj;    //浮き沈みする対象オブジェクト(子オブジェクト) 
    List<Vector3> childStartPos;

    /*! @brief 初期化*/
    void Start()
    {
        mat = targetMr.sharedMaterial;
        childObj = new List<Transform>();
        childStartPos = new List<Vector3>();
        foreach (Transform c in transform)
        {
            childObj.Add(c);
            childStartPos.Add(c.transform.localPosition);
        }
    }

    /*! @brief 更新*/
    void Update()
    {

        Vector2 xzVtx = new Vector2(transform.position.x, transform.position.z);
        Vector4 steepness = mat.GetVector("_GSteepness");
        Vector4 amp = mat.GetVector("_GAmplitude");
        Vector4 freq = mat.GetVector("_GFrequency");
        Vector4 speed = mat.GetVector("_GSpeed");
        Vector4 dirAB = mat.GetVector("_GDirectionAB");
        Vector4 dirCD = mat.GetVector("_GDirectionCD");
        for (int i = 0; i < childObj.Count; i++)
        {
            Vector3 ofs = GerstnerOffset4(i,xzVtx, steepness, amp, freq, speed, dirAB, dirCD);
            //transform.GetChild(0).localPosition = ofs * 0.5f;

            childObj[i].transform.localPosition = childStartPos[i] + ofs * mag;
        }
    }

    /*! @brief 頂点オフセットの計算*/
    Vector3 GerstnerOffset4(int no,Vector2 xzVtx, Vector4 steepness, Vector4 amp, Vector4 freq, Vector4 speed, Vector4 dirAB, Vector4 dirCD)
    {
        float t = Time.timeSinceLevelLoad-diff*no;
        Vector4 _Time = new Vector4(t / 20, t, t * 2, t * 3);
        Vector3 offsets;

        Vector4 AB = Vector4.Scale(Vector4.Scale(xxyy(steepness), xxyy(amp)), dirAB); //steepness.xxyy * amp.xxyy * dirAB.xyzw;
        Vector4 CD = Vector4.Scale(Vector4.Scale(zzww(steepness), zzww(amp)), dirCD); //steepness.zzww * amp.zzww * dirCD.xyzw;

        Vector4 dotABCD = Vector4.Scale(freq, new Vector4(
            Vector2.Dot(new Vector2(dirAB.x, dirAB.y), xzVtx),
            Vector2.Dot(new Vector2(dirAB.z, dirAB.w), xzVtx),
            Vector2.Dot(new Vector2(dirCD.x, dirCD.y), xzVtx),
            Vector2.Dot(new Vector2(dirCD.z, dirCD.w), xzVtx)
        )); //freq.xyzw * half4(dot(dirAB.xy, xzVtx), dot(dirAB.zw, xzVtx), dot(dirCD.xy, xzVtx), dot(dirCD.zw, xzVtx));
        Vector4 TIME = Vector4.Scale(Vector4.one * _Time.y, speed); //_Time.yyyy * speed;

        Vector4 COS = new Vector4(
            Mathf.Cos(dotABCD.x + TIME.x),
            Mathf.Cos(dotABCD.y + TIME.y),
            Mathf.Cos(dotABCD.z + TIME.z),
            Mathf.Cos(dotABCD.w + TIME.w)
        ); //cos (dotABCD + TIME);
        Vector4 SIN = new Vector4(
            Mathf.Sin(dotABCD.x + TIME.x),
            Mathf.Sin(dotABCD.y + TIME.y),
            Mathf.Sin(dotABCD.z + TIME.z),
            Mathf.Sin(dotABCD.w + TIME.w)
        ); //sin (dotABCD + TIME);

        offsets.z = Vector4.Dot(COS, new Vector4(AB.x, AB.z, CD.x, CD.z)); // dot(COS, Vector4(AB.xz, CD.xz));
        offsets.x = Vector4.Dot(COS, new Vector4(AB.y, AB.w, CD.y, CD.w)); // dot(COS, Vector4(AB.yw, CD.yw));
        offsets.y = Vector4.Dot(SIN, amp); // dot(SIN, amp);

        return offsets;
    }

    Vector4 xxyy(Vector4 _in) { return new Vector4(_in.x, _in.x, _in.y, _in.y); }
    Vector4 zzww(Vector4 _in) { return new Vector4(_in.z, _in.z, _in.w, _in.w); }
}
