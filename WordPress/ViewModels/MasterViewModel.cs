﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;

using WordPress.Model;
using System.ComponentModel;
using Microsoft.Phone.Shell;
using System.Collections.Generic;

namespace WordPress
{
    public class MasterViewModel: INotifyPropertyChanged
    {
        #region member variables
        #endregion

        #region events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region constructors

        public MasterViewModel()
        {
            Blogs = DataService.Current.Blogs;
        }

        #endregion


        #region properties

        public ObservableCollection<Blog> Blogs { get; private set; }

        public Blog CurrentBlog
        {
            get { return DataService.Current.CurrentBlog; }
            set 
            {
                if (value != DataService.Current.CurrentBlog)
                {
                    DataService.Current.CurrentBlog = value;
                    NotifyPropertyChanged("CurrentBlog");
                
                    NotifyPropertyChanged("Comments");
                    NotifyPropertyChanged("Posts");
                    NotifyPropertyChanged("Pages");
                }
            }
        }

        public ObservableCollection<Comment> Comments
        {
            get
            {
                if (null == CurrentBlog)
                {
                    return null;
                }
                return CurrentBlog.Comments;
            }
        }

        public Comment CurrentComment
        {
            get { return DataService.Current.CurrentComment; }
            set 
            {
                if (value != DataService.Current.CurrentComment)
                {
                    DataService.Current.CurrentComment = value;
                    NotifyPropertyChanged("CurrentComment");
                }
            }
        }

        public ObservableCollection<PostListItem> Posts
        {
            get
            {
                if (null == CurrentBlog) 
                {
                    return null; 
                }

                //1. Mark the drafts to remove from the list
                List<PostListItem> draftToRemove = new List<PostListItem>();
                foreach (PostListItem post in CurrentBlog.PostListItems)
                {
                    if (post.PostId == "-1") //draft
                        draftToRemove.Add(post);
                }
                //2. Remove the drafts
                foreach (PostListItem post in draftToRemove)
                {
                    CurrentBlog.PostListItems.Remove(post);
                }
                
                //3. Re-add the drafts
                CurrentBlog.addLocalPostDraftsToPostList();

                return CurrentBlog.PostListItems;
            }
        }

        public PostListItem CurrentPostListItem
        {
            get { return DataService.Current.CurrentPostListItem; }
            set
            {
                if (value != DataService.Current.CurrentPostListItem)
                {
                    DataService.Current.CurrentPostListItem = value;
                    NotifyPropertyChanged("CurrentPostListItem");
                }
            }
        }

        public ObservableCollection<PageListItem> Pages 
        {
            get
            {
                if (null == CurrentBlog)
                {
                    return null;
                }

                //1. Mark the drafts to remove from the list
                List<PageListItem> draftToRemove = new List<PageListItem>();
                foreach (PageListItem page in CurrentBlog.PageListItems)
                {
                    if (page.PageId == "-1") //draft
                        draftToRemove.Add(page);
                }
                //2. Remove the drafts
                foreach (PageListItem page in draftToRemove)
                {
                    CurrentBlog.PageListItems.Remove(page);
                }

                //3. Re-add the drafts
                CurrentBlog.addLocalPageDraftsToPostList();

                return CurrentBlog.PageListItems;
            }
        }

        public PageListItem CurrentPageListItem
        {
            get { return DataService.Current.CurrentPageListItem; }
            set
            {
                if (value != DataService.Current.CurrentPageListItem)
                {
                    DataService.Current.CurrentPageListItem = value;
                    NotifyPropertyChanged("CurrentPageListItem");
                }
            }
        }

        public Post CurrentPost
        {
            get { return DataService.Current.CurrentPost; }
            set
            {
                if (value != DataService.Current.CurrentPost)
                {
                    DataService.Current.CurrentPost = value;
                    NotifyPropertyChanged("CurrentPost");
                }
            }
        }

        private string _sharingPhotoToken = null;
        public string SharingPhotoToken
        {
            get { return _sharingPhotoToken; }
            set
            {
                if (value != _sharingPhotoToken)
                {
                    _sharingPhotoToken = value;
                    NotifyPropertyChanged("SharingPhotoToken");
                }
            }
        }

        #endregion

        #region methods

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            if (null != PropertyChanged)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public Uri BuildBlogTileUrl(Blog blog)
        {
            return new Uri("/BlogPanoramaPage.xaml?Blog=" + blog.Xmlrpc, UriKind.Relative);
        }

        /// <summary>
        /// Searches for a secondary ShellTile for a blog.
        /// </summary>
        /// <param name="blog">Blog to find a tile for. If null, searches for the currently active blog.</param>
        public ShellTile FindBlogTile(Blog blog=null)
        {
            Uri blogUri = App.MasterViewModel.BuildBlogTileUrl(blog ?? CurrentBlog);
            return ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri == blogUri);
        }

        #endregion

    }
}
