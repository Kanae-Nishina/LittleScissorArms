/*
 * @file PlayerPath.cs
 * @brief プレイヤーの移動パス
 * @date 2017/04/14
 * @author 仁科香苗
 */
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/* @brief パスの見た目 */
[System.Serializable]
public class PathVisual
{
    public Color pathColor = Color.green;                       //パスの色
    public Color inactivePathColor = Color.gray;           //非アクティブ時のパスの色
    public Color selectHandleColor = Color.red;            //選択したハンドルの色
    public Color handleColor = Color.yellow;                 //ハンドルの色
}

/* @brief ハンドル情報 */
[System.Serializable]
public class HandlePoint
{
    public Vector3 position;            //座標
    public Quaternion rotation;     //角度
    public Vector3 handlePrev;      //前ハンドル方向
    public Vector3 handleNext;     //次のハンドル方向
    public bool chained;                   //パス曲線のハンドルが対照かどうか
    public bool showPoints;           //インスペクターに情報を描画するかどうか
    public float nextSpentTime;    //次のハンドルまでにかかる時間
    public float nextDistance;      //次のハンドルまでの距離

    //コンストラクタ
    public HandlePoint(Vector3 pos, Quaternion rot)
    {
        position = pos;
        rotation = rot;
        handlePrev = Vector3.back;
        handleNext = Vector3.forward;
        chained = true;
        showPoints = false;
        nextSpentTime = 0f;
        nextDistance = 0f;
    }
    public HandlePoint(Vector3 pos,Quaternion rot,Vector3 prev,Vector3 next,float time,float dist)
    {
        position = pos;
        rotation = rot;
        handlePrev = prev;
        handleNext = next;
        nextSpentTime = time;
        nextDistance = dist;
    }
}

/* @brief パスの処理 */
public class PlayerPath : MonoBehaviour
{
    public Transform player = null;                                                                 //プレイヤートランスフォーム
    public float perSecond = 0f;                                                                     //秒速
    public List<HandlePoint> points = new List<HandlePoint>();     //ハンドルリスト
    public PathVisual visual;                                                                          //パスの見た目
    public bool alwaysShow = true;                                                              //パスの可視化
    public int firstPointsNumber;  //開始地点

    private Vector3 addPosition = Vector3.zero;     //座標に加算される値
    private int currentWayPointIndex = 0;                //現在のハンドル番号
    private float currentTimeInWayPoint = 0f;        //補間する値
    private float inputX = 0f;                                         //入力情報
    private int inputMag = 0;                                           //移動倍率

    /* @brief 起動時初期化*/
    private void Awake()
    {
        //ハンドル間の距離とかかる時間設定
        SetTimeAndDistance();
        OutputPathinfomation();
        InputPathInfomation();
    }

    /* @brief 更新前初期化 */
    void Start()
    {
        //プレイヤーや秒速が設定されているかどうかのチェック
        if (player == null) Debug.LogError("Player Transform is Null!!");
        if (perSecond <= 0) Debug.LogError("PerScond is not set!!");

        //移動開始
        MovePath();
    }

    /* @brief パス情報出力 */
    void OutputPathinfomation()
    {
        string pathName = Application.dataPath + "/ExternalData/" + SceneManager.GetActiveScene().name + "_Player.txt";
        StreamWriter sw = new StreamWriter(pathName, false);
        int count = 0;
        //sw.WriteLine(SceneManager.GetActiveScene().name);
        foreach (var p in points)
        {
            sw.WriteLine("#" + count + "#");
            sw.WriteLine(p.position);
            sw.WriteLine(p.rotation);
            sw.WriteLine(p.handlePrev);
            sw.WriteLine(p.handleNext);
            sw.WriteLine(p.nextSpentTime);
            sw.WriteLine(p.nextDistance);
            ++count;
        }
        sw.Flush();
        sw.Close();
    }

