using System;

namespace ServiceLink.Exceptions
{
    public class MissedContractException : ServiceLinkException
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public MissedContractException()
        {
        }

        public MissedContractException(string message) : base(message)
        {
        }

        public MissedContractException(string message, Exception inner) : base(message, inner)
        {
        }

     
    }
}