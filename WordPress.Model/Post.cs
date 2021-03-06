﻿using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Collections.Generic;

namespace WordPress.Model
{
    public class Post : INotifyPropertyChanged
    {
        #region member variables

        private DateTime _dateCreated;
        private string _userId;
        private string _postId;
        private string _description = "";
        private string _title;
        private string _link;
        private string _permaLink;
        private ObservableCollection<string> _categories;
        private string _mtExcerpt;
        private bool _mtAllowComments;
        private bool _mtAllowPings;
        private string _mtKeyWords;
        private string _wpSlug;
        private string _wpPassword;
        private string _wpAuthorId;
        private string _wpAuthorDisplayName;
        private DateTime _dateCreatedGMT;
        private string _postStatus;
        private ObservableCollection<CustomField> _customFields;
        private bool _sticky;
        private bool _isNew;
        private ObservableCollection<Media> _media;
        private string _postFormat;
        private string _postThumbnail = "";
        private Gallery _gallery;
        private MediaItem _featuredImage;

        private const string DATECREATED_VALUE = "dateCreated";
        private const string USERID_VALUE = "userid";
        private const string POSTID_VALUE = "postid";
        private const string DESCRIPTION_VALUE = "description";
        private const string TITLE_VALUE = "title";
        private const string LINK_VALUE = "link";
        private const string PERMALINK_VALUE = "permaLink";
        private const string CATEGORIES_VALUE = "categories";
        private const string MTEXCERPT_VALUE = "mt_excerpt";
        private const string MTTEXTMORE_VALUE = "mt_text_more";
        private const string MTALLOWCOMMENTS_VALUE = "mt_allow_comments";
        private const string MTALLOWPINGS_VALUE = "mt_allow_pings";
        private const string MTKEYWORDS_VALUE = "mt_keywords";
        private const string WPSLUG_VALUE = "wp_slug";
        private const string WPPASSWORD_VALUE = "wp_password";
        private const string WPAUTHORID_VALUE = "wp_author_id";
        private const string WPAUTHORDISPLAYNAME_VALUE = "wp_author_display_name";
        private const string DATECREATEDGMT_VALUE = "date_created_gmt";
        private const string POSTSTATUS_VALUE = "post_status";
        private const string CUSTOMFIELDS_VALUE = "custom_fields";
        private const string STICKY_VALUE = "sticky";
        private const string POSTFORMAT_VALUE = "wp_post_format";
        private const string POSTTHUMBNAIL_VALUE = "wp_post_thumbnail";

        private const int MINIMUM_NUMBER_OF_PHOTOS_FOR_GALLERY = 2;

        #endregion

        #region events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region constructors

        public Post()
        {
            _isNew = true;
            _categories = new ObservableCollection<string>();
            _categories.CollectionChanged += OnCategoriesChanged;
            _customFields = new ObservableCollection<CustomField>();
            _media = new ObservableCollection<Media>();
            _gallery = new Gallery();
            _postFormat = "standard";
        }

        public Post(XElement structElement)
            :this()
        {
            _postFormat = "standard";
            _gallery = new Gallery();
            ParseElement(structElement);
            _media = new ObservableCollection<Media>();
        }

        #endregion

        #region properties

        public DateTime DateCreated
        {
            get { return _dateCreated; }
            set 
            {
                if (value != _dateCreated)
                {
                    _dateCreated = value;
                    NotifyPropertyChanged("DateCreated");
                }
            }
        }

        public string UserId
        {
            get { return _userId; }
            set 
            {
                if (value != _userId)
                {
                    _userId = value;
                    NotifyPropertyChanged("UserId");
                }
            }
        }

        public string PostId
        {
            get { return _postId; }
            set
            {
                if (value != _postId)
                {
                    _postId = value;
                    NotifyPropertyChanged("PostId");
                }
            }
        }

        public string Description
        {
            get { return _description; }
            set 
            {
                if (value != _description)
                {
                    _description = value;
                    NotifyPropertyChanged("Description");
                }
            }
        }

        public string Title
        {
            get { return _title; }
            set
            {
                if (value != _title)
                {
                    _title = value;
                    NotifyPropertyChanged("Title");
                }
            }
        }

        public string Link
        {
            get { return _link; }
            set 
            {
                if (value != _link)
                {
                    _link = value;
                    NotifyPropertyChanged("Link");
                }
            }
        }

        public string PermaLink
        {
            get { return _permaLink; }
            set
            {
                if (value != _permaLink)
                {
                    _permaLink = value;
                    NotifyPropertyChanged("PermaLink");
                }
            }
        }

