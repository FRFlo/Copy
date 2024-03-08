namespace Copy.Types
{
    /// <summary>
    /// Base class for a thread safe singleton.
    /// </summary>
    /// <typeparam name="T">Singleton type</typeparam>
    public abstract class Singleton<T> where T : new()
    {
        /// <summary>
        /// Padlock object for thread safety.
        /// </summary>
        private static readonly object _padlock = new();
        /// <summary>
        /// Nullable instance of the <typeparamref name="T"/> class.
        /// </summary>
        private static T? _instance;

        /// <summary>
        /// Instance of <typeparamref name="T"/> class.
        /// </summary>
        public static T Instance
        {
            get
            {
                lock (_padlock)
                {
                    return _instance ??= new T();
                }
            }
            set => _instance = value;
        }
    }
}
