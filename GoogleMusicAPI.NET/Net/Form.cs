using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace GoogleMusic.Net
{
    internal class Form
    {
        #region Members
        private byte[] _Bytes;
        private string _String;
        private string _ContentType;

        #endregion

        #region Constructor
        public Form(byte[] bytes, string contentType)
        {
            _Bytes = bytes;
            _String = Encoding.UTF8.GetString(_Bytes);
            _ContentType = contentType;
        }
        #endregion

        #region Properties

        public byte[] Bytes
        {
            get { return _Bytes; }
        }

        public string String
        {
            get { return _String; }
        }

        public string ContentType
        {
            get { return _ContentType; }
        }

        #endregion

    }

    internal class FormBuilder : IDisposable
    {
        private string boundary = "----------" + DateTime.Now.Ticks.ToString("x");
        private MemoryStream ms;
        private bool _Ended = false;

        public static Form Empty
        {
            get
            {
                using (FormBuilder b = new FormBuilder())
                {
                    return b.ToForm();
                }
            }
        }

        public String ContentType
        {
            get { return "multipart/form-data; boundary=" + boundary; }
        }

        public FormBuilder()
        {
            ms = new MemoryStream();
        }

        public void AddField(string key, string value)
        {
            if (_Ended)
                throw new InvalidOperationException("Cannot add to FormBuilder after it has been ended.");

            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("\r\n--{0}\r\n", boundary);
            sb.AppendFormat("Content-Disposition: form-data; name=\"{0}\";\r\n\r\n{1}", key, value);

            byte[] sbData = Encoding.UTF8.GetBytes(sb.ToString());

            ms.Write(sbData, 0, sbData.Length);
        }

        public async Task AddFieldAsync(string key, string value)
        {
            if (_Ended)
                throw new InvalidOperationException("Cannot add to FormBuilder after it has been ended.");

            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("\r\n--{0}\r\n", boundary);
            sb.AppendFormat("Content-Disposition: form-data; name=\"{0}\";\r\n\r\n{1}", key, value);

            byte[] sbData = Encoding.UTF8.GetBytes(sb.ToString());

            await ms.WriteAsync(sbData, 0, sbData.Length);
        }

        public void AddFields(Dictionary<string, string> fields)
        {
            foreach (KeyValuePair<string, string> key in fields)
                this.AddField(key.Key, key.Value);
        }

        public async Task AddFieldsAsync(Dictionary<string, string> fields)
        {
            foreach (KeyValuePair<string, string> key in fields)
                await this.AddFieldAsync(key.Key, key.Value);
        }

        public void AddFile(string name, string fileName, byte[] file)
        {
            if (_Ended)
                throw new InvalidOperationException("Cannot add to FormBuilder after it has been ended.");

            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("\r\n--{0}\r\n", boundary);
            sb.AppendFormat("Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\n", name, fileName);

            sb.AppendFormat("Content-Type: {0}\r\n\r\n", "application/octet-stream");

            byte[] sbData = Encoding.UTF8.GetBytes(sb.ToString());
            ms.Write(sbData, 0, sbData.Length);

            ms.Write(file, 0, file.Length);
        }

        public async Task AddFileAsync(string name, string fileName, byte[] file)
        {
            if (_Ended)
                throw new InvalidOperationException("Cannot add to FormBuilder after it has been ended.");

            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("\r\n--{0}\r\n", boundary);
            sb.AppendFormat("Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\n", name, fileName);

            sb.AppendFormat("Content-Type: {0}\r\n\r\n", "application/octet-stream");

            byte[] sbData = Encoding.UTF8.GetBytes(sb.ToString());
            await ms.WriteAsync(sbData, 0, sbData.Length);

            await ms.WriteAsync(file, 0, file.Length);
        }

        public void AddFile(string name, string fileName, FileStream file)
        {
            if (_Ended)
                throw new InvalidOperationException("Cannot add to FormBuilder after it has been ended.");

            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("\r\n--{0}\r\n", boundary);
            sb.AppendFormat("Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\n", name, fileName);

            sb.AppendFormat("Content-Type: {0}\r\n\r\n", "application/octet-stream");

            byte[] sbData = Encoding.UTF8.GetBytes(sb.ToString());
            ms.Write(sbData, 0, sbData.Length);

            file.CopyTo(ms);
        }

        public async void AddFileAsync(string name, string fileName, FileStream file)
        {
            if (_Ended)
                throw new InvalidOperationException("Cannot add to FormBuilder after it has been ended.");

            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("\r\n--{0}\r\n", boundary);
            sb.AppendFormat("Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\n", name, fileName);

            sb.AppendFormat("Content-Type: {0}\r\n\r\n", "application/octet-stream");

            byte[] sbData = Encoding.UTF8.GetBytes(sb.ToString());
            await ms.WriteAsync(sbData, 0, sbData.Length);

            await file.CopyToAsync(ms);
        }

        public Form ToForm()
        {
            byte[] sbData = Encoding.UTF8.GetBytes("\r\n--" + boundary + "--\r\n");
            ms.Write(sbData, 0, sbData.Length);

            _Ended = true;
            return new Form(ms.ToArray(), "multipart/form-data; boundary=" + boundary);
        }

        public async Task<Form> ToFormAsync()
        {
            byte[] sbData = Encoding.UTF8.GetBytes("\r\n--" + boundary + "--\r\n");
            await ms.WriteAsync(sbData, 0, sbData.Length);

            _Ended = true;
            return new Form(ms.ToArray(), "multipart/form-data; boundary=" + boundary);
        }

        public void Dispose()
        {
            ms.Dispose();
        }

    }
}
