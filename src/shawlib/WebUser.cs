using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace ShawLib
{
    public struct PostData
    {
        public string Name;
        public string Value;
    }

    public class WebUser
    {
        CookieContainer ck;
        List<PostData> postDatas;

        public string Url { get; private set; }

        public WebUser()
        {
            ck = new CookieContainer();
            postDatas = new List<PostData>();
        }

        public void Add(string attr, string val)
        {
            var postData = new PostData() { Name = attr, Value = val };
            postDatas.Add(postData);
        }

        public string RequestString(string url, string referer = null)
        {
            var ret = RequestBytes(url, referer);
            return ret != null ? Encoding.UTF8.GetString(ret) : null;
        }

        public byte[] RequestBytes(string url, string referer = null, string postData = null)
        {
            var request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.CookieContainer = ck;
            request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            request.Timeout = 5 * 1000;

            if (postDatas.Count > 0)
            {
                request.Method = WebRequestMethods.Http.Post;

                var first = true;
                var sb = new StringBuilder();
                foreach (var data in postDatas)
                {
                    sb.Append((first ? "" : "&") + WebUtility.UrlEncode(data.Name) + "=" + WebUtility.UrlEncode(data.Value));
                    if (first)
                        first = false;
                }
                var post = Encoding.UTF8.GetBytes(sb.ToString());
                request.ContentLength = post.Length;
                using (var stream = request.GetRequestStream())
                    stream.Write(post, 0, post.Length);
                postDatas.Clear();
            }
            else
                request.Method = WebRequestMethods.Http.Get;

            if (!String.IsNullOrEmpty(referer))
                request.Referer = referer;

            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var memStream = new MemoryStream())
            {
                Url = response.ResponseUri.ToString();
                stream.CopyTo(memStream);
                return memStream.ToArray();
            }
        }
    }
}
