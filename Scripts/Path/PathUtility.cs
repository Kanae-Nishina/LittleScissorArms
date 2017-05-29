/*
 * @file PathUtility.cs
 * @brief パス計算のユーティリティ
 * @date 2017/05/24
 * @author 仁科香苗
 * @note 参考:PathMagic(https://www.assetstore.unity3d.com/jp/#!/content/47769)
 */
using System.Collections;
using UnityEngine;

public class PathUtility
{
    /* @brief ベジェによるクォータニオン補間*/
    public static Quaternion QuaternionBezier(Quaternion p, Quaternion prevP, Quaternion nextP, Quaternion nextNextP, float stepPos)
    {
        Quaternion an = PathUtility.QuaternionNormalize(Quaternion.Slerp(Quaternion.Slerp(prevP, p, 2f), nextP, 0.5f));
        Quaternion an1 = PathUtility.QuaternionNormalize(Quaternion.Slerp(Quaternion.Slerp(p, nextP, 2f), nextNextP, 0.5f));
        Quaternion bn1 = PathUtility.QuaternionNormalize(Quaternion.Slerp(an1, nextP, 2f));

        Quaternion p1 = Quaternion.Slerp(p, an, stepPos);
        Quaternion p2 = Quaternion.Slerp(an, bn1, stepPos);
        Quaternion p3 = Quaternion.Slerp(bn1, nextP, stepPos);
        Quaternion p12 = Quaternion.Slerp(p1, p1, stepPos);
        Quaternion p23 = Quaternion.Slerp(p2, p3, stepPos);

        return Quaternion.Slerp(p12, p23, stepPos);
    }

    /* @brief クォータニオンの正規化*/
    public static Quaternion QuaternionNormalize(Quaternion q)
    {
        float norm = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
        if (norm > 0.0f)
        {
            q.x /= norm;
            q.y /= norm;
            q.z /= norm;
            q.w /= norm;
        }
        else
        {
            q.x = 0.0f;
            q.y = 0.0f;
            q.z = 0.0f;
            q.w = 1.0f;
        }
        return q;
    }

    /* @brief ベジェによる2つのベクトルの3次元補間の計算*/
    public static Vector3 Vector3Bezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float tt = t * t;
        float ttt = t * tt;
        float u = 1.0f - t;
        float uu = u * u;
        float uuu = u * uu;

        Vector3 B = new Vector3();
        B = uuu * p0;
        B += 3.0f * uu * t * p1;
        B += 3.0f * u * tt * p2;
        B += ttt * p3;

        return B;
    }

    /* @brief 2つのスカラーの補間をベジェで計算*/
    public static float FloatBezier(float p0, float p1, float p2, float p3, float t)
    {
        float tt = t * t;
        float ttt = t * tt;
        float u = 1.0f - t;
        float uu = u * u;
        float uuu = u * uu;

        float B;
        B = uuu * p0;
        B += 3.0f * uu * t * p1;
        B += 3.0f * u * tt * p2;
        B += ttt * p3;

        return B;
    }
}
