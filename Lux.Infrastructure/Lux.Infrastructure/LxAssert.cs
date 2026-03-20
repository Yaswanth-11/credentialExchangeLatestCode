using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lux.Infrastructure
{
    class LxAssert
    {
        public static void NotNullOrEmpty(string argumentValue,
            string argumentName)
        {
            if (String.IsNullOrEmpty(argumentValue))
            {
                throw new LxException(argumentName +
                    " must not be null or empty",
                    LxErrorCodes.E_INVALID_ARGUMENT);
            }
        }

        public static void NotNull(object argumentValue, string argumentName)
        {
            if (argumentValue == null)
            {
                throw new LxException(argumentName + " must not be null",
                    LxErrorCodes.E_INVALID_ARGUMENT);
            }
        }
        public static void NotNegativeOrZero(int argumentValue,
            string argumentName)
        {
            if (argumentValue <= 0)
            {
                throw new LxException(argumentName +
                    " value must be greater than zero",
                    LxErrorCodes.E_INVALID_ARGUMENT);
            }
        }
        public static void Success(int code)
        {
            if (code != 0)
            {
                throw new LxException(code);
            }
        }
    }
}
