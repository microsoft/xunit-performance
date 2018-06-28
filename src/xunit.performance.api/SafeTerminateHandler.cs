using Microsoft.Xunit.Performance.Api.Native.Windows;
using System;

namespace Microsoft.Xunit.Performance.Api
{
    /// <summary>
    /// Provides a mechanism for releasing disposable resources when CTRL+C,
    /// CTRL+BREAK, or Close event (when the user closes the console by either
    /// clicking Close on the console windows's window menu, or by clicking the
    /// "End Task" button on the Task Manager) signals are received by the
    /// application.
    /// </summary>
    /// <remarks>
    /// 1. This class it is currently adding support for Windows only.
    /// 2. This class owns the Disposable object that is being wrapped, and it is responsible for its cleanup.
    /// </remarks>
    /// <typeparam name="T">The type that this class encapsulates.</typeparam>
    sealed class SafeTerminateHandler<T> : IDisposable
        where T : class, IDisposable
    {
        readonly Kernel32.PHANDLER_ROUTINE _HandlerRoutine;

        readonly object _lock;

        /// <summary>
        /// Initializes a new instance of the <see cref="SafeTerminateHandler{T}"/> class that
        /// wraps a disposable object that will be created by calling the
        /// specified callback Func.
        /// </summary>
        /// <param name="createCallback">A method that has no parameters and returns a new disposable object of the type specified by the <typeparamref name="T"/> parameter.</param>
        public SafeTerminateHandler(Func<T> createCallback)
        {
            _disposedValue = true;
            _lock = new object();
            BaseDisposableObject = null;
            _HandlerRoutine = (Kernel32.CtrlTypes dwCtrlType) =>
            {
                switch (dwCtrlType)
                {
                    case Kernel32.CtrlTypes.CTRL_C_EVENT:
                    case Kernel32.CtrlTypes.CTRL_BREAK_EVENT:
                    case Kernel32.CtrlTypes.CTRL_CLOSE_EVENT:
                        Dispose();
                        break;

                    default:
                        break;
                }
                return false;
            };

            lock (_lock)
            {
                _disposedValue = false;
                if (!Kernel32.SetConsoleCtrlHandler(_HandlerRoutine, true))
                    throw new Exception("SetConsoleCtrlHandler failed to add handler.");
                BaseDisposableObject = createCallback();
            }
        }

        /// <summary>
        /// Gets the base disposable object wrap by this handler.
        /// </summary>
        public T BaseDisposableObject { get; }

        #region IDisposable Support

        volatile bool _disposedValue; // To detect redundant calls

        ~SafeTerminateHandler()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                lock (_lock)
                {
                    if (!_disposedValue)
                    {
                        if (disposing)
                        {
                            BaseDisposableObject?.Dispose();
                        }

                        if (!Kernel32.SetConsoleCtrlHandler(_HandlerRoutine, false))
                        {
                            throw new ObjectDisposedException("SetConsoleCtrlHandler failed to remove handler.");
                        }

                        _disposedValue = true;
                    }
                }
            }
        }

        #endregion IDisposable Support
    }
}