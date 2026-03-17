using Org.Sinkorty.MineSweepServer.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChooseOptionPanel : BasePanel
{
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button joinRoomButton;
    [SerializeField] private TextMeshProUGUI txt_HelloName;
    [SerializeField] private TextMeshProUGUI txt_sign;
    [SerializeField] private Gradient gradient;

    [SerializeField] private float timeScale = 8f;
    [SerializeField] private float intensity = 0.3f;
    [SerializeField] private float breathSpeed = 1f;
    [SerializeField] private float rotatingSpeed = 1f;
    [SerializeField] private float maxAngle = Mathf.PI / 6f;

    float timer = 0;
    bool positive = true;
    Vector3 originalEularAngle = new Vector3(0, 0, -18f);

    public override void OnHide()
    {
        createRoomButton.onClick.RemoveAllListeners();
        joinRoomButton.onClick.RemoveAllListeners();
    }
    private void Update()
    {
        SignAnimate();
    }

    private void SignAnimate()
    {
        timer += (positive ? 1f : -1f) * Time.unscaledDeltaTime;
        timer = Mathf.Clamp(timer, 0, timeScale);
        if (timer == 0 || timer == timeScale) positive = !positive;

        Color color = gradient.Evaluate(timer / timeScale);
        txt_sign.color = color;

        float breath = Mathf.Abs(Mathf.Sin(Time.time * breathSpeed)) * intensity;
        txt_sign.rectTransform.localScale = Vector3.one * (1 + breath);

        float angle = Mathf.PingPong(Time.time * rotatingSpeed * 2, maxAngle * 2);
        txt_sign.rectTransform.localEulerAngles = originalEularAngle + Vector3.forward * angle;
    }
    public override void OnShow()
    {
        txt_HelloName.SetText($"Welcome to MineSweepOL!\n<b>{GameManager.Instance.playerName}</b>");
        createRoomButton.onClick.AddListener(() =>
        {
            PanelManager.Instance.SwitchPanel(PanelManager.Layer.CREATE_ROOM);
        });

        joinRoomButton.onClick.AddListener(() =>
        {
            PanelManager.Instance.SwitchPanel(PanelManager.Layer.JOIN_ROOM);
        });
    }
}
