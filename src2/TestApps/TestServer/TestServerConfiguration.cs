using System.Runtime.Serialization;

namespace TestServer
{
    public class TestServerConfiguration
    {
        #region Constructors

        /// <summary>
        /// The default constructor.
        /// </summary>
        public TestServerConfiguration()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes the object during deserialization.
        /// </summary>
        [OnDeserializing()]
        private void Initialize(StreamingContext context)
        {
            Initialize();
        }

        /// <summary>
        /// Sets private members to default values.
        /// </summary>
        private void Initialize()
        {
        }

        #endregion
    }
}