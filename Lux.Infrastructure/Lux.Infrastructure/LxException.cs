using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lux.Infrastructure
{
    public class LxException : Exception
    {
        private int _code = LxErrorCodes.E_UNSPECIFIED_ERROR;
        public int Code
        {
            get
            {
                return _code;
            }
        }

        /// <summary>
        /// Default Constructor initialize LxException
        /// </summary>
        public LxException() : base()
        {
        }

        /// <summary>
        /// Constructor to initialize LxException with message
        /// </summary>
        /// <param name="message">
        /// LxException class is initialized with the given message
        /// </param>
        public LxException(string message) : base(message)
        {
        }

        /// <summary>
        /// Constructor to initialize LxException with code
        /// </summary>
        /// <param name="code">Error code</param>
        public LxException(int code) : base(LxErrorCodes.GetErrorMessage(code))
        {
            this._code = code;
        }

        /// <summary>
        /// Constructor to initialize LxException with actual exception
        /// </summary>
        /// <param name="ex">
        /// LxException class is initialized with the given ex
        /// </param>
        public LxException(Exception ex) : base(ex.Message, ex)
        {
        }

        /// <summary>
        /// Constructor to initialize LxException with message and code
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="code">Error code</param>
        public LxException(string message, int code) : this(message)
        {
            this._code = code;
        }
    }
}
