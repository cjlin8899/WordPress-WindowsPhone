﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Xml.Serialization;

namespace WordPress.Model
{
    public class Gallery : INotifyPropertyChanged
    {
        public const int DEFAULT_NUMBER_OF_COLUMNS = 3;

        public event PropertyChangedEventHandler PropertyChanged;
        private bool _enabled;
        private eGalleryLinkTo _linkTo;
        private int _columns;
        private bool _randomOrder;
        private eGalleryType _type;
        private bool _contentBelow;

        public Gallery()
        {
            _columns = DEFAULT_NUMBER_OF_COLUMNS;
            _contentBelow = true;
        }

        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (value != _enabled)
                {
                    _enabled = value;
                    NotifyPropertyChanged("Enabled");
                }
            }
        }

        [XmlIgnore]
        public eGalleryLinkTo LinkTo
        {
            get { return _linkTo; }
            set
            {
                if (value != _linkTo)
                {
                    _linkTo = value;
                    NotifyPropertyChanged("LinkTo");
                }
            }
        }

        // Workaround to makean  enum serializable: what the snippet does is tell the XmlSerializer - IGNORE the LinkTo property (see above)
        // And SERIALIZE the FooInt32 Property, which just casts the LinkTo prop to an Int32 value.
        [XmlElement("Foo")]
        [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
        public int FooInt32
        {
            get { return (int)LinkTo; }
            set { LinkTo = (eGalleryLinkTo)value; }
        }

        public int Columns
        {
            get { return _columns; }
            set
            {
                if (value != _columns)
                {
                    _columns = value;
                    NotifyPropertyChanged("Columns");
                }
            }
        }

        public bool RandomOrder
        {
            get { return _randomOrder; }
            set
            {
                if (value != _randomOrder)
                {
                    _randomOrder = value;
                    NotifyPropertyChanged("RandomOrder");
                }
            }
        }

        [XmlIgnore]
        public eGalleryType Type
        {
            get { return _type; }
            set
            {
                if (value != _type)
                {
                    _type = value;
                    NotifyPropertyChanged("Type");
                }
            }
        }

        [XmlElement("Foo2")]
        [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
        public int Foo2Int32
        {
            get { return (int)Type; }
            set { Type = (eGalleryType)value; }
        }

        public bool ContentBelow
        {
            get { return _contentBelow; }
            set
            {
                if (value != _contentBelow)
                {
                    _contentBelow = value;
                    NotifyPropertyChanged("ContentBelow");
                }
            }
        }

        public string GenerateShortcode(ICollection<string> mediaIds)
        {
            if (mediaIds.Count < 1)
                return "";

            List<string> galleryAttributes = new List<string>();

            galleryAttributes.Add(String.Format("ids=\"{0}\"", String.Join(",", mediaIds)));

            if (_columns != DEFAULT_NUMBER_OF_COLUMNS)
                galleryAttributes.Add(String.Format("columns=\"{0}\"", _columns));

            if (_randomOrder)
                galleryAttributes.Add("orderby=\"rand\"");

            if (_linkTo == eGalleryLinkTo.MediaFile)
                galleryAttributes.Add("link=\"file\"");

            if (_type != eGalleryType.Default)
                galleryAttributes.Add(GetTypeAttribute());

            return String.Format("[gallery {0}]", String.Join(" ", galleryAttributes));
        }

        private string GetTypeAttribute()
        {
            string type;
            if (_type == eGalleryType.Tiles)
                type = "rectangular";
            else if (_type == eGalleryType.SquareTiles)
                type = "square";
            else if (_type == eGalleryType.Circles)
                type = "circle";
            else
                type = "slideshow";

            return String.Format("type=\"{0}\"", type);
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            if (null != PropertyChanged)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}