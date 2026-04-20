using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class BuffCard : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    [Header("UI References")]
    public TMP_Text buffNameText;
    public TMP_Text buffDescriptionText;

    [Header("Hover Settings")]
    [Tooltip("Scale the card pops to on hover.")]
    public float hoverScale = 1.08f;
    [Tooltip("How fast the card scales up/down.")]
    public float scaleSpeed = 10f;

    private BuffReward         _buff;
    private Action<BuffReward> _onSelected;
    private bool               _interactable = false;
    private bool               _hovering     = false;
    private Vector3            _baseScale;

    void Awake()
    {
        _baseScale = Vector3.one;
        transform.localScale = _baseScale;
    }

    void Update()
    {
        if (!_interactable) return;

        // Smoothly lerp toward hover or rest scale
        Debug.Log($"[BuffCard LIVE] Name: {buffNameText.text}");

        Vector3 targetScale = _hovering ? Vector3.one * hoverScale : Vector3.one;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * scaleSpeed);
    }

    public void Setup(BuffReward buff, Action<BuffReward> onSelected)
    {
        transform.localScale = Vector3.one;
        buffNameText.text = "VISIBLE TEST";
        buffNameText.color = Color.red;
        buffNameText.fontSize = 40;
        Debug.Log($"[BuffCard] Instance ID: {GetInstanceID()}");
        Debug.Log($"[BuffCard AFTER SET] Name: {buffNameText.text}");
        Debug.Log($"[BuffCard] Setup called — Type: {buff.buffType}, Value: {buff.value}");
        Debug.Log($"[BuffCard] buffNameText null: {buffNameText == null}, buffDescText null: {buffDescriptionText == null}");
        
        _buff       = buff;
        _onSelected = onSelected;
        _interactable = true;
        _hovering     = false;
        transform.localScale = _baseScale;

        if (buffNameText)        buffNameText.text        = GetBuffName(buff.buffType);
        if (buffDescriptionText) buffDescriptionText.text = GetBuffDescription(buff);
    }

    public void SetEmpty()
    {
        _interactable = false;
        _hovering     = false;
        transform.localScale = _baseScale;

        if (buffNameText)        buffNameText.text        = "—";
        if (buffDescriptionText) buffDescriptionText.text = "";
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_interactable) _hovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovering = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_interactable) return;
        _hovering = false;
        transform.localScale = _baseScale;
        _onSelected?.Invoke(_buff);
    }

    private string GetBuffName(BuffType type) => type switch
    {
        BuffType.MaxHealth     => "Reinforced Frame",
        BuffType.Damage        => "Sharpened Edge",
        BuffType.FireRate      => "Overclocked Chamber",
        BuffType.MovementSpeed => "Lightweight Rig",
        BuffType.BulletCount   => "Split Payload",
        _                      => type.ToString()
    };

    private string GetBuffDescription(BuffReward buff) => buff.buffType switch
    {
        BuffType.MaxHealth     => $"Increase max health by {buff.value}.",
        BuffType.Damage        => $"Deal {buff.value * 100:F0}% more damage.",
        BuffType.FireRate      => $"Fire {buff.value * 100:F0}% faster.",
        BuffType.MovementSpeed => $"Move {buff.value * 100:F0}% faster.",
        BuffType.BulletCount   => $"Fire {(int)buff.value} extra bullet(s) per shot.",
        _                      => buff.GetDescription()
    };
}