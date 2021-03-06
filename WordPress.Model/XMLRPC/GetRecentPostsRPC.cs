﻿using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace WordPress.Model
{
    public class GetRecentPostsRPC : XmlRemoteProcedureCall<PostListItem>
    {
        #region member variables

        private readonly string _content;

        private const string METHODNAME_VALUE = "mt_getRecentPostTitles";

        #endregion

        #region constructors

        public GetRecentPostsRPC()
            : base()
        {
            _content = XMLRPCTable.mt_getRecentPostTitles;
            MethodName = METHODNAME_VALUE;
        }

        public GetRecentPostsRPC(Blog blog)
            : base(blog.Xmlrpc, METHODNAME_VALUE, blog.Username, blog.Password)
        {
            _content = XMLRPCTable.mt_getRecentPostTitles;
            BlogId = blog.BlogId;
        }

        #endregion

        #region properties

        public string AppKey { get; set; }

        public int BlogId { get; set; }

        public int NumberOfPosts { get; set; }

        #endregion

        #region methods
        
        protected override void ValidateValues()
        {
            base.ValidateValues();

            if (-1 >= BlogId)
            {
                throw new ArgumentException("BlogId is an invalid value", "BlogId");
            }
        }
        protected override string BuildPostContentString()
        {
            string result = string.Format(_content, 
                BlogId,
                Credentials.UserName.HtmlEncode(),
                Credentials.Password.HtmlEncode(), 
                NumberOfPosts);
            return result;
        }

        protected override List<PostListItem> ParseResponseContent(XDocument xDoc)
        {
            List<PostListItem> result = new List<PostListItem>();
            foreach (XElement structElement in xDoc.Descendants(XmlRPCResponseConstants.STRUCT))
            {
                PostListItem current = new PostListItem(structElement);
                result.Add(current);
            }

            return result;
        }

        #endregion

    }
}