        public ObservableCollection<string> Categories
        {
            get { return _categories; }
            private set { _categories = value; }
        }

        public string MtExcerpt
        {
            get { return _mtExcerpt; }
            set
            {
                if (value != _mtExcerpt)
                {
                    _mtExcerpt = value;
                    NotifyPropertyChanged("MtExcerpt");
                }
            }
        }

        public bool MtAllowComments
        {
            get { return _mtAllowComments; }
            set
            {
                if (value != _mtAllowComments)
                {
                    _mtAllowComments = value;
                    NotifyPropertyChanged("MtAllowComments");
                }
            }
        }

        public bool MtAllowPings
        {
            get { return _mtAllowPings; }
            set
            {
                if (value != _mtAllowPings)
                {
                    _mtAllowPings = value;
                    NotifyPropertyChanged("MtAllowPings");
                }
            }
        }

        public string MtKeyWords
        {
            get { return _mtKeyWords; }
            set 
            {
                if (value != _mtKeyWords)
                {
                    _mtKeyWords = value;
                    NotifyPropertyChanged("MtKeyWords");
                }
            }
        }

        public string WpSlug
        {
            get { return _wpSlug; }
            set 
            {
                if (value != _wpSlug)
                {
                    _wpSlug = value;
                    NotifyPropertyChanged("WpSlug");
                }
            }
        }

        public string WpPassword
        {
            get { return _wpPassword; }
            set
            {
                if (value != _wpPassword)
                {
                    _wpPassword = value;
                    NotifyPropertyChanged("WpPassword");
                }
            }
        }

        public string WpAuthorId
        {
            get { return _wpAuthorId; }
            set 
            {
                if (value != _wpAuthorId)
                {
                    _wpAuthorId = value;
                    NotifyPropertyChanged("WpAuthorId");
                }
            }
        }

        public string WpAuthorDisplayName
        {
            get { return _wpAuthorDisplayName; }
            set
            {
                if (value != _wpAuthorDisplayName)
                {
                    _wpAuthorDisplayName = value;
                    NotifyPropertyChanged("WpAuthorDisplayName");
                }
            }
        }
        
        public DateTime DateCreatedGMT
        {
            get { return _dateCreatedGMT; }
            set
            {
                if (value != _dateCreatedGMT)
                {
                    _dateCreatedGMT = value;
                    NotifyPropertyChanged("DateCreatedGMT");
                }
            }
        }
        
        public string PostStatus
        {
            get { return _postStatus; }
            set
            {
                if (value != _postStatus)
                {
                    _postStatus = value;
                    NotifyPropertyChanged("PostStatus");
                }
            }
        }
        
        public ObservableCollection<CustomField> CustomFields
        {
            get { return _customFields; }
            private set { _customFields = value; }
        }
        
        public bool Sticky
        {
            get { return _sticky; }
            set 
            {
                if (value != _sticky)
                {
                    _sticky = value;
                    NotifyPropertyChanged("Sticky");
                }
            }
        }

        public bool IsNew
        {
            get { return _isNew; }
            set { _isNew = value; }
        }

        public ObservableCollection<Media> Media
        {
            get { return _media; }
            set { _media = value; }
        }


        public string PostFormat
        {
            get { return _postFormat; }
            set
            {
                if (value != _postFormat)
                {
                    _postFormat = value;
                    NotifyPropertyChanged("PostFormat");
                }
            }
        }

        public string PostThumbnail
        {
            get { return _postThumbnail; }
            set
            {
                if (value != _postThumbnail)
                {
                    _postThumbnail = value;
                    NotifyPropertyChanged("PostThumbnail");
                }
            }
        }


        public Gallery Gallery
        {
            get { return _gallery; }
            set
            {
                if (value != _gallery)
                {
                    _gallery = value;
                    NotifyPropertyChanged("Gallery");
                }
            }
        }

        public MediaItem FeaturedImage
        {
            get { return _featuredImage; }
            set
            {
                if (value != _featuredImage)
                {
                    _featuredImage = value;
                    NotifyPropertyChanged("FeaturedImage");
                }
            }
        }

        public bool HasMedia()
        {
            return Media != null && Media.Count > 0;
        }


        public bool isGalleryAvailable()
        {
            int numberOfPictures = this.Media.Count;

            foreach (Media m in this.Media)
            {
                if (m.IsFeatured == true)
                {
                    numberOfPictures = this.Media.Count - 1;
                    break;
                }
            }

            return numberOfPictures >= MINIMUM_NUMBER_OF_PHOTOS_FOR_GALLERY;
        }

        public bool IsLocalDraft()
        {
            return PostStatus.Equals("localdraft");
        }

