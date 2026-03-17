using Org.Sinkorty.MineSweepServer.Message;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomTemplate : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI txt_roomId;
    [SerializeField] private TextMeshProUGUI txt_playerNum;
    [SerializeField] private TextMeshProUGUI txt_state;
    [SerializeField] private Button btn_joinRoom;

    private int roomId;

    public void Init(int roomId, int playerNum, int width, int height, int mineCount)
    {
        this.roomId = roomId;
        txt_roomId.SetText($"房间ID：\n{roomId}");
        txt_playerNum.SetText($"玩家数量：\n{playerNum}");
        txt_state.SetText($"棋盘状态：\nW: {width} H: {height} M: {mineCount}");
        btn_joinRoom.onClick.AddListener(OnClick);
    }
    private void OnClick()
    {
        MessageWrapper wrapper = new MessageWrapper() { JoinRoomMsg = new JoinRoomMsg() { RoomId = roomId } };
        NetManager.Send(wrapper);
    }
}
