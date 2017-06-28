/*!
 *  @file           DestroyObject.cs
 *  @brief         消えるオブジェクト
 *  @date         2017/06/08
 *  @author      仁科香苗
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*! @brief 消えるオブジェクト*/
public class DestroyObject : MonoBehaviour
{
    /*! @brief 衝突検知*/
    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.tag == "DestroyZone")
        {
            Destroy(this.gameObject);
        }
    }
    
}
