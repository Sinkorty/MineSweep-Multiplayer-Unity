using Google.Protobuf.Collections;
using Org.Sinkorty.MineSweepServer.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MineController : MonoSingleton<MineController>
{
    private MineView view;
    private MineModule model;
    private Tilemap tilemap;
    public bool isFirstClick = false;
    private bool hasInit = false;
    private bool isOver = false;
    //private bool hasStateLoaded = false;

    // 为了省事
    [SerializeField] private TextMeshProUGUI txtMineRemains;
    [SerializeField] private TextMeshProUGUI txtVeilRemains;
    [SerializeField] private TextMeshProUGUI txtPlayers;
    [SerializeField] private TextMeshProUGUI txtCurrentState;


    public void Init(int width, int height, int mineCount)
    {
        model = new MineModule(width, height, mineCount);
        tilemap = GetComponent<Tilemap>();
        view = GetComponent<MineView>();
        hasInit = true;
        view.RefreshView();

        NetManager.AddMsgListener(MessageWrapper.MessageBodyOneofCase.PlayerEnterMSg, (wrapper) =>
        {
            //Debug.Log("Invoked!!!!!!");
            txtPlayers.SetText("<b>other players:</b>\n" + GameManager.Instance.GetAllPlayerNames("\n"));
        });
        UpdatePlayerText();
        UpdateMineRemainText();
        UpdateVeilNumText();
        txtCurrentState.SetText("<b>current state:</b>\nPROGRESS");
    }

    public void UpdatePlayerText()
    {
        txtPlayers.SetText("<b>other players:</b>\n" + GameManager.Instance.GetAllPlayerNames("\n"));
    }
    public void UpdateMineRemainText()
    {
        txtMineRemains.SetText("<b>Mine remains:</b> " + model.getMineCount());
    }
    public void UpdateVeilNumText()
    {
        txtVeilRemains.SetText("<b>Veil num:</b>" + model.GetVeilNum());
    }
    //private void Start()
    //{
    //    view.RefreshView();
    //}
    private void Update()
    {
        if (!hasInit || isOver) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3Int cellPos = MousePosToCell();
            if (!tilemap.HasTile(cellPos)) return;
            if (!isFirstClick) // 雷生成，同时给服务端发送 state
            {
                isFirstClick = true;
                model.SummonMines(cellPos.x, cellPos.y);
                SendStateToServer();
                //SendProgressToServer();
            }
            if (!model.IsFlaged[cellPos.x, cellPos.y] && !model.IsVeiled[cellPos.x, cellPos.y])
            {
                int cell = model.board[cellPos.x, cellPos.y];

                NetManager.Send(new MessageWrapper() { SetVeilOrFlagMsg = new SetVeilOrFlagMsg() { IsVeil = true, X = cellPos.x, Y = cellPos.y } });

                if (cell >= 1 && cell <= 8)
                {
                    model.SetVeil(cellPos.x, cellPos.y, true);
                    view.SetCell(cellPos.x, cellPos.y, model.ToIndex(cell, false));
                }
                if (cell == 0)
                {
                    Reveil(cellPos.x, cellPos.y);
                }
                if (cell == -1) // 扫到雷了
                {
                    Fail();
                    view.SetCell(cellPos.x, cellPos.y, model.ToIndex(cell, true));
                }
                if (model.IsWin()) Win();
                UpdateVeilNumText();
            }
        }
        if (Input.GetMouseButtonDown(1))
        {
            Vector3Int cellPos = MousePosToCell();
            if (tilemap.HasTile(cellPos))
            {
                SetFlag(cellPos.x, cellPos.y);
                NetManager.Send(new MessageWrapper() { SetVeilOrFlagMsg = new SetVeilOrFlagMsg() { IsVeil = false, X = cellPos.x, Y = cellPos.y } });

                // 要有 逻辑 先于 UI 的思维
                UpdateMineRemainText(); // 更新文本数据
                UpdateVeilNumText();
            }
        }
    }
    private Vector3Int MousePosToCell()
    {
        Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        pos.z = 0;
        return tilemap.WorldToCell(pos);
    }
    public void Reveil(int x, int y)
    {
        if (x < 1 || x > model.MaxWidth || y < 1 || y > model.MaxHeight) return;
        if (model.IsVeiled[x, y]) return;
        int cell = model.board[x, y];
        if (cell >= 1 && cell <= 8)
        {
            model.SetVeil(x, y, true);
            view.SetCell(x, y, model.ToIndex(cell, false));
        }
        if (cell == 0)
        {
            model.SetVeil(x, y, true);
            view.SetCell(x, y, model.ToIndex(0, false));
            Reveil(x - 1, y - 1);
            Reveil(x, y - 1);
            Reveil(x + 1, y - 1);
            Reveil(x + 1, y);
            Reveil(x + 1, y + 1);
            Reveil(x, y + 1);
            Reveil(x - 1, y + 1);
            Reveil(x - 1, y);
        }
    }
    public MineModule GetModel()
    {
        return model;
    }
    private void Win()
    {
        isOver = true;
        txtCurrentState.SetText("<b>current state:</b>\nWIN");
    }
    private void Fail()
    {
        isOver = true;
        txtCurrentState.SetText("<b>current state:</b>\nLOSE");
    }
    private void SendStateToServer()
    {
        MessageWrapper toSend = new MessageWrapper();
        QueryStateMsg msg = new QueryStateMsg();
        msg.State.AddRange(model.EncodeState());

        msg.Progress.AddRange(model.EncodeProgress());

        toSend.QueryStateMsg = msg;
        NetManager.Send(toSend);
    }

    public void SetFlag(int x, int y)
    {
        if (!model.IsVeiled[x, y])
        {
            model.SetFlag(x, y);
            view.SetCell(x, y, model.ToIndex(model.IsFlaged[x, y] ? -2 : 0, true));
            if (model.IsWin()) Win();
        }
    }
    /// <summary>
    /// 加载 Progress 数据的时候 顺便刷新视图
    /// </summary>
    /// <param name="progress"></param>
    public void LoadProgress(List<int> progress)
    {
        model.DecodeProgress(progress);
        view.RefreshView();
    }
}
