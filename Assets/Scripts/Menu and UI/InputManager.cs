using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameAction
{
    CameraUp,
    CameraDown,
    CameraLeft,
    CameraRight,
    AttackMove,
    RotatePlacement
}

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private readonly Dictionary<GameAction, KeyCode> keyBindings = new Dictionary<GameAction, KeyCode>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadBindings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadBindings()
    {
        keyBindings[GameAction.CameraUp] = (KeyCode)PlayerPrefs.GetInt("Key_" + GameAction.CameraUp, (int)KeyCode.W);
        keyBindings[GameAction.CameraDown] = (KeyCode)PlayerPrefs.GetInt("Key_" + GameAction.CameraDown, (int)KeyCode.S);
        keyBindings[GameAction.CameraLeft] = (KeyCode)PlayerPrefs.GetInt("Key_" + GameAction.CameraLeft, (int)KeyCode.A);
        keyBindings[GameAction.CameraRight] = (KeyCode)PlayerPrefs.GetInt("Key_" + GameAction.CameraRight, (int)KeyCode.D);
        keyBindings[GameAction.AttackMove] = (KeyCode)PlayerPrefs.GetInt("Key_" + GameAction.AttackMove, (int)KeyCode.F);
        keyBindings[GameAction.RotatePlacement] = (KeyCode)PlayerPrefs.GetInt("Key_" + GameAction.RotatePlacement, (int)KeyCode.R);
    }

    public KeyCode GetKey(GameAction action)
    {
        if (keyBindings.TryGetValue(action, out KeyCode key))
        {
            return key;
        }
        return KeyCode.None;
    }

    public void SetKey(GameAction action, KeyCode newKey)
    {
        keyBindings[action] = newKey;
        PlayerPrefs.SetInt("Key_" + action, (int)newKey);
        PlayerPrefs.Save();
    }

    // Универсальные проверки для других контроллеров
    public bool GetActionKeyDown(GameAction action) => Input.GetKeyDown(GetKey(action));
    public bool GetActionKey(GameAction action) => Input.GetKey(GetKey(action));
}
