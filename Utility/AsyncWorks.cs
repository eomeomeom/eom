using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class AsyncWorks : Singleton<AsyncWorks>
{
    public delegate bool WorkDelegate();

    private class AsyncWorkEntry
    {
        public WorkDelegate work;
        public WorkDelegate checkFinish;
        public System.Action onPostFinish;
        public float delay;
        public float elapsedTime;
    }

    private WorkDelegate currentWorkFinishCheck = null;
    private System.Action currentOnPostFinish = null;
    private Queue<AsyncWorkEntry> pendingWorkList = new Queue<AsyncWorkEntry>();

    public override void Init()
    {
        base.Init();
        currentWorkFinishCheck = null;
        currentOnPostFinish = null;
        pendingWorkList.Clear();
    }

    private void Update()
    {
        ProcessAsyncWorks();
    }

    public void EnqueueAsyncWork(WorkDelegate work_, WorkDelegate checkFinish_, float delay_ = 0.0f, System.Action onPostFinish_ = null)
    {
        if (null == work_ && null == checkFinish_)
            return;

        if (0 != this.pendingWorkList.Count || null != this.currentWorkFinishCheck || delay_ > 0.0f)
        {
            this.pendingWorkList.Enqueue(
                new AsyncWorkEntry() { work = work_, checkFinish = checkFinish_, onPostFinish = onPostFinish_, delay = delay_, elapsedTime = 0.0f });

            return;
        }

        if (null != work_ && false == work_())
            return;

        if (null == checkFinish_ || true == checkFinish_())
        {
            if (null != onPostFinish_)
                onPostFinish_();

            return;
        }

        this.currentWorkFinishCheck = checkFinish_;
        this.currentOnPostFinish = onPostFinish_;
    }

    private void ProcessAsyncWorks()
    {
        if (null != this.currentWorkFinishCheck)
        {
            if (false == this.currentWorkFinishCheck())
                return;

            if (null != this.currentOnPostFinish)
                this.currentOnPostFinish();

            this.currentWorkFinishCheck = null;
            this.currentOnPostFinish = null;
        }

        float timeRemainder = Time.deltaTime;
        while (this.pendingWorkList.Count > 0)
        {
            AsyncWorkEntry entry = this.pendingWorkList.Peek();
            if (null == entry)
                continue;

            if (entry.delay > 0.0f)
            {
                entry.elapsedTime += timeRemainder;
                if (entry.elapsedTime < entry.delay)
                    break;
                else
                    timeRemainder = entry.elapsedTime - entry.delay;
            }

            this.pendingWorkList.Dequeue();

            if (null != entry.work && false == entry.work())
                continue;

            if (null == entry.checkFinish || true == entry.checkFinish())
            {
                if (null != entry.onPostFinish)
                    entry.onPostFinish();

                continue;
            }

            this.currentWorkFinishCheck = entry.checkFinish;
            this.currentOnPostFinish = entry.onPostFinish;
            break;
        }
    }
}
