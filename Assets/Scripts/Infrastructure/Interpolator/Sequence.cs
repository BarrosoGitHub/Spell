using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Sequence
{
    private Queue<Action> actions = new Queue<Action>();
    private float currentTime =  0f;
    private bool cancelled;

    public void Cancel()
    {
        cancelled = true;
        actions.Clear();
    }

    public Sequence AppendInterval(float duration)
    {
        actions.Enqueue(() => { currentTime += duration; });
        return this;
    }

    public Sequence AppendCallback(Action callback)
    {
        actions.Enqueue(callback);
        return this;
    }

    public IEnumerator ExecuteActions()
    {
        while (!cancelled && actions.Count >  0)
        {
            var action = actions.Dequeue();
            action();
            while (!cancelled && currentTime >  0f)
            {
                currentTime -= Time.deltaTime;
                yield return null;
            }
            currentTime =  0;
        }
    }
}