    /* @brief パス情報の読み込み */
    public string pathName;
    void InputPathInfomation()
    {
        pathName = Application.dataPath + "/ExternalData/" + SceneManager.GetActiveScene().name + "_Player.txt";
        StreamReader sr = new StreamReader(pathName);
        string line = "";
        points.Clear();
        while ((line = sr.ReadLine()) != null)
        {
            string[] count = line.Split("#"[0]);
            int num = int.Parse(count[1]);
           Vector3 pos = StringToVector3(sr.ReadLine());
            Quaternion rot = StringToQuaternion(sr.ReadLine());
            Vector3 prev = StringToVector3(sr.ReadLine());
            Vector3 next = StringToVector3(sr.ReadLine());
            float time = float.Parse(sr.ReadLine());
            float dist = float.Parse(sr.ReadLine());
            HandlePoint point=new HandlePoint(pos,rot,prev,next,time,dist);
            points.Add(point);
        }
    }

    /* @brief string→Vector3変換 */
    public Vector3 StringToVector3(string str)
    {
        string[] separate = { "(", ",", ")" };
        string[] pos = str.Split(separate, System.StringSplitOptions.RemoveEmptyEntries);
        Vector3 vec;
        vec.x = float.Parse(pos[0]);
        vec.y = float.Parse(pos[1]);
        vec.z = float.Parse(pos[2]);
        return vec;
    }

    /* @brief string→Quatenion変換 */
    public Quaternion StringToQuaternion(string str)
    {
        string[] separate = { "(", ",", ")" };
        string[] rot = str.Split(separate, System.StringSplitOptions.RemoveEmptyEntries);
        Quaternion qua;
        qua.x = float.Parse(rot[0]);
        qua.y = float.Parse(rot[1]);
        qua.z = float.Parse(rot[2]);
        qua.w = float.Parse(rot[3]);
        return qua;
    }

    /* @brief 移動開始処理*/
    void MovePath()
    {
        StopAllCoroutines();
        StartCoroutine(FollowPath());
    }

    /* @brief パスに沿って移動*/
    IEnumerator FollowPath()
    {
        //開始地点の設定
        currentWayPointIndex = firstPointsNumber;
        while (currentWayPointIndex < points.Count)
        {
            //補間値初期化
            if (currentTimeInWayPoint >= 0)
                currentTimeInWayPoint = 0f;
            else
                currentTimeInWayPoint = 1f + currentTimeInWayPoint;

            float timePerSegment = points[currentWayPointIndex].nextSpentTime;
            while (currentTimeInWayPoint < 1f && currentTimeInWayPoint >= 0f)
            {
                float input = 0f;
                if (inputX > 0f && currentWayPointIndex < points.Count - 1)
                    input = 1f* inputMag;
                else if (inputX < 0f)
                    input = -1f* inputMag;

                float timeInWayPoint = currentTimeInWayPoint + (Time.deltaTime / timePerSegment) * input;
                //currentTimeInWayPoint += (Time.deltaTime / timePerSegment) * input;

                //加算値を算出
                Vector3 newPos = GetBezierPosition(currentWayPointIndex, timeInWayPoint);
                addPosition = newPos - player.transform.position;
                addPosition.y = 0f;
                Vector3 dir = addPosition;// * input*-1;
                Vector3 pivot = player.transform.position;
                pivot.y += player.transform.localScale.y/2;
                Debug.DrawRay(pivot, dir, Color.red);
                if (!Physics.Raycast(pivot,dir, player.transform.localScale.x))
                {
                    addPosition.y = 0f;
                    currentTimeInWayPoint = timeInWayPoint;
                }
                else
                {
                    addPosition = Vector3.zero;
                }
                yield return 0;
            }

            //ハンドル更新
            if (currentTimeInWayPoint >= 1f)
                ++currentWayPointIndex;
            else if (currentTimeInWayPoint < 0f)
                --currentWayPointIndex;
            //最終ハンドルに到達
            if (currentWayPointIndex == points.Count)
                currentWayPointIndex = points.Count - 1;
            else if (currentWayPointIndex == -1)
            {
                currentWayPointIndex = 0;
                currentTimeInWayPoint = 0f;
            }
        }
        //コルーチン停止
        StopAllCoroutines();
    }

