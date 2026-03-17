using Org.Sinkorty.MineSweepServer.Message;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GhostController : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap;
    private float posUpdateTimer = 0f;
    private float postPosTimer = 0f;

    Vector3 worldPos;
    float lastX;
    float lastY;

    private void Update()
    {
        posUpdateTimer += Time.unscaledDeltaTime;
        if (posUpdateTimer >= 0.02f)
        {
            Vector3Int pos = new Vector3Int(0, 0, 0);
            if (IsMouseOverTilemap(out pos))
            {
                transform.position = tilemap.CellToWorld(pos);
                worldPos = transform.position;
                worldPos.x = worldPos.x + 0.5f;
                worldPos.y = worldPos.y + 0.5f;
                transform.position = worldPos;
            }
            posUpdateTimer = 0;
        }
        // 计时器，每过0.6秒向服务端提交一次ghost位置数据（if changed）
        postPosTimer += Time.unscaledDeltaTime;
        if (postPosTimer >= 0.6f)
        {
            if (lastX != worldPos.x || lastY != worldPos.y)
            {
                lastX = worldPos.x; lastY = worldPos.y;
                //Debug.Log("ghost pos sent");
                NetManager.Send(new MessageWrapper()
                {
                    GhostMoveMsg = new GhostMoveMsg()
                    {
                        Name = GameManager.Instance.playerName,
                        X = worldPos.x,
                        Y = worldPos.y
                    }
                });
            }
        }
    }
    private bool IsMouseOverTilemap(out Vector3Int pos)
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPos = tilemap.WorldToCell(mouseWorldPos);
        pos = cellPos;
        return tilemap.HasTile(cellPos);
    }
}
