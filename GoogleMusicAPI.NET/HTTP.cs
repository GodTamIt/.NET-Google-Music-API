using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Text;

namespace GoogleMusicAPI
{
    public class HTTP
    {
        public delegate void RequestCompletedEventHandler(HttpWebRequest request, HttpWebResponse response, String jsonData, Exception error);
        public delegate void RequestProgressEventHandler(HttpWebRequest request, int percentage);

        static HTTP()
        {

        }

        private class RequestState
        {
            public HttpWebRequest Request;
            public byte[] UploadData;
            public int MillisecondsTimeout;
            public RequestCompletedEventHandler CompletedCallback;
            public RequestProgressEventHandler ProgressCallback;

            public RequestState(HttpWebRequest request, byte[] uploadData, int millisecondsTimeout, RequestCompletedEventHandler completedCallback, RequestProgressEventHandler progressCallback)
            {
                Request = request;
                UploadData = uploadData;
                MillisecondsTimeout = millisecondsTimeout;
                CompletedCallback = completedCallback;
                ProgressCallback = progressCallback;
            }
        }

        public HttpWebRequest UploadDataAsync(Uri address, FormBuilder builder, RequestCompletedEventHandler complete, int timeout = 10000)
        {
            return UploadDataAsync(address, builder.ContentType, builder.GetBytes(), timeout, complete, null);
        }

        public HttpWebRequest UploadDataAsync(Uri address, string contentType, byte[] data, int millisecondsTimeout, RequestCompletedEventHandler completedCallback, RequestProgressEventHandler progressCallback, string userAgent = null)
        {
            // Create the request
            HttpWebRequest request = SetupRequest(address, userAgent);
            
#if !NETFX_CORE
            request.ContentLength = (data != null) ? data.Length : 0;
#endif

            if (!String.IsNullOrEmpty(contentType))
                request.ContentType = contentType;

            request.Method = "POST";
            RequestState state = new RequestState(request, data, millisecondsTimeout, completedCallback, progressCallback);
            IAsyncResult result = request.BeginGetRequestStream(OpenWrite, state);

#if !NETFX_CORE
            ThreadPool.RegisterWaitForSingleObject(result.AsyncWaitHandle, TimeoutCallback, state, millisecondsTimeout, true);
#endif

            return request;
        }


        public byte[] UploadDataSync(Uri address, string contentType, byte[] data, string userAgent = null, string method = "POST")
        {
            // Create the request
            HttpWebRequest request = SetupRequest(address, userAgent);

#if !NETFX_CORE
            request.ContentLength = (data != null) ? data.Length : 0;
#endif

            if (!String.IsNullOrEmpty(contentType))
                request.ContentType = contentType;

            request.Method = method;

            // Get the stream to write our upload to
            using (Stream uploadStream = request.GetRequestStream())
            {
                byte[] buffer = new Byte[checked((uint)Math.Min(1024, (int)data.Length))];

                MemoryStream ms = new MemoryStream(data);

                int bytesRead;
                int i = 0;
                while ((bytesRead = ms.Read(buffer, 0, buffer.Length)) != 0)
                {
                    int prog = (int)Math.Floor(Math.Min(100.0,
                            (((double)(bytesRead * i) / (double)ms.Length) * 100.0)));


                    uploadStream.Write(buffer, 0, bytesRead);

                    i++;
                }

#if !NETFX_CORE
                ms.Close();
                uploadStream.Close();
#endif
            }

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            MemoryStream ms2 = new MemoryStream();
            byte[] result;

            using (Stream responseStream = response.GetResponseStream())
            {
                byte[] buffer = new byte[4096];
                int count = responseStream.Read(buffer, 0, 4096);
                while (count > 0)
                {
                    ms2.Write(buffer, 0, count);

                    count = responseStream.Read(buffer, 0, 4096);
                }

#if !NETFX_CORE
                responseStream.Close();
#endif
                ms2.Position = 0;
                result = new byte[ms2.Length];
                ms2.Read(result, 0, (int)ms2.Length);
                ms2.Close();
            }

            return result;
        }

        public void UploadDataSync(Uri address, string contentType, byte[] data, out HttpWebRequest request, out HttpWebResponse response)
        {
            // Create the request
            request = SetupRequest(address);

#if !NETFX_CORE
            request.ContentLength = (data != null) ? data.Length : 0;
#endif

            if (!String.IsNullOrEmpty(contentType))
                request.ContentType = contentType;

            request.Method = "POST";

            // Get the stream to write our upload to
            using (Stream uploadStream = request.GetRequestStream())
            {
                byte[] buffer = new Byte[checked((uint)Math.Min(1024, (int)data.Length))];

                MemoryStream ms = new MemoryStream(data);

                int bytesRead;
                int i = 0;
                while ((bytesRead = ms.Read(buffer, 0, buffer.Length)) != 0)
                {
                    int prog = (int)Math.Floor(Math.Min(100.0,
                            (((double)(bytesRead * i) / (double)ms.Length) * 100.0)));


                    uploadStream.Write(buffer, 0, bytesRead);

                    i++;
                }

#if !NETFX_CORE
                ms.Close();
                uploadStream.Close();
#endif
            }

            response = (HttpWebResponse)request.GetResponse();
        }

        public HttpWebRequest UploadWriteStreamSync(Uri address, string contentType, string userAgent = null, string method = "POST")
        {
            // Create the request
            HttpWebRequest request = SetupRequest(address, userAgent);

            if (!String.IsNullOrEmpty(contentType))
                request.ContentType = contentType;

            request.Method = method;

            return request;
        }

