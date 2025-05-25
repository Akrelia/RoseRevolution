using System;
using System.Collections.Generic;
using UnityEngine;

public class AutoUnsubscriber : MonoBehaviour
{
    private readonly List<Action> unsubscribeActions = new();

    public void RegisterUnsubscribe(Action action)
    {
        unsubscribeActions.Add(action);
    }

    private void OnDestroy()
    {
        foreach (var action in unsubscribeActions)
        {
            action.Invoke();
        }
    }
}
