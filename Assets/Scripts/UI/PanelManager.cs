using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PanelManager : MonoSingleton<PanelManager>
{
    [SerializeField] private TextMeshProUGUI txt_errInfo;
    public enum Layer
    {
        START_CONNECT,
        CHOOSE_OPTION,
        JOIN_ROOM,
        CREATE_ROOM,
    }
    // 层级面板
    private Dictionary<Layer, BasePanel> layers = new Dictionary<Layer, BasePanel>();

    private bool errShowFlag = false;
    private string errStr = string.Empty;

    private void Init()
    {
        foreach (BasePanel layer in layers.Values)
        {
            layer.gameObject.SetActive(false);
        }
        layers[Layer.START_CONNECT].OnShow();
        layers[Layer.START_CONNECT].gameObject.SetActive(true);

        ShowErrMsg(string.Empty);
    }
    protected override void Awake()
    {
        base.Awake();
        layers.Add(Layer.START_CONNECT, transform.Find("Panel1_StartConnect").GetComponent<StartConnectPanel>());
        layers.Add(Layer.CHOOSE_OPTION, transform.Find("Panel2_ChooseOption").GetComponent<ChooseOptionPanel>());
        layers.Add(Layer.JOIN_ROOM, transform.Find("Panel3_JoinRoom").GetComponent<JoinRoomPanel>());
        layers.Add(Layer.CREATE_ROOM, transform.Find("Panel4_CreateRoom").GetComponent<CreateRoomPanel>());

        Init();
    }
    private void Update()
    {
        if (errShowFlag)
        {
            errShowFlag = false;
            txt_errInfo.SetText(errStr);
            errStr = string.Empty;
        }
    }
    // 单一显示 Panel
    public void SwitchPanel(Layer layer)
    {
        foreach (Layer other in layers.Keys)
        {
            if (other == layer)
            {
                layers[other].OnShow();
                layers[other].gameObject.SetActive(true);
            }
            else if (layers[other].gameObject.activeInHierarchy) // TODO: 很有可能是这里出问题了
            {
                layers[other].OnHide();
                layers[other].gameObject.SetActive(false);
            }
        }
    }
    // 防止异步线程调用导致文字无法显示
    public void ShowErrMsg(string err)
    {
        errShowFlag = true;
        errStr = err;
    }
}