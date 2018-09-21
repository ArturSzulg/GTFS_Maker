using System;
using System.Runtime.Serialization;

namespace Parser_GTFS
{
    [Serializable]
    internal class GhostCellsException : Exception
    {
        public GhostCellsException()
        {
        }

        public GhostCellsException(string message) : base(message)
        {
        }

        public GhostCellsException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected GhostCellsException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}