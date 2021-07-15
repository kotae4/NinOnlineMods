using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NinMods.Hooking;

namespace NinMods
{
    public enum EHookExecutionState
    {
        Pre,
        Post
    };

    public abstract class HookData
    {
        public bool FirstRun = true;
        protected Type TargetType, HookType;
        protected string TargetMethodName, HookMethodName;
        protected int HookPriority;
        public ManagedHooker.HookEntry Hook;
        // placeholder just so we can call them from a HookData instance w/out casting up to the actual derived type.
        // the derived, generic types will declare these same field names (but w/ different types) and use the 'new' keyword
        // so hopefully at runtime invoking Pre and Post from HookData instance will end up calling each derived type's field instance.
        public event Action Pre;
        public event Action Post;

        protected HookData(Type targetType, string targetMethodName, Type hookType, string hookMethodName, int hookPriority = 0)
        {
            TargetType = targetType;
            TargetMethodName = targetMethodName;
            HookType = hookType;
            HookMethodName = hookMethodName;
            HookPriority = hookPriority;
        }

        public abstract bool TryHook();

        public bool IsHooked()
        {
            return Hook != null;
        }
    }
    // this is the C# way of doing things... (refer to Func<...> and Action<...>)
    // C# doesn't support variadic templates like C++, so we have to do this.
    // AND because 'void' isn't a real type in C#, we have to have separate but identical classes, one with TOut and one without (uses void)
    public class GenericGameHookClass_Void<T1> : HookData
    {
        public delegate void dHookDel(T1 first);
        new public event Action<T1> Pre;
        new public event Action<T1> Post;

        public GenericGameHookClass_Void(Type targetType, string targetMethodName, Type hookType, string hookMethodName, int hookPriority = 0)
            : base(targetType, targetMethodName, hookType, hookMethodName, hookPriority) { }

        public override bool TryHook()
        {
            Hook = ManagedHooker.HookMethod<dHookDel>(TargetType, TargetMethodName, HookType, HookMethodName, HookPriority);
            return Hook == null;
        }

        public void FireEvent(T1 first, EHookExecutionState state)
        {
            if (state == EHookExecutionState.Pre)
                Pre?.Invoke(first);
            else
                Post?.Invoke(first);
        }
    }

    public class GenericGameHookClass_Void<T1, T2> : HookData
    {
        public delegate void dHookDel(T1 first, T2 second);
        new public event Action<T1, T2> Pre;
        new public event Action<T1, T2> Post;

        public GenericGameHookClass_Void(Type targetType, string targetMethodName, Type hookType, string hookMethodName, int hookPriority = 0)
            : base(targetType, targetMethodName, hookType, hookMethodName, hookPriority) { }

        public override bool TryHook()
        {
            Hook = ManagedHooker.HookMethod<dHookDel>(TargetType, TargetMethodName, HookType, HookMethodName, HookPriority);
            return Hook == null;
        }

        public void FireEvent(T1 first, T2 second, EHookExecutionState state)
        {
            if (state == EHookExecutionState.Pre)
                Pre?.Invoke(first, second);
            else
                Post?.Invoke(first, second);
        }
    }

    public class GenericGameHookClass_Void<T1, T2, T3> : HookData
    {
        public delegate void dHookDel(T1 first, T2 second, T3 third);
        new public event Action<T1, T2, T3> Pre;
        new public event Action<T1, T2, T3> Post;

        public GenericGameHookClass_Void(Type targetType, string targetMethodName, Type hookType, string hookMethodName, int hookPriority = 0)
            : base(targetType, targetMethodName, hookType, hookMethodName, hookPriority) { }

        public override bool TryHook()
        {
            Hook = ManagedHooker.HookMethod<dHookDel>(TargetType, TargetMethodName, HookType, HookMethodName, HookPriority);
            return Hook == null;
        }

