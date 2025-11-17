using System.Threading;

namespace Unity.Industry.Viewer.Shared
{
    /// <summary>
    /// Utils related to parallel tasks.
    /// </summary>
    public static class TaskUtils
    {
        /// <summary>
        /// Cancel and dispose the given <paramref name="tokenSource"/> and set its reference to null.
        /// Don't use the <paramref name="tokenSource"/> after calling this method.
        /// Usage of <c>CancellationToken</c> produced by this <paramref name="tokenSource"/> is still valid.
        /// The method is safe for null reference.
        /// </summary>
        /// <param name="tokenSource"><c>CancellationTokenSource</c> to be cancelled.</param>
        public static void CancelTokenSource(ref CancellationTokenSource tokenSource)
        {
            var source = tokenSource;
            if (source != null)
            {
                tokenSource = null;
                source.Cancel();
                source.Dispose();
            }
        }
    }
}
