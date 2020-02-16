using System;

namespace SShared
{

    /// <summary>
    /// A singleton used to initialize / terminate the ENet library as needed.
    /// </summary>
    public class BusContext
    {
        // See: https://csharpindepth.com/articles/singleton
        //      https://stackoverflow.com/questions/4364665/static-destructor

        static readonly Lazy<BusContext> instance = new Lazy<BusContext>(() => new BusContext());
        bool inited;

        /// <summary>
        /// Initialize ENet and tell C# to finalize it when the AppDomain dies.
        /// </summary>
        private BusContext()
        {
            inited = ENet.Library.Initialize();
            AppDomain.CurrentDomain.DomainUnload += Deinit;
        }

        private void Deinit(object sender, EventArgs e)
        {
            if (inited)
            {
                ENet.Library.Deinitialize();
                inited = false;
            }
        }

        /// <summary>
        /// Initializes ENet the first time; no-op any further time this is called.
        /// </summary>
        public static BusContext Instance
        {
            get
            {
                return instance.Value;
            }
        }

        /// <summary>
        /// Returns true if ENet is initialized, false otherwise (likely due to an initialization error).
        /// </summary>
        /// <value></value>
        public bool Inited
        {
            get
            {
                return inited;
            }
        }
    }
}