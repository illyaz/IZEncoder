namespace IZEncoder.Launcher.Common.Client
{
    using System;
    using System.Net;
    using System.Runtime.Serialization;
    using RestSharp;

    public class LauncherClientException : Exception
    {
        public LauncherClientException() { }

        public LauncherClientException(string message) : base(message) { }

        public LauncherClientException(string message, Exception innerException) : base(message, innerException) { }

        protected LauncherClientException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public HttpStatusCode StatusCode { get; set; }
        public string StatusDescription { get; set; }
        public IRestResponse RestResponse { get; set; }
    }
}