    /* @brief 各ハンドル間の距離設定*/
    void SetTimeAndDistance()
    {
        int pointIndex = 0;
        float pointTime = 0f;
        Vector3 pos = points[pointIndex].position;
        float timePerSegment = 1f / points.Count;
        while (pointIndex < points.Count)
        {
            float dist = 0f;
            pointTime = 0f;
            while (pointTime < 1f && pointTime >= 0f)
            {
                pointTime += Time.deltaTime / timePerSegment;
                Vector3 newPos = GetBezierPosition(pointIndex, pointTime);
                dist += Vector3.Distance(pos, newPos);
                pos = newPos;
            }
            points[pointIndex].nextDistance = dist;
            points[pointIndex].nextSpentTime = dist / perSecond;
            ++pointIndex;
            if (pointIndex == points.Count) break;
        }
    }

    /* @brief ベジエ曲線上の座標取得*/
    public Vector3 GetBezierPosition(int pointIndex, float time)
    {
        int nextIndex = GetNextIndex(pointIndex);
        Vector3 currentNext = points[pointIndex].position + points[pointIndex].handleNext;
        Vector3 nextPrev = points[nextIndex].position + points[nextIndex].handlePrev;
        Vector3 newPos =
            Vector3.Lerp(
                Vector3.Lerp(
                    Vector3.Lerp(points[pointIndex].position, currentNext, time),
                    Vector3.Lerp(currentNext, nextPrev, time), time),
                Vector3.Lerp(
                    Vector3.Lerp(currentNext, nextPrev, time),
                    Vector3.Lerp(nextPrev, points[nextIndex].position, time), time), time);

        return newPos;
    }

    /* @brief 次のハンドル取得*/
    int GetNextIndex(int index)
    {
        if (index == points.Count - 1)
            return 0;
        return ++index;
    }

    /* @brief 加算値取得*/
    public Vector3 GetAddPotision()
    {
        return addPosition;
    }

    /* @brief 入力値セット */
    public void SetInput(float input,int mag)
    {
        inputX = input;
        inputMag = mag;
    }
    

    /* @brief 入力値取得*/
    public float GetInput()
    {
        float input = 0f;
        if (inputX > 0f && currentWayPointIndex < points.Count - 1)
            input = 1f * inputMag;
        else if (inputX < 0f)
            input = -1f * inputMag;
        return inputX* inputMag;
    }

    /* @brief 補間値取得*/
    public float GetCurrentWayTime()
    {
        return currentTimeInWayPoint;
    }

#if UNITY_EDITOR
    /* @brief シーンビュー上にパス描画*/
    public void OnDrawGizmos()
    {
        //描画対象があるとき又は常に描画するとき
        if (UnityEditor.Selection.activeGameObject == gameObject || alwaysShow)
        {
            //ハンドルの数が2つ以上のときのみ
            if (points.Count >= 2)
            {
                //ハンドルの数未満描画
                for (int i = 0; i < points.Count; i++)
                {
                    //最終ハンドルまでベジエ曲線を描画
                    if (i < points.Count - 1)
                    {
                        var index = points[i];
                        var indexNext = points[i + 1];
                        UnityEditor.Handles.DrawBezier(index.position, indexNext.position, index.position + index.handleNext,
                            indexNext.position + indexNext.handlePrev, ((UnityEditor.Selection.activeGameObject == gameObject) ? visual.pathColor : visual.inactivePathColor), null, 5);
                    }
#if false
                    else
                    {
                        var index = points[i];
                        var indexNext = points[0];
                        UnityEditor.Handles.DrawBezier(index.position, indexNext.position, index.position + index.handleNext,
                            indexNext.position + indexNext.handlePrev, ((UnityEditor.Selection.activeGameObject == gameObject) ? visual.pathColor : visual.inactivePathColor), null, 5);
                    }
#endif
                }
            }

#if false
            //オブジェクトに隠れてもパスが見えるよう設定
            for (int i = 0; i < points.Count; i++)
            {
                var index = points[i];
                //ハンドルの座標と回転を行列に代入
                Gizmos.matrix = Matrix4x4.TRS(index.position, index.rotation, Vector3.one);
                //行列の初期化
                Gizmos.matrix = Matrix4x4.identity;
            }
#endif
        }
    }
#endif

}

