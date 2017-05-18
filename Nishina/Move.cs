using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InputGamePad;

public class Move : MonoBehaviour
{
    public PlayerPath playerPath = null;
    
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        playerPath.SetInput(GamePad.GetLeftStickAxis(true).x, 1);
    }

    private void LateUpdate()
    {
        transform.position += playerPath.GetAddPotision();
    }
}
