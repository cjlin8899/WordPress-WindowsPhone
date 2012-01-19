﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Xml.Linq;


namespace WordPress.Model
{
    public class UploadFileRPC : XmlRemoteProcedureCall<UploadedFileInfo>
    {
        #region member variables

        private const string METHODNAME_VALUE = "wp.uploadFile";
        private const string PHOTOCHOOSER_VALUE = "PhotoChooser";
        private const string JPEG_EXTENSION = ".jpg";
        private const string PNG_EXTENSION = ".png";
        private const string BMP_EXTENSION = ".bmp";

        /// <summary>
        /// Holds a format string with the XMLRPC post content
        /// </summary>
        private readonly string _content;

        private string _fileName;

        private Stream _bitmapStream;

        #endregion

        #region constructors

        public UploadFileRPC()
            : base()
        {
            _content = XMLRPCTable.wp_uploadFile;
            MethodName = METHODNAME_VALUE;
        }

        public UploadFileRPC(Blog blog, string fileName, Stream bitmapStream, bool overwrite)
            : base(blog.Xmlrpc, METHODNAME_VALUE, blog.Username, blog.Password)
        {
            _content = XMLRPCTable.wp_uploadFile;

            Blog = blog;
            TranslateFileName(fileName);
            _bitmapStream = bitmapStream;
            Overwrite = overwrite;
        }

        #endregion

        #region properties

        public Blog Blog { get; private set; }

        public string FileName
        {
            get { return _fileName; }
            private set
            {
                _fileName = value;
                TranslateMimeType();
            }
        }

        public string MimeType { get; private set; }

        public bool Overwrite { get; private set; }

        #endregion

        #region methods

        protected override void ValidateValues()
        {
            base.ValidateValues();

            if (null == Blog)
            {
                throw new ArgumentException("Blog may not be null", "Blog");
            }
            if (string.IsNullOrEmpty(FileName))
            {
                throw new ArgumentException("Name may not be null or an empty string.", "Name");
            }
            if (string.IsNullOrEmpty(MimeType))
            {
                throw new ArgumentException("Type may not be null or an empty string", "Type");
            }
            if (null == _bitmapStream)
            {
                throw new ArgumentException("Payload may not be null", "Payload");
            }
        }

        protected override string BuildPostContentString()
        {
            throw new NotSupportedException("BuildPostContentString should not be called on image");
            /*
            string content = "<?xml version=\"1.0\"?><methodCall><methodName>wp.uploadFile</methodName><params>" +
                "<param><value><int>" + Blog.BlogId + "</int></value></param>" +
                "<param><value><string>" + Credentials.UserName.HtmlEncode() + "</string></value></param>" +
                "<param><value><string>" + Credentials.Password.HtmlEncode() + "</string></value></param>" +
                "<param><struct><member><name>name</name><value><string>" + FileName.HtmlEncode() + "</string></value></member>" +
                "<member><name>type</name><value><string>" + MimeType.HtmlEncode() + "</string></value></member>" +
                "<member><name>bits</name><value><base64>";

            byte[] chunk = new byte[3600];
            _bitmapStream.Position = 0;

            int count = 0;
            while ((count = _bitmapStream.Read(chunk, 0, chunk.Length)) > 0)
            {
                string result = Convert.ToBase64String(chunk);
                result = result.Trim();
                content += result;
            }

            content += "</base64></value></member>" +
                "<member><name>overwrite</name><value><bool>" + Convert.ToInt32(Overwrite) + "</bool></value></member>" +
                "</struct></param></params></methodCall>";
            return content;*/
        }
        
        /* Another way of reading a file by chunks
         *             bool readCompleted = false;
               while ( readCompleted == false )
               {
                   int index = 0;
                   //we're trying to keep reading into our chunk until either we reach the end of the stream, or we've read everything we need.
                   while (index < chunk.Length)
                   {
                       int bytesRead = _bitmapStream.Read(chunk, index, chunk.Length - index);
                       this.DebugLog("Read the following noof bytes: " + bytesRead);
                       if (bytesRead == 0)
                       {
                           break;
                       }
                       index += bytesRead;
                   }
                   if (index != 0) // Our previous chunk may have been the last one
                   {
                       string result = Convert.ToBase64String(chunk);
                       result = result.Trim();
                       content += result;
                   }
                   if (index != chunk.Length) // We didn't read a full chunk: we're done
                   {
                       readCompleted = true;
                       this.DebugLog("No more bytes to read");
                   }
               }
               */



        /* Read the whole file into the byte array. Do not use this method.
        int length = (int)_bitmapStream.Length;
        _bitmapStream.Position = 0;
        byte[] payload = new byte[length];
        _bitmapStream.Read(payload, 0, length);
        string result = Convert.ToBase64String(payload);
        result = result.Trim();
        */

        /* Override methods defined in the base class since we are uploading the content of the image by chunk */
        public override void ExecuteAsync()
        {
            ExecuteAsync(Guid.NewGuid());
        }
        
