using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleMusic.Net
{
    internal class FormBuilder : IDisposable
    {
        #region Members
        private string boundary = "----------" + DateTime.Now.Ticks.ToString("x");
        private List<Stream> streams = new List<Stream>();

        private long _length = new long();
        #endregion

        #region Properties

        public static FormBuilder Empty
        {
            get
            {
                return new FormBuilder();
            }
        }

        public String ContentType
        {
            get { return "multipart/form-data; boundary=" + boundary; }
        }

        public long Length
        {
            get { return _length; }
            set { _length = value; }
        }

        #endregion

        #region Add

        public void AddField(string key, string value)
        {
            if (streams.Count < 1 || !(streams[streams.Count - 1] is MemoryStream))
                streams.Add(new MemoryStream());

            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("\r\n--{0}\r\n", boundary);
            sb.AppendFormat("Content-Disposition: form-data; name=\"{0}\";\r\n\r\n{1}", key, value);

            byte[] sbData = Encoding.UTF8.GetBytes(sb.ToString());

            streams[streams.Count - 1].Write(sbData, 0, sbData.Length);
            _length += sbData.Length;
        }

        public void AddFields(Dictionary<string, string> fields)
        {
            foreach (KeyValuePair<string, string> key in fields)
                this.AddField(key.Key, key.Value);
        }

        public void AddFile(string name, string fileName, FileStream fileStream)
        {
            if (streams.Count < 1 || !(streams[streams.Count - 1] is MemoryStream))
                streams.Add(new MemoryStream());

            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("\r\n--{0}\r\n", boundary);
            sb.AppendFormat("Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\n", name, fileName);

            sb.AppendFormat("Content-Type: {0}\r\n\r\n", "application/octet-stream");

            byte[] sbData = Encoding.UTF8.GetBytes(sb.ToString());
            streams[streams.Count - 1].Write(sbData, 0, sbData.Length);
            _length += sbData.Length;

            streams.Add(fileStream);
            _length += fileStream.Length;
        }

        /// <summary>
        /// Adds the appropriate ending data to the form. This must be called before writing the form to ensure it is valid.
        /// </summary>
        public void AddEnding()
        {
            if (streams.Count < 1 || !(streams[streams.Count - 1] is MemoryStream))
                streams.Add(new MemoryStream());

            byte[] sbData = Encoding.UTF8.GetBytes("\r\n--" + boundary + "--\r\n");

            streams[streams.Count - 1].Write(sbData, 0, sbData.Length);
            _length += sbData.Length;
        }

        #endregion

        #region WriteTo

        public void WriteTo(Stream destination, int bufferSize)
        {
            foreach (Stream s in streams)
            {
                s.Position = 0;
                if (s is MemoryStream)
                {
                    MemoryStream ms = (MemoryStream)s;
                    ms.WriteTo(destination);
                }
                else
                {
                    s.CopyTo(destination, bufferSize);
                }
            }
        }

        public async Task WriteToAsync(Stream destination, int bufferSize,
            CancellationToken cancellationToken = default(CancellationToken), TaskProgressEventHandler progressHandler = null, TaskCompleteEventHandler completeHandler = null)
        {
            byte[] buffer = null;

            foreach (Stream currentStream in streams)
            {
                currentStream.Position = 0;

                if (cancellationToken.IsCancellationRequested)
                    return;

                // Purely for progress purposes
                double totalRead = new double();

                if (currentStream is MemoryStream)
                {
                    // MemoryStream: Get underlying buffer
                    MemoryStream memoryStream = (MemoryStream)currentStream;

                    // MemoryStream: Check if underlying buffer is valid
                    bool bufferValid = false;
                    try
                    {
                        buffer = memoryStream.GetBuffer();
                        bufferValid = buffer != null;
                    }
                    catch { }

                    if (bufferValid)
                    {
                        // MemoryStream Buffer: Buffer is valid and can be used
                        int readPosition = new int();

                        while (readPosition < memoryStream.Length && !cancellationToken.IsCancellationRequested)
                        {
                            // ProgressHandler: Begin asynchronous invoke
                            IAsyncResult progressHandlerResult = null;
                            if (progressHandler != null)
                                progressHandlerResult = progressHandler.BeginInvoke(Math.Min(1.0, totalRead / (double)_length), null, null);

                            // WriteTask: Start writing to stream asynchronously
                            int chunk = (int) Math.Min(memoryStream.Length - readPosition, bufferSize);
                            Task writeTask = destination.WriteAsync(buffer, readPosition, chunk, cancellationToken);

                            // ProgressHandler: End asynchronous invoke
                            if (progressHandler != null && progressHandlerResult != null)
                                progressHandler.EndInvoke(progressHandlerResult);

                            // WriteTask: Wait for chunk upload to finish
                            await writeTask;
                            totalRead += chunk;
                            readPosition += chunk;
                        }
                        // Go onto next stream
                        continue;
                    }

                    // MemoryStream Buffer: Buffer was not valid - fall through to next case
                }

                if (buffer == null || buffer.Length < bufferSize)
                    buffer = new byte[bufferSize];

                int chunkRead;
                while ((chunkRead = await currentStream.ReadAsync(buffer, 0, bufferSize)) > 0 && !cancellationToken.IsCancellationRequested)
                {
                    // ProgressHandler: Begin asynchronous invoke
                    IAsyncResult progressHandlerResult = null;
                    if (progressHandler != null)
                        progressHandlerResult = progressHandler.BeginInvoke(Math.Min(1.0, (double)totalRead / (double)_length), null, null);

                    // WriteTask: Start writing to stream asynchronously
                    Task writeTask = destination.WriteAsync(buffer, 0, chunkRead, cancellationToken);

                    // ProgressHandler: End asynchronous invoke
                    if (progressHandler != null && progressHandlerResult != null)
                        progressHandler.EndInvoke(progressHandlerResult);

                    // WriteTask: Wait for write to finish
                    await writeTask;
                    totalRead += chunkRead;
                }
            }
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            foreach (Stream s in streams)
                s.Dispose();
        }

        #endregion

    }
}
