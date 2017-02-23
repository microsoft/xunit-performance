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
    /// <remarks>This class it is currently adding support for Windows only.</remarks>
    /// <typeparam name="T"></typeparam>
    internal sealed class SafeTerminateHandler<T> : IDisposable
        where T : class, IDisposable
    {
        public SafeTerminateHandler(Func<T> createCallback)
        {
            BaseDisposableObject = null;

            _HandlerRoutine = (Kernel32.CtrlTypes dwCtrlType) =>
            {
                switch (dwCtrlType)
                {
                    case Kernel32.CtrlTypes.CTRL_C_EVENT:
                    case Kernel32.CtrlTypes.CTRL_BREAK_EVENT:
                    case Kernel32.CtrlTypes.CTRL_CLOSE_EVENT:
                        BaseDisposableObject?.Dispose();
                        break;
                    default:
                        break;
                }
                return false;
            };

            if (!Kernel32.SetConsoleCtrlHandler(_HandlerRoutine, true))
                throw new Exception("SetConsoleCtrlHandler failed to add handler.");

            BaseDisposableObject = createCallback();
        }

        public T BaseDisposableObject { get; }

        private readonly Kernel32.PHANDLER_ROUTINE _HandlerRoutine;

        #region IDisposable Support
        private bool _disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
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
        #endregion
    }
}
