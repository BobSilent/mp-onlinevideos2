using System;
using System.Threading;
using MediaPortal.GUI.Library;
using MediaPortal.Dialogs;

namespace OnlineVideos.MediaPortal1
{
    internal class Gui2UtilConnector
    {
        #region Singleton
        private Gui2UtilConnector()
        {
            _timeoutTimer.Elapsed += TaskWatcherTimerElapsed;
        }
        private static readonly Lazy<Gui2UtilConnector> _lazy =
            new Lazy<Gui2UtilConnector>(() => new Gui2UtilConnector());
        internal static Gui2UtilConnector Instance => _lazy.Value;
        #endregion

        internal bool IsBusy { get; private set; }

        private Action<bool, object> _currentResultHandler = null;
        private object _currentResult = null;
        private bool? _currentTaskSuccess = null;
        private OnlineVideosException _currentError = null;
        private string _currentTaskDescription = null;
        private Thread _backgroundThread = null;
        private CancellationTokenSource _cts = null;
        private bool _abortedByUser = false;
        private readonly System.Timers.Timer _timeoutTimer = new System.Timers.Timer()
        { 
            AutoReset = false 
        };

        internal void StopBackgroundTask()
        {
            StopBackgroundTask(true);
        }

        private void StopBackgroundTask(bool byUserRequest)
        {
            if (IsBusy && _currentTaskSuccess == null && _backgroundThread != null && _backgroundThread.IsAlive)
            {
                Log.Instance.Info("Aborting background thread{0}.", byUserRequest ? " by User Request" : "");
                _cts?.Cancel();
                _abortedByUser = byUserRequest;
                return;
            }
        }

        private void TaskWatcherTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            StopBackgroundTask(false);
        }

        /// <summary>
        /// This method should be used to call methods from siteutils that might take a few seconds.
        /// It makes sure only on thread at a time executes and has a timeout for the execution.
        /// It also catches Exceptions from the utils and writes errors to the log, and show a message on the GUI.
        /// The Wait Cursor will be shown on while executing the task and the resultHandler will be called on the MPMain thread.
        /// </summary>
        /// <param name="task">method to invoke on a background thread</param>
        /// <param name="resultHandler">method to invoke on the GUI Thread with the result of the task</param>
        /// <param name="taskDescription">description of the tak to be invoked - will be shown in the error message if execution fails or times out</param>
        /// <param name="timeout">true: use the timeout, or false: wait forever</param>
        /// <returns>true, if the task could be successfully started in the background</returns>
        internal bool ExecuteInBackgroundAndCallback(Func<object> task, Action<bool, object> resultHandler, string taskDescription, bool timeout)
        {
            if (Thread.CurrentThread.ManagedThreadId != 1)
            {
                Log.Instance.Error("OnlineVideos not called on the MPMain thread - not executing any background action!");
                return false;
            }

            // make sure only one background task can be executed at a time
            if (!IsBusy && Monitor.TryEnter(this))
            {
                try
                {
                    IsBusy = true;
                    _abortedByUser = false;
                    _currentResultHandler = resultHandler;
                    _currentTaskDescription = taskDescription;
                    _currentResult = null;
                    _currentError = null;
                    _currentTaskSuccess = null;// while this is null the task has not finished (or later on timeouted), true indicates successfull completion and false error
                    _cts = new CancellationTokenSource();
                    var token = _cts.Token;
                    GUIWaitCursor.Init(); GUIWaitCursor.Show(); // init and show the wait cursor in MediaPortal
                    _backgroundThread = new Thread(delegate ()
                    {
                        try
                        {
                            _currentResult = task.Invoke();
                            // Only mark success if we were not cancelled during or after the call.
                            if (!token.IsCancellationRequested)
                            {
                                _currentTaskSuccess = true;
                            }
                        }
                        catch (Exception threadException)
                        {
                            _currentError = threadException as OnlineVideosException;
                            Log.Instance.Warn(threadException.Message);
                            _currentTaskSuccess = false;
                        }
                        _timeoutTimer.Stop();
                        // hide the wait cursor
                        GUIWaitCursor.Hide();
                        // execute the ResultHandler on the Main Thread
                        GUIWindowManager.SendThreadCallbackAndWait((p1, p2, o) => { ExecuteTaskResultHandler(); return 0; }, 0, 0, null);
                    })
                    { Name = "OnlineVideos", IsBackground = true };
                    // disable timeout when debugging
                    if (timeout && !System.Diagnostics.Debugger.IsAttached)
                    {
                        _timeoutTimer.Interval = OnlineVideoSettings.Instance.UtilTimeout * 1000;
                        _timeoutTimer.Start();
                    }

                    _backgroundThread.Start();
                    // successfully started the background task
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Instance.Error(ex);
                    IsBusy = false;
                    _currentResultHandler = null;
                    GUIWaitCursor.Hide(); // hide the wait cursor
                    return false; // could not start the background task
                }
            }
            else
            {
                Log.Instance.Error("Another thread tried to execute a task in background.");
                return false;
            }
        }

