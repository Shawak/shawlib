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
        bool stop;

        public Dispatcher()
        {
            queue = new Queue<Task>();
            lockQueue = new object();
            stop = false;

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
            while (!stop)
                lock (lockQueue)
                    if (queue.Count > 0)
                        queue.Dequeue().RunSynchronously();
                    else
                        Thread.Sleep(1);
        }

        public void Shutdown()
        {
            stop = true;
        }

        public void Join()
        {
            thread.Join();
        }
    }
}