        public void AddTagline(string tagline)
        {
            Description = Description + "\r\n<p class=\"post-sig\">" + tagline + "</p>";
        }

        public void GenerateImageMarkup(bool placeImageAboveText, bool galleryEnabled)
        {
            StringBuilder prependBuilder = new StringBuilder();
            StringBuilder appendBuilder = new StringBuilder();
            List<string> mediaIds = new List<string>();

            foreach (Media currentMedia in Media)
            {
                if (currentMedia.IsFeatured)
                {
                    PostThumbnail = currentMedia.Id;
                }
                else if (galleryEnabled)
                {
                    mediaIds.Add(currentMedia.Id);
                }
                else
                {
                    if (currentMedia.placement != eMediaPlacement.BlogPreference)
                    {
                        if (currentMedia.placement == eMediaPlacement.Before)
                        {
                            prependBuilder.Append(currentMedia.getHTML());
                        }
                        else
                        {
                            appendBuilder.Append(currentMedia.getHTML());
                        }
                    }
                    else
                    {
                        if (placeImageAboveText)
                        {
                            prependBuilder.Append(currentMedia.getHTML());
                        }
                        else
                        {
                            appendBuilder.Append(currentMedia.getHTML());
                        }
                    }
                }
            }

            if (mediaIds.Count >= 1)
            {
                string galleryShortcode = Gallery.GenerateShortcode(mediaIds);
                if (Gallery.ContentBelow)
                {
                    appendBuilder.AppendFormat("\n{0}", galleryShortcode);
                }
                else
                {
                    prependBuilder.AppendFormat("{0}\n", galleryShortcode);
                }
            }

            Description = prependBuilder + Description + appendBuilder;
        }

        #endregion

        #region methods

