using Org.Sinkorty.MineSweepServer.Message;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// VIEW
public class SyncGhost : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI txt_name;
    private string playerName;
    

    public void Init(string playerName,Color color)
    {
        this.playerName = playerName;
        txt_name.text = playerName;
        foreach (var item in GetComponentsInChildren<SpriteRenderer>())
        {
            item.color = color;
        }
    }
    public void Move(float x, float y)
    {
        transform.position = new Vector3(x, y, 0);
    }
    public bool IsPlayer(string name) => playerName.Equals(name);
}
