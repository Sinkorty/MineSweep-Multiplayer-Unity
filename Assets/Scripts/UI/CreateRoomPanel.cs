using Org.Sinkorty.MineSweepServer.Message;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 相异于 StartConnectPanel 的写法，看上去更简洁
public class CreateRoomPanel : BasePanel
{
    [SerializeField] private TMP_InputField if_width;
    [SerializeField] private TMP_InputField if_height;
    [SerializeField] private TMP_InputField if_mineCount;
    [SerializeField] private Button btn_createRoom;
    [SerializeField] private Button btn_back;

    public override void OnHide()
    {
        NetManager.RemoveMsgListener(MessageWrapper.MessageBodyOneofCase.CreateRoomMsg, OnCreateRoomReceived);
        btn_createRoom.onClick.RemoveListener(OnClick);
        btn_back.onClick.RemoveListener(OnBackButtonClicked);
    }
    public override void OnShow()
    {
        btn_createRoom.onClick.AddListener(OnClick);
        btn_back.onClick.AddListener(OnBackButtonClicked);
        NetManager.AddMsgListener(MessageWrapper.MessageBodyOneofCase.CreateRoomMsg, OnCreateRoomReceived);
    }
    private void OnClick()
    {
        if (!InputValidUtil.CheckSizeAndMineCount(if_width.text, if_height.text, if_mineCount.text,
            out string err, out int width, out int height, out int mineCount))
        {
            PanelManager.Instance.ShowErrMsg(err);
            return;
        }
        MessageWrapper wrapper = new MessageWrapper() { CreateRoomMsg = new CreateRoomMsg() { Width = width, Height = height, MineCount = mineCount } };
        NetManager.Send(wrapper);

    }
    private void OnCreateRoomReceived(MessageWrapper wrapper)
    {
        CreateRoomMsg msg = wrapper.CreateRoomMsg;
        //GameManager.Instance.roomId = msg.CreateRoomMsg.RoomId;
        GameManager.Instance.GameStart(msg.Width, msg.Height, msg.MineCount, msg.RoomId, null);
    }

    private void OnBackButtonClicked()
    {
        PanelManager.Instance.SwitchPanel(PanelManager.Layer.CHOOSE_OPTION);
    }
}
