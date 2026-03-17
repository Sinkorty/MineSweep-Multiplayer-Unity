using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSceneTest01 : MonoBehaviour
{
    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private int mineCount;

    private void Start()
    {
        MineController.Instance.Init(width, height, mineCount);
    }
}