        public Stream UploadReadStreamSync(HttpWebRequest request, Stream stream)
        {
#if !NETFX_CORE
            request.ContentLength = (stream != null) ? stream.Length : 0;
#endif
            Stream requestStream = request.GetRequestStream();

            byte[] buffer = new byte[stream.Length];
            stream.Position = 0;
            int read = stream.Read(buffer, 0, (int)stream.Length);
            requestStream.Write(buffer, 0, (int)stream.Length);
            requestStream.Close();
            stream.Close();

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            return response.GetResponseStream();
        }

        public string DownloadStringSync(Uri address, String uploaderId = null, String userAgent = null)
        {
            HttpWebRequest request = SetupRequest(address);
            request.Method = "GET";

            if (uploaderId != null)
                request.Headers["X-Device-ID"] = uploaderId;

            if (userAgent != null)
                request.UserAgent = userAgent;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            Stream responseStream = response.GetResponseStream();

            MemoryStream ms = new MemoryStream();
            byte[] buffer = new byte[4096];
            int count = responseStream.Read(buffer, 0, 4096);
            while (count > 0)
            {
                ms.Write(buffer, 0, count);

                count = responseStream.Read(buffer, 0, 4096);
            }

#if !NETFX_CORE
            responseStream.Close();
#endif
            ms.Position = 0;
            byte[] result = new byte[ms.Length];
            ms.Read(result, 0, (int)ms.Length);
            ms.Close();

            return Encoding.UTF8.GetString(result);
        }

        public HttpWebRequest DownloadStringAsync(Uri address, RequestCompletedEventHandler completedCallback, int millisecondsTimeout = 10000)
        {
            HttpWebRequest request = SetupRequest(address);
            request.Method = "GET";
            DownloadDataAsync(request, null, millisecondsTimeout, completedCallback);
            return request;
        }

        public void DownloadDataAsync(HttpWebRequest request, byte[] d,  int millisecondsTimeout,
           RequestCompletedEventHandler completedCallback)
        {
            RequestState state = new RequestState(request, d, millisecondsTimeout, completedCallback, null);

            IAsyncResult result = request.BeginGetResponse(GetResponse, state);

#if !NETFX_CORE
            ThreadPool.RegisterWaitForSingleObject(result.AsyncWaitHandle, TimeoutCallback, state, millisecondsTimeout, true);
#endif
        }


        public virtual HttpWebRequest SetupRequest(Uri address, String userAgent = null)
        {
            if (address == null)
                throw new ArgumentNullException("'address' cannot be null.");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);
            request.AllowAutoRedirect = false;

#if !NETFX_CORE
            request.ServicePoint.MaxIdleTime = 1000 * 60;
            request.ServicePoint.Expect100Continue = false;
            request.ServicePoint.ConnectionLimit = 20;
            request.ServicePoint.UseNagleAlgorithm = false;
#endif

            return request;
        }

        void OpenWrite(IAsyncResult ar)
        {
            RequestState state = (RequestState)ar.AsyncState;

            try
            {
                // Get the stream to write our upload to
                using (Stream uploadStream = state.Request.EndGetRequestStream(ar))
                {
                    byte[] buffer = new Byte[checked((uint)Math.Min(1024, (int)state.UploadData.Length))];

                    MemoryStream ms = new MemoryStream(state.UploadData);

                    int bytesRead;
                    int i = 0;
                    while ((bytesRead = ms.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        int prog = (int)Math.Floor(Math.Min(100.0,
                                (((double)(bytesRead * i) / (double)ms.Length) * 100.0)));


                        uploadStream.Write(buffer, 0, bytesRead);

                        i++;

                        if (state.ProgressCallback != null)
                            state.ProgressCallback(state.Request, prog);
                    }

                    if (state.ProgressCallback != null)
                        state.ProgressCallback(state.Request, 100);

#if !NETFX_CORE
                    ms.Close();
                    uploadStream.Close();
#endif
                }

                IAsyncResult result = state.Request.BeginGetResponse(GetResponse, state);

#if !NETFX_CORE
                ThreadPool.RegisterWaitForSingleObject(result.AsyncWaitHandle, TimeoutCallback, state,
                    state.MillisecondsTimeout, true);
#endif

            }
            catch (Exception ex)
            {
                if (state.CompletedCallback != null)
                    state.CompletedCallback(state.Request, null, null, ex);
            }
        }

        void GetResponse(IAsyncResult ar)
        {
            RequestState state = (RequestState)ar.AsyncState;
            HttpWebResponse response = null;
            Exception error = null;
            String result = "";

            try
            {
                response = (HttpWebResponse)state.Request.EndGetResponse(ar);
                using (Stream responseStream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(responseStream);

                    result = reader.ReadToEnd();

#if !NETFX_CORE
                    reader.Close();
                    responseStream.Close();
#endif

                }
            }
            catch (Exception ex)
            {
                error = ex;
            }

            if (state.CompletedCallback != null)
                state.CompletedCallback(state.Request, response, result, error);
        }

        void TimeoutCallback(object state, bool timedOut)
        {
            if (timedOut)
            {
                RequestState requestState = state as RequestState;
                if (requestState != null && requestState.Request != null)
                {
                    requestState.Request.Abort();
                }
            }
        }
    }
}
