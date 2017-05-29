using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReSpawn : MonoBehaviour
{
    public float respawnPos;
    public float height;
    public PlayerPath playerPath;
    public float fadeTime = 1f;                   //フェードにかける時間
    [SerializeField]
    FadeControl fade = null;

    private void Start() { }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.tag == "Player")
        {
            fade.FadeIn(fadeTime, () =>
            {
                playerPath.Respawn(respawnPos, height);
                fade.FadeOut(fadeTime, () =>
                {
                });
            });
        }
    }
}
