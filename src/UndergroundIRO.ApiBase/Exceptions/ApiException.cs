using System;

namespace ApiBase.Exceptions
{
    public class ApiException : Exception
    {
        /// <summary>
        /// Gets or sets the error code (HTTP status code)
        /// </summary>
        /// <value>The error code (HTTP status code).</value>
        public int? HttpCode { get; }

        public string ResponseContent { get; }

        public ApiException() { }

        public ApiException(string message) : base(message) { }

        public ApiException(int httpCode, string message, string responseContent = null) : base(message)
        {
            this.HttpCode = httpCode;
            this.ResponseContent = responseContent;
        }

        public override string ToString()
        {
            var res = base.ToString();
            if (HttpCode != null)
            {
                res += $"\n------------\nWith http code: {HttpCode}.";
            }
            if (ResponseContent != null)
            {
                res += "\n------------\nWith responseContent content:\n";
                res += ResponseContent;
            }
            return res;
        }
    }


}