        private void ExecuteTaskResultHandler()
        {
            if (!IsBusy)
            {
                return;
            }

            // show an error message if task was not completed successfully
            if (_currentTaskSuccess != true)
            {
                if (_currentError != null)
                {
                    MediaPortal.Dialogs.GUIDialogOK dlg_error = (MediaPortal.Dialogs.GUIDialogOK)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK);
                    if (dlg_error != null)
                    {
                        dlg_error.Reset();
                        dlg_error.SetHeading(PluginConfiguration.Instance.BasicHomeScreenName);
                        if (_currentError.ShowCurrentTaskDescription)
                        {
                            dlg_error.SetLine(1, string.Format("{0} {1}", Translation.Instance.Error, _currentTaskDescription));
                        }
                        dlg_error.SetLine(2, _currentError.Message);
                        dlg_error.DoModal(GUIWindowManager.ActiveWindow);
                    }
                }
                else
                {
                    GUIDialogNotify dlg_error = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                    if (dlg_error != null)
                    {
                        dlg_error.Reset();
                        dlg_error.SetImage(SiteImageExistenceCache.GetImageForSite("OnlineVideos", type: "Icon"));
                        dlg_error.SetHeading(PluginConfiguration.Instance.BasicHomeScreenName);
                        if (_currentTaskSuccess.HasValue)
                        {
                            dlg_error.SetText(string.Format("{0} {1}", Translation.Instance.Error, _currentTaskDescription));
                        }
                        else
                        {
                            dlg_error.SetText(string.Format("{0} {1}", Translation.Instance.Timeout, _currentTaskDescription));
                        }

                        if (!_abortedByUser)
                        {
                            dlg_error.DoModal(GUIWindowManager.ActiveWindow);
                        }
                    }
                }
            }

            // store info needed to invoke the result handler
            bool stored_TaskSuccess = _currentTaskSuccess == true;
            var stored_Handler = _currentResultHandler;
            object stored_ResultObject = _currentResult;

            // clear all fields and allow execution of another background task 
            // before actually executing the result handler -> this way a result handler can also inovke another background task)
            _currentResultHandler = null;
            _currentResult = null;
            _currentTaskSuccess = null;
            _currentError = null;
            _backgroundThread = null;
            _cts?.Dispose();
            _cts = null;
            _abortedByUser = false;
            IsBusy = false;
            _timeoutTimer.Stop();
            // execute the result handler
            if (stored_Handler != null)
            {
                try
                {
                    stored_Handler.Invoke(stored_TaskSuccess, stored_ResultObject);
                }
                catch (OnlineVideosException ex)
                {
                    GUIDialogOK dlg_error = (GUIDialogOK)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK);
                    if (dlg_error != null)
                    {
                        dlg_error.Reset();
                        dlg_error.SetHeading(PluginConfiguration.Instance.BasicHomeScreenName);
                        dlg_error.SetLine(2, ex.Message);
                        dlg_error.DoModal(GUIWindowManager.ActiveWindow);
                    }
                }
                finally
                {
                    Monitor.Exit(this);
                }
            }
            else
            {
                Monitor.Exit(this);
            }
        }
    }
}