        private void NotifyPropertyChanged(string propertyName)
        {
            if (null != PropertyChanged)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void ParseElement(XElement element)
        {
            if (!element.HasElements)
            {
                throw new XmlRPCParserException(XmlRPCResponseConstants.XELEMENTMISSINGCHILDELEMENTS_CODE, XmlRPCResponseConstants.XELEMENTMISSINGCHILDELEMENTS_MESSAGE);
            }

            string _mtTextMore = null;
            foreach (XElement member in element.Descendants(XmlRPCResponseConstants.MEMBER))
            {
                string value = string.Empty;
                string memberName = member.Element(XmlRPCResponseConstants.NAME).Value;
                if (DATECREATED_VALUE.Equals(memberName))
                {
                    value = member.Descendants(XmlRPCResponseConstants.DATETIMEISO8601).First().Value;
                    DateTime tempDate;
                    if (DateTime.TryParseExact(value, Constants.WORDPRESS_DATEFORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out tempDate))
                    {
                        _dateCreated = tempDate; //This value will be replaced with the correct value obtained from the GMT -> local time conversion (later in this method).
                    }
                    else
                    {
                        Exception detailedError = new FormatException("Unable to parse dataCreated: " + value);
                        throw new XmlRPCParserException(XmlRPCResponseConstants.SERVER_RETURNED_INVALID_XML_RPC_CODE, XmlRPCResponseConstants.SERVER_RETURNED_INVALID_XML_RPC_MESSAGE, detailedError);
                    }
                }
                else if (USERID_VALUE.Equals(memberName))
                {
                    try
                    {
                        value = member.GetValueAsString(false);
                    }
                    catch (Exception)
                    {
                        _userId = "-1";
                    }
                }
                else if (POSTID_VALUE.Equals(memberName))
                {                        
                    _postId = member.GetValueAsString(true);
                }
                else if (DESCRIPTION_VALUE.Equals(memberName))
                {
                    value = member.Descendants(XmlRPCResponseConstants.STRING).First().Value;
                    _description = value.HtmlDecode();
                }
                else if (TITLE_VALUE.Equals(memberName))
                {
                    value = member.Descendants(XmlRPCResponseConstants.STRING).First().Value;
                    _title = value.HtmlDecode();
                }
                else if (LINK_VALUE.Equals(memberName))
                {
                    value = member.Descendants(XmlRPCResponseConstants.STRING).First().Value;
                    _link = value;
                }
                else if (PERMALINK_VALUE.Equals(memberName))
                {
                    value = member.Descendants(XmlRPCResponseConstants.STRING).First().Value;
                    _permaLink = value;
                }
                else if (CATEGORIES_VALUE.Equals(memberName))
                {
                    foreach (XElement dataElement in member.Descendants(XmlRPCResponseConstants.STRING))
                    {
                        _categories.Add(dataElement.Value.HtmlDecode());
                    }
                }
                else if (MTEXCERPT_VALUE.Equals(memberName))
                {
                    value = member.Descendants(XmlRPCResponseConstants.STRING).First().Value;
                    _mtExcerpt = value.HtmlDecode();
                }
                else if (MTTEXTMORE_VALUE.Equals(memberName))
                {
                    value = member.Descendants(XmlRPCResponseConstants.STRING).First().Value;
                    _mtTextMore = value.HtmlDecode();
                }
                else if (MTALLOWCOMMENTS_VALUE.Equals(memberName))
                {
                    value = member.Descendants(XmlRPCResponseConstants.INT).First().Value;
                    try
                    {
                        _mtAllowComments = Convert.ToBoolean(Convert.ToInt16(value));
                    }
                    catch
                    {
                        _mtAllowComments = false;
                    }
                }
                else if (MTALLOWPINGS_VALUE.Equals(memberName))
                {
                    value = member.Descendants(XmlRPCResponseConstants.INT).First().Value;
                    try
                    {
                        _mtAllowPings = Convert.ToBoolean(Convert.ToInt16(value));
                    }
                    catch
                    {
                        _mtAllowPings = false;
                    }
                }
                else if (MTKEYWORDS_VALUE.Equals(memberName))
                {
                    value = member.Descendants(XmlRPCResponseConstants.STRING).First().Value;
                    _mtKeyWords = value.HtmlDecode();
                }
                else if (WPSLUG_VALUE.Equals(memberName))
                {
                    value = member.Descendants(XmlRPCResponseConstants.STRING).First().Value;
                    _wpSlug = value.HtmlDecode();
                }
                else if (WPPASSWORD_VALUE.Equals(memberName))
                {
                    value = member.Descendants(XmlRPCResponseConstants.STRING).First().Value;
                    _wpPassword = value.HtmlDecode();
                }
                else if (WPAUTHORID_VALUE.Equals(memberName))
                {
                    _wpAuthorId = member.GetValueAsString(false);
                }
                else if (WPAUTHORDISPLAYNAME_VALUE.Equals(memberName))
                {
                    value = member.Descendants(XmlRPCResponseConstants.STRING).First().Value;
                    _wpAuthorDisplayName = value.HtmlDecode();
                }
                else if (DATECREATEDGMT_VALUE.Equals(memberName))
                {
                    value = member.Descendants(XmlRPCResponseConstants.DATETIMEISO8601).First().Value;
                    DateTime tempDate;
                    if (DateTime.TryParseExact(value, Constants.WORDPRESS_DATEFORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out tempDate))
                    {
                        _dateCreatedGMT = tempDate;
                        _dateCreated = tempDate.ToLocalTime(); //set the post's local time by using the device timezone offset.
                    }
                    else
                    {
                        Exception detailedError = new FormatException("Unable to parse GMT date-time: " + value);
                        throw new XmlRPCParserException(XmlRPCResponseConstants.SERVER_RETURNED_INVALID_XML_RPC_CODE, XmlRPCResponseConstants.SERVER_RETURNED_INVALID_XML_RPC_MESSAGE, detailedError);
                    }
                }
                else if (POSTSTATUS_VALUE.Equals(memberName))
                {
                    value = member.Descendants(XmlRPCResponseConstants.STRING).First().Value;
                    _postStatus = value;
                }
                else if (CUSTOMFIELDS_VALUE.Equals(memberName))
                {
                    CustomField field = null;
                    foreach (XElement childStructElement in element.Descendants(XmlRPCResponseConstants.STRUCT))
                    {
                        field = new CustomField(childStructElement);
                        _customFields.Add(field);
                    }
                }
                else if (STICKY_VALUE.Equals(memberName))
                {
                    value = member.Descendants(XmlRPCResponseConstants.BOOLEAN).First().Value;
                    try
                    {
                        _sticky = Convert.ToBoolean(Convert.ToInt16(value));
                    }
                    catch
                    {
                        _sticky = false;
                    }
                }
                else if (POSTFORMAT_VALUE.Equals(memberName))
                {
                    value = member.Descendants(XmlRPCResponseConstants.STRING).First().Value;
                    _postFormat = value;
                }
                else if (POSTTHUMBNAIL_VALUE.Equals(memberName))
                {
                    value = member.Descendants(XmlRPCResponseConstants.STRING).First().Value;
                    _postThumbnail = value;
                }
            } // end for-each

            if (!String.IsNullOrEmpty(_mtTextMore))
                _description = _description + "<!--more-->\n" + _mtTextMore;
            
            IsNew = false;
        }

        private void OnCategoriesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("Categories");
        }

        #endregion

    }
}
