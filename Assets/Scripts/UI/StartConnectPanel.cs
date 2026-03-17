using Org.Sinkorty.MineSweepServer.Message;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StartConnectPanel : BasePanel
{
    [SerializeField] private TMP_InputField if_host;
    [SerializeField] private TMP_InputField if_port;
    [SerializeField] private TMP_InputField if_playerName;
    [SerializeField] private Button btnConnect;

    public override void OnShow()
    {
        btnConnect.onClick.AddListener(() =>
        {
            PanelManager.Instance.ShowErrMsg(string.Empty);


            string host = if_host.text;
            string port = if_port.text;
            string playerName = if_playerName.text;

            if (!InputValidUtil.CheckHostAndPort(host, port, out string err))
            {
                PanelManager.Instance.ShowErrMsg(err);
                return;
            }
            if (!InputValidUtil.CheckPlayerName(playerName, out err))
            {
                PanelManager.Instance.ShowErrMsg(err);
                return;
            }

            NetManager.AddEventListener(NetEvent.CONNECT_SUCC, (_) => // 当连接成功 发送PlayerName再次验证
            {
                MessageWrapper wrapper = new MessageWrapper() { ConnectServerMsg = new ConnectServerMsg() { PlayerName = playerName } };
                // 先注册 inbound ，再发送
                NetManager.AddMsgListener(MessageWrapper.MessageBodyOneofCase.ConnectServerMsg, (msg) =>
                {
                    // 自定义的连接失败，和别的玩家重名了
                    if (!msg.ConnectServerMsg.IsSuccesful)
                    {
                        PanelManager.Instance.ShowErrMsg("Please rename, already exist.");
                        NetManager.Close();
                    }
                    else // VERIFIED SUCCEED.
                    {
                        // 成功通过验证：1.切换Panel 2.关闭事件所有监听（在OnHide中实现）
                        GameManager.Instance.playerName = playerName;
                        PanelManager.Instance.SwitchPanel(PanelManager.Layer.CHOOSE_OPTION);
                    }
                });
                // 发送
                NetManager.Send(wrapper);
            });
            NetManager.AddEventListener(NetEvent.CONNECT_FAIL, (message) =>
            {
                PanelManager.Instance.ShowErrMsg($"Connect failed: {message}");
            });
            NetManager.Connect(host, int.Parse(port));
        });
    }
    // StartConnectPanel 的生命周期应该与 ConnectServerMsg以及相关 NetEvent 强关联
    public override void OnHide()
    {
        btnConnect.onClick.RemoveAllListeners();
        NetManager.RemoveEventAllListener(NetEvent.CONNECT_SUCC);
        NetManager.RemoveEventAllListener(NetEvent.CONNECT_FAIL);
        NetManager.RemoveMsgAllListener(MessageWrapper.MessageBodyOneofCase.ConnectServerMsg);
    }
}
