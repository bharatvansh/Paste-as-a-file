using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace PasteIt.Core
{
    public static class ClipboardAccessor
    {
        public static T Execute<T>(Func<T> action, int retryCount = 3, int retryDelayMs = 75)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                return ExecuteWithRetry(action, retryCount, retryDelayMs);
            }

            T result = default!;
            ExceptionDispatchInfo? dispatchInfo = null;

            var thread = new Thread(() =>
            {
                try
                {
                    result = ExecuteWithRetry(action, retryCount, retryDelayMs);
                }
                catch (Exception ex)
                {
                    dispatchInfo = ExceptionDispatchInfo.Capture(ex);
                }
            })
            {
                IsBackground = true
            };

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            dispatchInfo?.Throw();
            return result;
        }

        private static T ExecuteWithRetry<T>(Func<T> action, int retryCount, int retryDelayMs)
        {
            Exception? lastException = null;

            for (var attempt = 0; attempt <= retryCount; attempt++)
            {
                try
                {
                    return action();
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    if (attempt == retryCount)
                    {
                        break;
                    }

                    Thread.Sleep(retryDelayMs);
                }
            }

            throw lastException ?? new InvalidOperationException("Clipboard operation failed.");
        }
    }
}

