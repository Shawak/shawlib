using System;
using System.Collections.Generic;
using System.Threading;

namespace ShawLib
{
    public class MethodLinker<TBase>
    {
        Dictionary<Type, Action<TBase>> handler;

        public MethodLinker()
        {
            handler = new Dictionary<Type, Action<TBase>>();
        }

        public void Link<T>(Action<T> action) where T : TBase
        {
            var type = typeof(T);
            if (handler.ContainsKey(type))
                throw new Exception(type.ToString() + " is already linked");

            handler[type] = new Action<TBase>(e => action((T)e));
        }

        public void Unlink<T>()
        {
            var type = typeof(T);
            if (!handler.ContainsKey(type))
                return;

            handler.Remove(type);
        }

        public void Call(Type type, TBase e)
        {
            if (!handler.ContainsKey(type))
                return;

            handler[type].Invoke(e);
        }

        public TResult Once<T, TResult>(Func<T, TResult> action, Func<bool> shouldWait, int timeout) where T : TBase
        {
            TResult ret = default(TResult);

            var handler = new ManualResetEvent(false);
            Link<T>(e =>
            {
                ret = action(e);
                handler.Set();
            });

            if (shouldWait())
                handler.WaitOne(timeout);

            Unlink<T>();
            return ret;
        }
    }
}
