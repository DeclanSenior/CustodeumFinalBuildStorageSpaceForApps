using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public abstract class Button : MonoBehaviour
{

    [SerializeField] private Sprite _mouseNotOverSprite;
    [SerializeField] private Sprite _mouseOverSprite;

    private bool _mouseOver;

    private void Awake()
    {
        _mouseOver = false;
    }

    protected void OnMouseEnter()
    {
        if (!_mouseOver)
        {
            _mouseOver = true;
            this.GetComponent<SpriteRenderer>().sprite = _mouseOverSprite;
        }
    }

    protected void OnMouseExit()
    {
        if (_mouseOver)
        {
            _mouseOver = false;
            this.GetComponent<SpriteRenderer>().sprite = _mouseNotOverSprite;
        }
    }

    protected virtual void OnMouseUpAsButton()
    {
        OnClick();
        StartCoroutine(FlashUponClick());
    }
    protected IEnumerator FlashUponClick()
    {
        this.GetComponent<SpriteRenderer>().sprite = _mouseNotOverSprite;
        yield return new WaitForSeconds(0.1f);
        if (_mouseOver) this.GetComponent<SpriteRenderer>().sprite = _mouseOverSprite;
    }
    protected abstract void OnClick();
}
