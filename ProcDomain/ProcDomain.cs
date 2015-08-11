using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ProcDomain
{
    public class ProcDomain
    {
        private static Lazy<string> g_hProcStr = new Lazy<string>(GetProcessHandleString);
        private static ProcDomain g_currentDomain;
        private static readonly object g_currentDomainLock = new object();

        private string _domName = "DefaultProcDomain";
        private string _pipeName;
        private PipeStream _pipe;
        private Dictionary<Guid, TaskCompletionSource<CrossDomainInvokeResponse>> _pendingActions = new Dictionary<Guid, TaskCompletionSource<CrossDomainInvokeResponse>>();
        private Process _process;

        public event Action<ProcDomain> Unloading;
        public event Action<ProcDomain> Unloaded;

        public string Name { get { return this._domName; } }

        public static ProcDomain CreateDomain(string name)
        {
            ProcDomain domain = new ProcDomain();

            // TODO: does the pipe really need/want a name?
            domain._pipeName = g_hProcStr.Value + "_" + name;

            //create a named pipe
            //the child process will connect as a client but both sides act as a server and a client sending and waiting for messages
            var pipeServer = new NamedPipeServerStream(domain._pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous) { ReadMode = PipeTransmissionMode.Message };

            domain._pipe = pipeServer;

            //start waiting for a connection before creating the process
            Task clientConnected = pipeServer.WaitForConnectionAsync(); // new CancellationTokenSource(2000).Token);

            //create the process
            domain.CreateDomainProcess();

            //this will unwind any exception comming from WaitForConnection
            clientConnected.GetAwaiter().GetResult();

            //start listening for communications from the client
            Task throwaway = domain.ListenToChildDomainAsync();

            return domain;
        }

        public static ProcDomain GetCurrentProcDomain()
        {
            if (g_currentDomain == null)
            {
                InitializeCurrentDomain();
            }

            return g_currentDomain;
        }

        internal static void InitializeCurrentDomain(string pipeName = null)
        {
            lock (g_currentDomainLock)
            {
                if (g_currentDomain == null)
                {
                    g_currentDomain = new ProcDomain();

                    g_currentDomain._pipeName = pipeName;

                    //if the pipe name is specified this is a child domain
                    if (g_currentDomain._pipeName != null)
                    {
                        g_currentDomain.InitializeChildDomain();
                    }
                }
            }
        }

        private void Unload()
        {
            if (this.Unloading != null)
            {
                this.Unloading(this);
            }

            if (this.Unloaded != null)
            {
                this.Unloaded(this);
            }
        }

        private void InitializeChildDomain()
        {
            //get the first _ in the pipename as this splits the parent proc handle and the domain name
            int splitIdx = this._pipeName.IndexOf('_');

            string hProcStr = this._pipeName.Substring(0, splitIdx);

            this._domName = this._pipeName.Substring(splitIdx + 1);

            //get the handle for the parent process from the pipename
            IntPtr hParentProc = (IntPtr)ulong.Parse(hProcStr);

            Task throwaway = this.UnloadOnProcessExitAsync(hParentProc);

            //create a client for the named pipe of the parent
            //the child process will connect as a client but both sides act as a server and a client sending and waiting for messages
            var pipeClient = new NamedPipeClientStream(".", this._pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

            this._pipe = pipeClient;

            pipeClient.Connect();

            throwaway = ListenToParentDomainAsync();
        }

        private async Task UnloadOnProcessExitAsync(IntPtr hProc)
        {
            await Task.Run(() => Imports.WaitForSingleObject(hProc, 0xFFFFFFFF));

            this.Unload();
        }

        private async Task ListenToParentDomainAsync()
        {
            CrossDomainInvokeRequest parentMessage = null;

            //while the we continue to get messages from the parent process
            while ((parentMessage = await ReadNextRequestAsync()) != null)
            {
                Task throwaway = HandleInvokeRequest(parentMessage);
            }
        }

        private async Task HandleInvokeRequest(CrossDomainInvokeRequest request)
        {
            //TODO: assembly loading goo?

            var response = new CrossDomainInvokeResponse() { MessageId = request.MessageId };

            try
            {
                response.Result = await Task.Run<object>(() => request.Method.Invoke(null, request.Arguments));
            }
            catch (TargetInvocationException e)
            {
                response.Exception = e.InnerException;
            }
            catch (Exception e)
            {
                response.Exception = e;
            }

            await SendResponseAsync(response);
        }
        public async Task ExecuteAsync(Action method)
        {
            var methodInfo = method.GetMethodInfo();

            if (methodInfo.IsAbstract || methodInfo.IsPrivate || !methodInfo.IsStatic || methodInfo.DeclaringType.IsNotPublic)
            {
                throw new ArgumentException();
            }

            var request = new CrossDomainInvokeRequest() { Method = methodInfo, MessageId = Guid.NewGuid() };

            var response = await SendRequestAndWaitAsync(request);

            if (response.Exception != null)
            {
                throw response.Exception;
            }
        }

        private async Task SendResponseAsync(CrossDomainInvokeResponse message)
        {
            byte[] buff = message.ToByteArray();

            await this._pipe.WriteAsync(buff, 0, buff.Length);
        }

        private async Task<CrossDomainInvokeResponse> SendRequestAndWaitAsync(CrossDomainInvokeRequest message)
        {
            var taskCompletionSource = new TaskCompletionSource<CrossDomainInvokeResponse>();

            //add the transation to the pendingActions before sending the message to avoid a race with completion of the action
            this._pendingActions.Add(message.MessageId, taskCompletionSource);

            byte[] buff = message.ToByteArray();

            await this._pipe.WriteAsync(buff, 0, buff.Length);

            //wait for the response the completion source will be triggered in the ListenForResponsesAsync loop once the 
            //appropriate message has been recieved, (matching messageId)
            return await taskCompletionSource.Task;
        }

        private async Task ListenToChildDomainAsync()
        {
            CrossDomainInvokeResponse childMessage = null;

            //while the child process is aliave and we continue to get messages
            while (!this._process.HasExited && (childMessage = await ReadNextResponseAsync()) != null)
            {
                TaskCompletionSource<CrossDomainInvokeResponse> completionSource;

                //find the task completion which signals the completion of the invoke request
                if (this._pendingActions.TryGetValue(childMessage.MessageId, out completionSource))
                {
                    //try to mark the task as complete returning the response
                    completionSource.TrySetResult(childMessage);
                }
            }
        }

        private async Task<CrossDomainInvokeResponse> ReadNextResponseAsync()
        {
            byte[] buff = new byte[1024];

            MemoryStream memStream = new MemoryStream();

            do
            {
                int bytesRead = await this._pipe.ReadAsync(buff, 0, 1024);

                memStream.Write(buff, 0, bytesRead);

            } while (!this._pipe.IsMessageComplete);

            return CrossDomainInvokeResponse.FromByteArray(memStream.ToArray());
        }

        private async Task<CrossDomainInvokeRequest> ReadNextRequestAsync()
        {
            this._pipe.ReadMode = PipeTransmissionMode.Message;
            byte[] buff = new byte[1024];

            MemoryStream memStream = new MemoryStream();

            do
            {
                int bytesRead = await this._pipe.ReadAsync(buff, 0, 1024);

                memStream.Write(buff, 0, bytesRead);

            } while (!this._pipe.IsMessageComplete);

            return CrossDomainInvokeRequest.FromByteArray(memStream.ToArray());
        }

        private static string GetProcessHandleString()
        {
            IntPtr hProc;

            IntPtr hCurrProc = Imports.GetCurrentProcess();

            Imports.DuplicateHandle(hCurrProc, hCurrProc, hCurrProc, out hProc, 0, true, (uint)Imports.DuplicateOptions.DUPLICATE_SAME_ACCESS);

            //get the handle for the current process
            return hProc.ToString();
        }

        private Process CreateDomainProcess()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();

            startInfo.UseShellExecute = false;
            startInfo.FileName = Path.Combine(Directory.GetCurrentDirectory(), "oops.exe");
            startInfo.Arguments = this._pipeName;

            return this._process = Process.Start(startInfo);
        }

    }
}
