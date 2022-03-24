using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace OrleansContrib.Tester.Internals;


    /// <summary>
    /// Predetermined retry operation, used with <see cref="RetryHelper"/>.
    /// </summary>
    /// <remarks>A very crude system for current tests.</remarks>
    public static class RetryOperation
    {
        /// <summary>
        /// Produces sigmoid shaped delay curve.
        /// </summary>
        /// <param name="retryAttempt">The current retry attempt.</param>
        /// <returns>Delay for the next attempt to retry.</returns>
        /// <remarks>This function has not received deep scrutiny.</remarks>
        public static TimeSpan Sigmoid(int retryAttempt)
        {
            const int maxDurationInSeconds = 50;
            return TimeSpan.FromMilliseconds(Convert.ToInt32(Math.Round((1 / (1 + Math.Exp(-retryAttempt + 3))) * maxDurationInSeconds)) * 1000);
        }


        /// <summary>
        /// Produces a linear delay curve in increments of 10 ms.
        /// </summary>
        /// <param name="retryAttempt">The current retry attempt.</param>
        /// <remarks>This function has not received deep scrutiny.</remarks>
        public static TimeSpan LinearWithTenMilliseconds(int retryAttempt)
        {
            return TimeSpan.FromMilliseconds(retryAttempt * 10);
        }
    }


/// <summary>
/// A simple retry helper to make testing more robust.
/// </summary>
internal static class RetryHelper
{
    /// <summary>
    /// A scaffolding function to retry operations according to parameters.
    /// </summary>
    /// <typeparam name="TResult">The result type of the function to retry.</typeparam>
    /// <param name="maxAttempts">Maximum number of retry attempts.</param>
    /// <param name="retryFunction">The function taking the current retry parameter that provides the time to wait for next attempt.</param>
    /// <param name="operation">The operation to retry.</param>
    /// <param name="cancellation">The cancellation token.</param>
    /// <returns>The result of the retry.</returns>
    internal static async Task<TResult> RetryOnExceptionAsync<TResult>(int maxAttempts,
        Func<int, TimeSpan> retryFunction, Func<Task<TResult>> operation,
        CancellationToken cancellation = default(CancellationToken))
    {
        const int minAttempts = 1;
        if (maxAttempts <= minAttempts)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAttempts),
                $"The count of {maxAttempts} needs to be at least {minAttempts}.");
        }

        if (operation == null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        var attempts = 0;
        while (true)
        {
            try
            {
                cancellation.ThrowIfCancellationRequested();

                attempts++;
                return await operation().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (attempts == maxAttempts)
                {
                    throw;
                }

                Trace.WriteLine(ex);
                await Task.Delay(retryFunction(attempts), cancellation).ConfigureAwait(false);
            }
        }
    }

    public static ParallelOptions CreateDefaultParallelOptions()
    {
        var baseParallelism = Environment.ProcessorCount;
        var desiredParallelism = Convert.ToInt32(Math.Ceiling(baseParallelism / 2.0 * 1.5));
        if (desiredParallelism == baseParallelism)
            desiredParallelism -= 1;
        
        if (desiredParallelism < 1)
            desiredParallelism = 1;

        if (Debugger.IsAttached)
            desiredParallelism = 1;
        
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = desiredParallelism };
        return parallelOptions;
    }
}