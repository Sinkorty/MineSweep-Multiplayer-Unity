using Org.Sinkorty.MineSweepServer.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoSingleton<GameManager>
{
    [SerializeField] private Color syncGhostColor = Color.blue;

    [HideInInspector] public string playerName;
    public bool HasState { get; private set; }

    private int roomId;
    private int width;
    private int height;
    private int mineCount;

    private GameObject syncGhostPrefab;
    private Dictionary<string, SyncGhost> syncGhostsDictionary = new Dictionary<string, SyncGhost>();

    List<string> playerListWhenEnter;
    bool isInitializing = false;

    private void Start()
    {
        DontDestroyOnLoad(this);
        syncGhostPrefab = Resources.Load<GameObject>("Prefabs/Game/Sync Ghost");
    }
    private void Update()
    {
        if (isInitializing)
        {
            isInitializing = false;
            Init();
        }
        NetManager.Update();

    }

    // PlayerName: 当前客户端的名字
    public void GameStart(int width, int height, int mineCount, int roomId, List<string> playerList) // 加载到主场景
    {
        // 游戏主要状态
        this.width = width;
        this.height = height;
        this.mineCount = mineCount;
        this.roomId = roomId;
        playerListWhenEnter = playerList;

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
    }
    // 注意这是个异步方法
    private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        isInitializing = true;
    }
    private void Init()
    {
        RegisterGameMessages();
        MineController.Instance.Init(width, height, mineCount);
        if (playerListWhenEnter != null)
        {
            foreach (string item in playerListWhenEnter)
            {
                if (!item.Equals(playerName))
                {
                    SpawnGhost(item);
                }
            }
        }
        MineController.Instance.UpdatePlayerText();
        // 进入场景，向服务端发送进入游戏
        NetManager.Send(new MessageWrapper() { PlayerEnterMSg = new PlayerEnterMsg() { PlayerName = playerName } });

        // 向服务端请求当前进度（progress）
        NetManager.Send(new MessageWrapper() { UpdateProgress = new UpdateProgress() { Requester = playerName } });
    }

    private void RegisterGameMessages()
    {
        NetManager.AddMsgListener(MessageWrapper.MessageBodyOneofCase.PlayerEnterMSg, OnPlayerEnterMsgReceived);
        //NetManager.AddMsgListener(MessageWrapper.MessageBodyOneofCase.QueryStateMsg, OnQueryStateMsgReceived);
        //NetManager.AddMsgListener(MessageWrapper.MessageBodyOneofCase.QueryProgressMsg, OnQueryProgressMsgReceived);
        NetManager.AddMsgListener(MessageWrapper.MessageBodyOneofCase.SetVeilOrFlagMsg, OnSetVeilOrFlagMsgReceived);
        NetManager.AddMsgListener(MessageWrapper.MessageBodyOneofCase.QueryStateMsg, OnQueryStateMsgReceived);
        NetManager.AddMsgListener(MessageWrapper.MessageBodyOneofCase.GhostMoveMsg, OnGhostMoveMsgReceived);
        NetManager.AddMsgListener(MessageWrapper.MessageBodyOneofCase.PlayerQuitRoom, OnPlayerQuitRoomMsgReceived);
        NetManager.AddMsgListener(MessageWrapper.MessageBodyOneofCase.UpdateProgress, OnUpdateProgressMsgReceived);
    }

    private void OnUpdateProgressMsgReceived(MessageWrapper wrapper)
    {
        print("OnUpdateProgressMsgReceived invoked.");
        UpdateProgress msg = wrapper.UpdateProgress;
        if (msg.Progress.Count == 0) // 说明是来请求 progress 数据的
        {
            // 直接给 服务端 就是了
            print("我给了");
            List<int> progressData = MineController.Instance.GetModel().EncodeProgress();
            UpdateProgress msgToSend = new UpdateProgress();
            msgToSend.Progress.AddRange(progressData);
            msgToSend.Requester = msg.Requester; // Requester 还原
            NetManager.Send(new MessageWrapper() { UpdateProgress = msgToSend });
        }
        else // 说明自己请求的 progress 数据给过来了
        {
            MineController.Instance.LoadProgress(msg.Progress.ToList());

            // 更新下文本
            MineController.Instance.UpdateMineRemainText();
            MineController.Instance.UpdateVeilNumText();
        }
    }

    // 别的玩家从room中退出了
    private void OnPlayerQuitRoomMsgReceived(MessageWrapper wrapper)
    {
        PlayerQuitRoom msg = wrapper.PlayerQuitRoom;
        RemovePlayer(msg.Name);
    }

    private void RemovePlayer(string name)
    {
        if (!syncGhostsDictionary.ContainsKey(name))
        {
            Debug.LogError($"Player [{name}] does not exist.");
            return;
        }
        DestroyImmediate(syncGhostsDictionary[name].gameObject);
        syncGhostsDictionary.Remove(name);
        MineController.Instance.UpdatePlayerText();
        Debug.Log($"Player [{name}] successfully removed");
    }

    private void OnGhostMoveMsgReceived(MessageWrapper wrapper)
    {
        GhostMoveMsg msg = wrapper.GhostMoveMsg;
        if (!syncGhostsDictionary.TryGetValue(msg.Name, out SyncGhost syncGhost))
        {
            Debug.LogError("Player doesn't exist: " + msg.Name);
            return;
        }
        syncGhost.Move(msg.X, msg.Y);
    }
    private void OnQueryStateMsgReceived(MessageWrapper wrapper)
    {
        QueryStateMsg msg = wrapper.QueryStateMsg;
        if (msg.HasState)
        {
            Debug.Log("On QueryStateMsgReceived INVOKED");
            MineController.Instance.GetModel().DecodeState(msg.State.ToList());
            //MineController.Instance.GetModel().DecodeProgress(msg.Progress.ToList());
            MineController.Instance.LoadProgress(msg.Progress.ToList());
            MineController.Instance.isFirstClick = true; // 和牢大测试的时候遇到的问题的解决方案
            //MineController.Instance.GetView().RefreshView();
        }
    }
    private void OnSetVeilOrFlagMsgReceived(MessageWrapper wrapper)
    {
        SetVeilOrFlagMsg msg = wrapper.SetVeilOrFlagMsg;
        if (msg.IsVeil)
        {
            MineController.Instance.Reveil(msg.X, msg.Y);
            MineController.Instance.UpdateVeilNumText();
        }
        else
        {
            MineController.Instance.SetFlag(msg.X, msg.Y);
            MineController.Instance.UpdateMineRemainText();
        }
    }
    private void OnPlayerEnterMsgReceived(MessageWrapper wrapper)
    {
        // spawn players
        foreach (string item in wrapper.PlayerEnterMSg.PlayerList)
        {
            print(item);
            // 不和自己相同，在场景中不存在
            if (!item.Equals(playerName) && !syncGhostsDictionary.ContainsKey(item))
            {
                print("+" + item);
                SpawnGhost(item);
            }
        }
        //// aquire state:
        //MessageWrapper msg = new MessageWrapper() { QueryStateMsg = new QueryStateMsg() };
        //NetManager.Send(msg);

        MineController.Instance.UpdatePlayerText();
    }

    // 生成syncGhost
    private SyncGhost SpawnGhost(string name)
    {
        if (name.Equals(playerName))
        {
            Debug.Log("Adding yourself?");
            return null;
        }
        if (syncGhostsDictionary.ContainsKey(name))
        {
            Debug.Log($"Plyaer [{name}] already exist.");
            return null;
        }
        SyncGhost ghost = Instantiate(syncGhostPrefab).GetComponent<SyncGhost>();
        syncGhostsDictionary.Add(name, ghost);
        ghost.Init(name, Color.blue);
        return ghost;
    }
    public string GetAllPlayerNames(string trim)
    {
        string names = string.Empty;
        foreach (string item in syncGhostsDictionary.Keys)
        {
            names += item + trim;
        }
        return names;
    }
}