        public override void ExecuteAsync(object taskId)
        {
            ValidateValues();

            AsyncOperation operation = AsyncOperationManager.CreateOperation(taskId);

            //start the async op
            ThreadPool.QueueUserWorkItem((object state) =>
            {
                BeginBuildingHttpWebRequest(operation);
            });
        }

        private void BeginBuildingHttpWebRequest(AsyncOperation asyncOp)
        {
            HttpWebRequest request = HttpWebRequest.CreateHttp(Url) as HttpWebRequest;
            request.AllowAutoRedirect = true;
            request.ContentType = XmlRPCRequestConstants.CONTENTTYPE;
            request.Method = XmlRPCRequestConstants.POST;
            request.UserAgent = Constants.WORDPRESS_USERAGENT;

            State state = new State { Operation = asyncOp, Request = request };

            request.BeginGetRequestStream(OnBeginGetRequestStreamCompleted, state);

            asyncOp.Post(onProgressReportDelegate, new ProgressChangedEventArgs(20, asyncOp.UserSuppliedState));
        }

        private void OnBeginGetRequestStreamCompleted(IAsyncResult result)
        {
            State state = result.AsyncState as State;

            HttpWebRequest request = state.Request;
            Stream contentStream = null;

            try
            {
                contentStream = request.EndGetRequestStream(result);
            }
            catch (Exception ex)
            {
                CompletionMethod(null, ex, false, state.Operation);
                return;
            }

            
            using (contentStream)
            {

                string content = "<?xml version=\"1.0\"?><methodCall><methodName>wp.uploadFile</methodName><params>" +
                "<param><value><int>" + Blog.BlogId + "</int></value></param>" +
                "<param><value><string>" + Credentials.UserName.HtmlEncode() + "</string></value></param>" +
                "<param><value><string>" + Credentials.Password.HtmlEncode() + "</string></value></param>" +
                "<param><struct><member><name>name</name><value><string>" + FileName.HtmlEncode() + "</string></value></member>" +
                "<member><name>type</name><value><string>" + MimeType.HtmlEncode() + "</string></value></member>" +
                "<member><name>bits</name><value><base64>";

                //Write the first chunk of data
                byte[] payload = Encoding.UTF8.GetBytes(content);
                contentStream.Write(payload, 0, payload.Length);
                
                //Write the chunks of the image
                byte[] chunk = new byte[3600];
                _bitmapStream.Position = 0;
                int count = 0;
                while ((count = _bitmapStream.Read(chunk, 0, chunk.Length)) > 0)
                {
                    payload = Encoding.UTF8.GetBytes(Convert.ToBase64String(chunk).Trim());
                    contentStream.Write(payload, 0, payload.Length);
                }

                //Write the last chunk of data
                content = "</base64></value></member>" +
                    "<member><name>overwrite</name><value><bool>" + Convert.ToInt32(Overwrite) + "</bool></value></member>" +
                    "</struct></param></params></methodCall>";

                payload = Encoding.UTF8.GetBytes(content);
                contentStream.Write(payload, 0, payload.Length);           
            }

            request.BeginGetResponse(OnBeginGetResponseCompleted, state);

            state.Operation.Post(onProgressReportDelegate, new ProgressChangedEventArgs(40, state.Operation.UserSuppliedState));
        }

        /* END of the overrided methods */

        protected override List<UploadedFileInfo> ParseResponseContent(XDocument xDoc)
        {
            List<UploadedFileInfo> result = new List<UploadedFileInfo>();
            foreach (XElement structElement in xDoc.Descendants(XmlRPCResponseConstants.STRUCT))
            {
                UploadedFileInfo info = new UploadedFileInfo(structElement);
                result.Add(info);
            }
            return result;
        }

        private void TranslateMimeType()
        {
            //DEV NOTE: PhotoChooserTask only seems to allow pictures, no video.
            //capture everything else (if that is even possible) as
            //application/octet-stream
            string extension = Path.GetExtension(FileName);

            if (JPEG_EXTENSION.Equals(extension))
            {
                MimeType = MimeTypes.JPEG;
            }
            else
            {
                MimeType = MimeTypes.UNKNOWN;
            }
        }

        public void TranslateFileName(string originalFileName)
        {
            //DEV NOTE: the original file name from the PhotoChooserTask is pretty gross.
            //The plan is to nab the extension and use a timestamp for the file name so
            //there's something that doesn't seem crazy when the user checks what media
            //has been uploaded.
            if (originalFileName.Contains(PHOTOCHOOSER_VALUE))
            {
                DateTime capture = DateTime.Now;
                string fileNameFormat = "{0}{1}{2}{3}{4}{5}{6}"; //year, month, day, hours, min, sec, file extension
                string fileName = string.Format(fileNameFormat,
                    capture.Year,
                    capture.Month,
                    capture.Day,
                    capture.Hour,
                    capture.Minute,
                    capture.Second,
                    Path.GetExtension(originalFileName));
                FileName = fileName;
                return;
            }

            //if we're at this point, the file name should be reasonably readable so we'll
            //leave it alone
            FileName = Path.GetFileName(originalFileName);
        }

        #endregion
    }
}
