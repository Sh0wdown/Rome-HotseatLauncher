using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace HotseatLauncher
{
    class FileWatcher : IDisposable
    {
        static List<FileWatcher> activeWatchers = new List<FileWatcher>();

        #region Threading

        static Thread watchersThread = new Thread(Loop);
        static ManualResetEvent runningEvent = new ManualResetEvent(true);
        static int currentIndex = -1;
        static object _lock = new object();

        static void Loop()
        {
            try
            {
                while (true)
                {
                    runningEvent.WaitOne();

                    FileWatcher current;
                    int count;
                    lock (_lock)
                    {
                        count = activeWatchers.Count;

                        currentIndex++;
                        if (currentIndex >= count)
                            currentIndex = 0;

                        current = activeWatchers[currentIndex];
                    }

                    current.Update();

                    int sleep = 500 / count;
                    Thread.Sleep(sleep < 10 ? 10 : sleep);
                }
            }
            catch (Exception e)
            {
                Debug.ShowException(e);
            }
        }

        static void ActivateWatcher(FileWatcher watcher)
        {
            int count;
            lock (_lock)
            {
                activeWatchers.Add(watcher);
                count = activeWatchers.Count;

                if (count == 1)
                    runningEvent.Set();
            }

            if (watchersThread.ThreadState == ThreadState.Unstarted)
            {
                watchersThread.IsBackground = true;
                watchersThread.Start();
            }
        }

        static void DeactivateWatcher(FileWatcher watcher)
        {
            int count;
            lock (_lock)
            {
                activeWatchers.Remove(watcher);
                count = activeWatchers.Count;

                if (count == 0)
                    runningEvent.Reset();
            }
        }

        #endregion

        /// <summary>
        /// Fires after the file is created or changed and is not locked by another process anymore.
        /// </summary>
        public event Action AfterWrite;

        /// <summary>
        /// Fires after the file is deleted.
        /// </summary>
        public event Action AfterDelete;

        FileInfo fileInfo;
        public FileInfo Info { get { return this.fileInfo; } }

        bool oldExists;
        DateTime oldWriteTime;
        bool hasChanged;

        public FileWatcher(string path)
        {
            fileInfo = new FileInfo(path);
            Reset();
        }

        public void Dispose()
        {
            Enabled = false;
        }

        void Reset()
        {
            fileInfo.Refresh();
            oldExists = fileInfo.Exists;
            oldWriteTime = fileInfo.LastWriteTimeUtc;
            hasChanged = false;
        }

        // fixme
        bool postponedEnabled;
        bool postpone = false;
        public bool Postpone
        {
            get { return postpone; }
            set
            {
                if (postpone == value)
                    return;

                postpone = value;
                
                if (postpone)
                {
                    postponedEnabled = enabled;
                }
                else
                {
                    Enabled = postponedEnabled;
                }
            }
        }

        bool enabled = false;
        public bool Enabled
        {
            get { return enabled; }
            set
            {
                if (enabled == value)
                    return;

                if (postpone)
                {
                    postponedEnabled = value;
                    return;
                }
                
                enabled = value;
                
                if (enabled)
                {
                    Reset();
                    ActivateWatcher(this);
                }
                else
                {
                    DeactivateWatcher(this);
                }
            }
        }

        void Update()
        {
            fileInfo.Refresh();
            
            bool newExists = fileInfo.Exists;
            if (newExists != oldExists)
            {
                if (oldExists) // deleted
                {
                    if (AfterDelete != null)
                        AfterDelete();
                    hasChanged = false;
                }
                else // created
                {
                    oldWriteTime = fileInfo.LastWriteTimeUtc;
                    hasChanged = true;
                }
                oldExists = newExists;
            }

            DateTime newWriteTime = fileInfo.LastWriteTimeUtc;
            if (newWriteTime != oldWriteTime) // was written to
            {
                oldWriteTime = newWriteTime;
                hasChanged = true;
            }

            if (hasChanged)
            {
                if (!fileInfo.IsLocked())
                {
                    if (AfterWrite != null)
                        AfterWrite();
                    hasChanged = false;
                }
            }
        }
    }
}
