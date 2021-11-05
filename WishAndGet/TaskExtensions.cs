using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace WishAndGet
{
    public static class TaskExtensions
    {
        [DebuggerStepThrough]
        public static ConfiguredTaskAwaitable AnyContext(this Task task)
        {
            return task.ConfigureAwait(false);
        }

        [DebuggerStepThrough]
        public static ConfiguredTaskAwaitable<T> AnyContext<T>(this Task<T> task)
        {
            return task.ConfigureAwait(false);
        }
    }
}
