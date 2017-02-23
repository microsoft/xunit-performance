using Microsoft.Xunit.Performance.Api.Native.Windows;
using System;

namespace Microsoft.Xunit.Performance.Api
{
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
                    case Kernel32.CtrlTypes.CTRL_LOGOFF_EVENT:
                    case Kernel32.CtrlTypes.CTRL_SHUTDOWN_EVENT:
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
                    // TODO: dispose managed state (managed objects).
                    BaseDisposableObject?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                if (!Kernel32.SetConsoleCtrlHandler(_HandlerRoutine, false))
                    throw new ObjectDisposedException("SetConsoleCtrlHandler failed to remove handler.");

                _disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
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
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