        public void FireEvent(T1 first, T2 second, T3 third, EHookExecutionState state)
        {
            if (state == EHookExecutionState.Pre)
                Pre?.Invoke(first, second, third);
            else
                Post?.Invoke(first, second, third);
        }
    }

    public class GenericGameHookClass_Void<T1, T2, T3, T4> : HookData
    {
        public delegate void dHookDel(T1 first, T2 second, T3 third, T4 fourth);
        new public event Action<T1, T2, T3, T4> Pre;
        new public event Action<T1, T2, T3, T4> Post;

        public GenericGameHookClass_Void(Type targetType, string targetMethodName, Type hookType, string hookMethodName, int hookPriority = 0)
            : base(targetType, targetMethodName, hookType, hookMethodName, hookPriority) { }

        public override bool TryHook()
        {
            Hook = ManagedHooker.HookMethod<dHookDel>(TargetType, TargetMethodName, HookType, HookMethodName, HookPriority);
            return Hook == null;
        }

        public void FireEvent(T1 first, T2 second, T3 third, T4 fourth, EHookExecutionState state)
        {
            if (state == EHookExecutionState.Pre)
                Pre?.Invoke(first, second, third, fourth);
            else
                Post?.Invoke(first, second, third, fourth);
        }
    }

    public class GenericGameHookClass_Void<T1, T2, T3, T4, T5> : HookData
    {
        public delegate void dHookDel(T1 first, T2 second, T3 third, T4 fourth, T5 fifth);
        new public event Action<T1, T2, T3, T4, T5> Pre;
        new public event Action<T1, T2, T3, T4, T5> Post;

        public GenericGameHookClass_Void(Type targetType, string targetMethodName, Type hookType, string hookMethodName, int hookPriority = 0)
            : base(targetType, targetMethodName, hookType, hookMethodName, hookPriority) { }

        public override bool TryHook()
        {
            Hook = ManagedHooker.HookMethod<dHookDel>(TargetType, TargetMethodName, HookType, HookMethodName, HookPriority);
            return Hook == null;
        }

        public void FireEvent(T1 first, T2 second, T3 third, T4 fourth, T5 fifth, EHookExecutionState state)
        {
            if (state == EHookExecutionState.Pre)
                Pre?.Invoke(first, second, third, fourth, fifth);
            else
                Post?.Invoke(first, second, third, fourth, fifth);
        }
    }

    public class GenericGameHookClass_Void<T1, T2, T3, T4, T5, T6> : HookData
    {
        public delegate void dHookDel(T1 first, T2 second, T3 third, T4 fourth, T5 fifth, T6 sixth);
        new public event Action<T1, T2, T3, T4, T5, T6> Pre;
        new public event Action<T1, T2, T3, T4, T5, T6> Post;

        public GenericGameHookClass_Void(Type targetType, string targetMethodName, Type hookType, string hookMethodName, int hookPriority = 0)
            : base(targetType, targetMethodName, hookType, hookMethodName, hookPriority) { }

        public override bool TryHook()
        {
            Hook = ManagedHooker.HookMethod<dHookDel>(TargetType, TargetMethodName, HookType, HookMethodName, HookPriority);
            return Hook == null;
        }

        public void FireEvent(T1 first, T2 second, T3 third, T4 fourth, T5 fifth, T6 sixth, EHookExecutionState state)
        {
            if (state == EHookExecutionState.Pre)
                Pre?.Invoke(first, second, third, fourth, fifth, sixth);
            else
                Post?.Invoke(first, second, third, fourth, fifth, sixth);
        }
    }

    public class GenericGameHookClass_Void<T1, T2, T3, T4, T5, T6, T7> : HookData
    {
        public delegate void dHookDel(T1 first, T2 second, T3 third, T4 fourth, T5 fifth, T6 sixth, T7 seventh);
        new public event Action<T1, T2, T3, T4, T5, T6, T7> Pre;
        new public event Action<T1, T2, T3, T4, T5, T6, T7> Post;

