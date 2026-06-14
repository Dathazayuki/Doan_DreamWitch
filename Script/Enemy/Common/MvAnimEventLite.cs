using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public interface IMvAnimEventLiteListener
{
    void OnMvAnimEvent(string eventName, MvAnimEventLite source);
}

[DisallowMultipleComponent]
public class MvAnimEventLite : MonoBehaviour
{
    [Serializable]
    public class StringEvent : UnityEvent<string> { }

    [Header("Dispatch")]
    [SerializeField] private bool sendToChildren = true;
    [SerializeField] private bool sendToParent = false;

    [Header("Optional Inspector Callback")]
    [SerializeField] private StringEvent onEventRaised;

    private readonly HashSet<string> triggeredThisFrame = new HashSet<string>(StringComparer.Ordinal);
    private readonly HashSet<string> raisingNow = new HashSet<string>(StringComparer.Ordinal);
    private readonly List<IMvAnimEventLiteListener> listeners = new List<IMvAnimEventLiteListener>();

    public event Action<string, MvAnimEventLite> EventRaised;

    public bool IsCancelReserve { get; private set; }
    public bool IsCancel { get; private set; }
    public bool IsCancelMove { get; private set; }

    public bool IsTriggered(string eventName)
    {
        return !string.IsNullOrEmpty(eventName) && triggeredThisFrame.Contains(eventName);
    }

    protected virtual void Awake()
    {
        CacheListeners();
    }

    protected virtual void OnEnable()
    {
        ResetFrameFlags();
        IsCancelReserve = false;
        IsCancel = false;
        IsCancelMove = false;
    }

    protected virtual void LateUpdate()
    {
        ResetFrameFlags();
    }

    [ContextMenu("Refresh Listener Cache")]
    public void CacheListeners()
    {
        listeners.Clear();

        if (sendToChildren)
        {
            GetComponentsInChildren(true, listeners);
        }
        else
        {
            GetComponents(listeners);
        }

        listeners.RemoveAll(listener => ReferenceEquals(listener, this));

        if (sendToParent)
        {
            Transform parent = transform.parent;
            if (parent != null)
            {
                IMvAnimEventLiteListener[] parentListeners = parent.GetComponents<IMvAnimEventLiteListener>();
                for (int i = 0; i < parentListeners.Length; i++)
                {
                    if (parentListeners[i] != null && !listeners.Contains(parentListeners[i]))
                    {
                        listeners.Add(parentListeners[i]);
                    }
                }
            }
        }
    }

    public void RaiseEvent(string eventName)
    {
        if (string.IsNullOrEmpty(eventName)) return;
        if (raisingNow.Contains(eventName)) return;

        raisingNow.Add(eventName);

        try
        {
            triggeredThisFrame.Add(eventName);

            switch (eventName)
            {
                case "CancelR":
                    IsCancelReserve = true;
                    break;
                case "CancelS":
                    IsCancel = true;
                    break;
                case "CancelMove":
                    IsCancelMove = true;
                    break;
                case "CancelE":
                    IsCancelReserve = false;
                    IsCancel = false;
                    IsCancelMove = false;
                    break;
            }

            for (int i = 0; i < listeners.Count; i++)
            {
                listeners[i]?.OnMvAnimEvent(eventName, this);
            }

            EventRaised?.Invoke(eventName, this);
            onEventRaised?.Invoke(eventName);
        }
        finally
        {
            raisingNow.Remove(eventName);
        }
    }

    private void ResetFrameFlags()
    {
        triggeredThisFrame.Clear();
    }

    // Optional overload for AnimationEvent(string)
    public void EventByName(string eventName) => RaiseEvent(eventName);
}
