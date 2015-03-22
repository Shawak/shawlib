using System;
using System.Collections.Generic;

namespace ShawLib
{
    public class TypeSwitch
    {
        Dictionary<Type, Action<object>> matches = new Dictionary<Type, Action<object>>();

        public TypeSwitch Case<TType>(Action<TType> action)
        {
            matches.Add(typeof(TType), (x) => action((TType)x));
            return this;
        }

        public void Switch(object x)
        {
            matches[x.GetType()](x);
        }
    }

    public class TypeSwitchArgs
    {
        Dictionary<Type, Action<object, object[]>> matches = new Dictionary<Type, Action<object, object[]>>();

        public TypeSwitchArgs Case<TType>(Action<TType, object[]> action)
        {
            matches.Add(typeof(TType), (x, y) => action((TType)x, y));
            return this;
        }

        public void Switch(object x, object[] y)
        {
            matches[x.GetType()](x, y);
        }
    }

    public class TypeSwitchReturnableArgs
    {
        Dictionary<Type, Func<object, object>> matches = new Dictionary<Type, Func<object, object>>();

        public TypeSwitchReturnableArgs Case<TType, TParam>(Func<TParam, object> action)
        {
            matches.Add(typeof(TType), (x) => action((TParam)x));
            return this;
        }

        public object Switch(Type type, object x)
        {
            return matches[type](x);
        }
    }
}