        public GenericGameHookClass_Void(Type targetType, string targetMethodName, Type hookType, string hookMethodName, int hookPriority = 0)
            : base(targetType, targetMethodName, hookType, hookMethodName, hookPriority) { }

        public override bool TryHook()
        {
            Hook = ManagedHooker.HookMethod<dHookDel>(TargetType, TargetMethodName, HookType, HookMethodName, HookPriority);
            return Hook == null;
        }

        public void FireEvent(T1 first, T2 second, T3 third, T4 fourth, T5 fifth, T6 sixth, T7 seventh, EHookExecutionState state)
        {
            if (state == EHookExecutionState.Pre)
                Pre?.Invoke(first, second, third, fourth, fifth, sixth, seventh);
            else
                Post?.Invoke(first, second, third, fourth, fifth, sixth, seventh);
        }
    }

    public class GenericGameHookClass_Void<T1, T2, T3, T4, T5, T6, T7, T8> : HookData
    {
        public delegate void dHookDel(T1 first, T2 second, T3 third, T4 fourth, T5 fifth, T6 sixth, T7 seventh, T8 eighth);
        new public event Action<T1, T2, T3, T4, T5, T6, T7, T8> Pre;
        new public event Action<T1, T2, T3, T4, T5, T6, T7, T8> Post;

        public GenericGameHookClass_Void(Type targetType, string targetMethodName, Type hookType, string hookMethodName, int hookPriority = 0)
            : base(targetType, targetMethodName, hookType, hookMethodName, hookPriority) { }

        public override bool TryHook()
        {
            Hook = ManagedHooker.HookMethod<dHookDel>(TargetType, TargetMethodName, HookType, HookMethodName, HookPriority);
            return Hook == null;
        }

        public void FireEvent(T1 first, T2 second, T3 third, T4 fourth, T5 fifth, T6 sixth, T7 seventh, T8 eighth, EHookExecutionState state)
        {
            if (state == EHookExecutionState.Pre)
                Pre?.Invoke(first, second, third, fourth, fifth, sixth, seventh, eighth);
            else
                Post?.Invoke(first, second, third, fourth, fifth, sixth, seventh, eighth);
        }
    }

    // the "Func<...>" versions where we expect a non-void return type...
    public class GenericGameHookClass<T1, TOut> : HookData
    {
        public delegate TOut dHookDel(T1 first);
        new public event Action<T1> Pre;
        new public event Action<T1> Post;

        public GenericGameHookClass(Type targetType, string targetMethodName, Type hookType, string hookMethodName, int hookPriority = 0)
            : base(targetType, targetMethodName, hookType, hookMethodName, hookPriority) { }

        public override bool TryHook()
        {
            Hook = ManagedHooker.HookMethod<dHookDel>(TargetType, TargetMethodName, HookType, HookMethodName, HookPriority);
            return Hook == null;
        }

        public void FireEvent(T1 first, EHookExecutionState state)
        {
            if (state == EHookExecutionState.Pre)
                Pre?.Invoke(first);
            else
                Post?.Invoke(first);
        }
    }

    public class GenericGameHookClass<T1, T2, TOut> : HookData
    {
        public delegate TOut dHookDel(T1 first, T2 second);
        new public event Action<T1, T2> Pre;
        new public event Action<T1, T2> Post;

        public GenericGameHookClass(Type targetType, string targetMethodName, Type hookType, string hookMethodName, int hookPriority = 0)
            : base(targetType, targetMethodName, hookType, hookMethodName, hookPriority) { }

        public override bool TryHook()
        {
            Hook = ManagedHooker.HookMethod<dHookDel>(TargetType, TargetMethodName, HookType, HookMethodName, HookPriority);
            return Hook == null;
        }

        public void FireEvent(T1 first, T2 second, EHookExecutionState state)
        {
            if (state == EHookExecutionState.Pre)
                Pre?.Invoke(first, second);
            else
                Post?.Invoke(first, second);
        }
    }

