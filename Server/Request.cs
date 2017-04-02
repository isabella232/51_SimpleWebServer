using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public sealed class Request
    {
        public string RawRequestString { get; private set; }
        public string RawRequestMethod { get; private set; }
        public string[] RequestPart { get; private set; }
        public string Method
        {
            get
            {
                try
                {
                    return RequestPart[0];
                }
                catch
                {
                    return null;
                }
            }
        }
        public string Path
        {
            get
            {
                try
                {
                    return RequestPart[1];
                }
                catch
                {
                    return null;
                }
            }
        }

        private string[] splitRawRequest;
        private string[] values;

        public Request(string rawRequestString)
        {
            RawRequestString = rawRequestString;

            try
            {
                splitRawRequest = rawRequestString.Split('\n');
                if (splitRawRequest.Length != 0)
                {
                    RawRequestMethod = splitRawRequest[0];
                    RequestPart = RawRequestMethod.Split(' ');
                    var valuePart = splitRawRequest[splitRawRequest.Length - 1];

                    if (!valuePart.StartsWith("\0"))
                    {
                        values = valuePart.Split('&');
                    }
                }
            }
            catch (Exception ex)
            {
                string s = ex.Message;
                s += "";

            }
        }

        public string[] GetValue(string key)
        {
            if (values == null) { return null; }
            string result = string.Empty;
            try
            {
                //key += "=";
                //foreach (var v in values)
                //{
                //    if (v.StartsWith(key))
                //    {
                //        var value = v.Substring(key.Length);
                //        result = System.Net.WebUtility.UrlDecode(value);
                //        break;
                //    }
                //}

                result = values[0].Replace("\"", "");

                if (result != null)
                {
                    // REVIEW: weird... tons of /0 in the string at the end...
                    result.Replace("\0", "");
                }
            }
            catch (Exception ex)
            {
                string s = ex.Message;
                s += "";
            }
            return result.Split('|');
        }

        public string GetValue()
        {
            if (values == null) { return null; }
            string result = string.Empty;
            try
            {
                result = values[0].Replace("\"", "");

                if (result != null)
                {
                    // REVIEW: weird... tons of /0 in the string at the end...
                    result.Replace("\0", "");
                }
            }
            catch (Exception ex)
            {
                string s = ex.Message;
                s += "";
            }
            return result;
        }
    }
}
