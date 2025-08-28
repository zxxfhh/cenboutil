namespace CenboGeneral
{
    public delegate void Handle<T>(T t);

    /// <summary>
    /// 线程管理泛型
    /// </summary>
    public class ThreadWithState<T>
    {
        private T t;

        private Handle<T> callback;

        public ThreadWithState(T _t, Handle<T> callbackDelegate)
        {
            t = _t;
            callback = callbackDelegate;
        }

        public void ThreadProc()
        {
            callback?.Invoke(t);
        }
    }
}
