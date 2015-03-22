using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShawLib
{
    public class Dispatcher
    {
        Thread thread;
        Queue<Task> queue;
        object lockQueue;
        bool stop, wait;

        public Dispatcher()
        {
            queue = new Queue<Task>();
            lockQueue = new object();

            thread = new Thread(taskDispatcher);
            thread.IsBackground = true;
            thread.Start();
        }

        public void Add(Action action)
        {
            Add(new Task(() => action()));
        }

        public void Add(Task task)
        {
            queue.Enqueue(task);
        }

        public void taskDispatcher()
        {
            Task task;
            while (true)
            {
                if (stop && (!wait || wait && queue.Count == 0))
                    break;
                else if (queue.Count > 0)
                {
                    lock (lockQueue)
                        task = queue.Dequeue();
                    task.RunSynchronously();
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
        }

        public void Shutdown(bool waitForPendingTasks = true)
        {
            stop = true;
            wait = waitForPendingTasks;
        }

        public void Join()
        {
            thread.Join();
        }
    }
}