    public class GenericGameHookClass<T1, T2, T3, TOut> : HookData
    {
        public delegate TOut dHookDel(T1 first, T2 second, T3 third);
        new public event Action<T1, T2, T3> Pre;
        new public event Action<T1, T2, T3> Post;

        public GenericGameHookClass(Type targetType, string targetMethodName, Type hookType, string hookMethodName, int hookPriority = 0)
            : base(targetType, targetMethodName, hookType, hookMethodName, hookPriority) { }

        public override bool TryHook()
        {
            Hook = ManagedHooker.HookMethod<dHookDel>(TargetType, TargetMethodName, HookType, HookMethodName, HookPriority);
            return Hook == null;
        }

        public void FireEvent(T1 first, T2 second, T3 third, EHookExecutionState state)
        {
            if (state == EHookExecutionState.Pre)
                Pre?.Invoke(first, second, third);
            else
                Post?.Invoke(first, second, third);
        }
    }

    public class GenericGameHookClass<T1, T2, T3, T4, TOut> : HookData
    {
        public delegate TOut dHookDel(T1 first, T2 second, T3 third, T4 fourth);
        new public event Action<T1, T2, T3, T4> Pre;
        new public event Action<T1, T2, T3, T4> Post;

        public GenericGameHookClass(Type targetType, string targetMethodName, Type hookType, string hookMethodName, int hookPriority = 0)
            : base(targetType, targetMethodName, hookType, hookMethodName, hookPriority) { }

        public override bool TryHook()
        {
            Hook = ManagedHooker.HookMethod<dHookDel>(TargetType, TargetMethodName, HookType, HookMethodName, HookPriority);
            return Hook == null;
        }

        public void FireEvent(T1 first, T2 second, T3 third, T4 fourth, EHookExecutionState state)
        {
            if (state == EHookExecutionState.Pre)
                Pre?.Invoke(first, second, third, fourth);
            else
                Post?.Invoke(first, second, third, fourth);
        }
    }

    public class GenericGameHookClass<T1, T2, T3, T4, T5, TOut> : HookData
    {
        public delegate TOut dHookDel(T1 first, T2 second, T3 third, T4 fourth, T5 fifth);
        new public event Action<T1, T2, T3, T4, T5> Pre;
        new public event Action<T1, T2, T3, T4, T5> Post;

        public GenericGameHookClass(Type targetType, string targetMethodName, Type hookType, string hookMethodName, int hookPriority = 0)
            : base(targetType, targetMethodName, hookType, hookMethodName, hookPriority) { }

        public override bool TryHook()
        {
            Hook = ManagedHooker.HookMethod<dHookDel>(TargetType, TargetMethodName, HookType, HookMethodName, HookPriority);
            return Hook == null;
        }

        public void FireEvent(T1 first, T2 second, T3 third, T4 fourth, T5 fifth, EHookExecutionState state)
        {
            if (state == EHookExecutionState.Pre)
                Pre?.Invoke(first, second, third, fourth, fifth);
            else
                Post?.Invoke(first, second, third, fourth, fifth);
        }
    }

    public class GenericGameHookClass<T1, T2, T3, T4, T5, T6, TOut> : HookData
    {
        public delegate TOut dHookDel(T1 first, T2 second, T3 third, T4 fourth, T5 fifth, T6 sixth);
        new public event Action<T1, T2, T3, T4, T5, T6> Pre;
        new public event Action<T1, T2, T3, T4, T5, T6> Post;

        public GenericGameHookClass(Type targetType, string targetMethodName, Type hookType, string hookMethodName, int hookPriority = 0)
            : base(targetType, targetMethodName, hookType, hookMethodName, hookPriority) { }

        public override bool TryHook()
        {
            Hook = ManagedHooker.HookMethod<dHookDel>(TargetType, TargetMethodName, HookType, HookMethodName, HookPriority);
            return Hook == null;
        }

