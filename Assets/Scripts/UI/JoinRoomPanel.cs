using Org.Sinkorty.MineSweepServer.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class JoinRoomPanel : BasePanel
{
    [SerializeField] private Transform contentRoot;
    [SerializeField] private Button btn_back;

    private GameObject tmpl_room;

    public override void OnHide()
    {
        print("OnHide invoked.");
        NetManager.RemoveMsgListener(MessageWrapper.MessageBodyOneofCase.QueryRoomMsg, OnQueryRoomMsgReceived);
        NetManager.RemoveMsgListener(MessageWrapper.MessageBodyOneofCase.JoinRoomMsg, OnJoinRoomMsgReceived);
        btn_back.onClick.RemoveAllListeners();

        // É¾µôËùÓÐµÄtmpl_room
        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(contentRoot.GetChild(i).gameObject);
        }
    }

    public override void OnShow()
    {
        print("OnShow invoked");
        btn_back.onClick.AddListener(OnBackButtonClicked);


        // 0.initialize all the references required.
        if (tmpl_room == null) tmpl_room = Resources.Load<GameObject>("Prefabs/UI/Tmpl_Room");

        // 1.query all the rooms.

        NetManager.AddMsgListener(MessageWrapper.MessageBodyOneofCase.QueryRoomMsg, OnQueryRoomMsgReceived);

        MessageWrapper toSend = new MessageWrapper() { QueryRoomMsg = new QueryRoomMsg() };
        NetManager.Send(toSend);

        // 3.add msg listener.
        NetManager.AddMsgListener(MessageWrapper.MessageBodyOneofCase.JoinRoomMsg, OnJoinRoomMsgReceived);

    }

    private void OnJoinRoomMsgReceived(MessageWrapper wrapper)
    {
        JoinRoomMsg msg = wrapper.JoinRoomMsg;
        if (!msg.IsSuccesful)
        {
            PanelManager.Instance.ShowErrMsg(msg.ErrInfo);
            return;
        }
        // GAME ENTRY
        GameManager.Instance.GameStart(msg.Width, msg.Height, msg.MineCount, msg.RoomId, msg.Players.ToList());
    }

    private void OnQueryRoomMsgReceived(MessageWrapper wrapper)
    {
        // 2.show them in the content column.
        foreach (QueryRoomMsg.Types.Room item in wrapper.QueryRoomMsg.Rooms)
        {
            GameObject go = Instantiate(tmpl_room, contentRoot);
            RoomTemplate roomTemplate = go.GetComponent<RoomTemplate>();
            roomTemplate.Init(item.RoomId, item.PlayerNum, item.Width, item.Height, item.MineCount);
        }
    }

    private void OnBackButtonClicked()
    {
        PanelManager.Instance.SwitchPanel(PanelManager.Layer.CHOOSE_OPTION);
    }
}