        public void FireEvent(T1 first, T2 second, T3 third, T4 fourth, T5 fifth, T6 sixth, EHookExecutionState state)
        {
            if (state == EHookExecutionState.Pre)
                Pre?.Invoke(first, second, third, fourth, fifth, sixth);
            else
                Post?.Invoke(first, second, third, fourth, fifth, sixth);
        }
    }

    public class GenericGameHookClass<T1, T2, T3, T4, T5, T6, T7, TOut> : HookData
    {
        public delegate TOut dHookDel(T1 first, T2 second, T3 third, T4 fourth, T5 fifth, T6 sixth, T7 seventh);
        new public event Action<T1, T2, T3, T4, T5, T6, T7> Pre;
        new public event Action<T1, T2, T3, T4, T5, T6, T7> Post;

        public GenericGameHookClass(Type targetType, string targetMethodName, Type hookType, string hookMethodName, int hookPriority = 0)
            : base(targetType, targetMethodName, hookType, hookMethodName, hookPriority) { }

        public override bool TryHook()
        {
            Hook = ManagedHooker.HookMethod<dHookDel>(TargetType, TargetMethodName, HookType, HookMethodName, HookPriority);
            return Hook == null;
        }

        public void FireEvent(T1 first, T2 second, T3 third, T4 fourth, T5 fifth, T6 sixth, T7 seventh, EHookExecutionState state)
        {
            if (state == EHookExecutionState.Pre)
                Pre?.Invoke(first, second, third, fourth, fifth, sixth, seventh);
            else
                Post?.Invoke(first, second, third, fourth, fifth, sixth, seventh);
        }
    }

    public class GenericGameHookClass<T1, T2, T3, T4, T5, T6, T7, T8, TOut> : HookData
    {
        public delegate TOut dHookDel(T1 first, T2 second, T3 third, T4 fourth, T5 fifth, T6 sixth, T7 seventh, T8 eighth);
        new public event Action<T1, T2, T3, T4, T5, T6, T7, T8> Pre;
        new public event Action<T1, T2, T3, T4, T5, T6, T7, T8> Post;

        public GenericGameHookClass(Type targetType, string targetMethodName, Type hookType, string hookMethodName, int hookPriority = 0)
            : base(targetType, targetMethodName, hookType, hookMethodName, hookPriority) { }

        public override bool TryHook()
        {
            Hook = ManagedHooker.HookMethod<dHookDel>(TargetType, TargetMethodName, HookType, HookMethodName, HookPriority);
            return Hook == null;
        }

        public void FireEvent(T1 first, T2 second, T3 third, T4 fourth, T5 fifth, T6 sixth, T7 seventh, T8 eighth, EHookExecutionState state)
        {
            if (state == EHookExecutionState.Pre)
                Pre?.Invoke(first, second, third, fourth, fifth, sixth, seventh, eighth);
            else
                Post?.Invoke(first, second, third, fourth, fifth, sixth, seventh, eighth);
        }
    }

    // and a non-generic version for the void return type AND no arg cases...
    public class GenericGameHookClass_Void : HookData
    {
        public delegate void dHookDel();
        new public event Action Pre;
        new public event Action Post;

        public GenericGameHookClass_Void(Type targetType, string targetMethodName, Type hookType, string hookMethodName, int hookPriority = 0)
            : base(targetType, targetMethodName, hookType, hookMethodName, hookPriority) { }

        public override bool TryHook()
        {
            Hook = ManagedHooker.HookMethod<dHookDel>(TargetType, TargetMethodName, HookType, HookMethodName, HookPriority);
            return Hook == null;
        }

        public void FireEvent(EHookExecutionState state)
        {
            if (state == EHookExecutionState.Pre)
                Pre?.Invoke();
            else
                Post?.Invoke();
        }
    }
}