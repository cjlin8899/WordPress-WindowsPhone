﻿using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Microsoft.Xna.Framework.Media;

using WordPress.Converters;
using WordPress.Localization;
using WordPress.Model;
using WordPress.Settings;
using System.Windows.Input;
using System.IO.IsolatedStorage;
using System.Windows.Resources;

namespace WordPress
{
    public partial class EditPostPage : PhoneApplicationPage
    {
        #region member variables

        private static object _syncRoot = new object();

        private const string DATACONTEXT_VALUE = "dataContext";
        private const string PUBLISHKEY_VALUE = "publish";
        private const string TITLEKEY_VALUE = "title";
        private const string CONTENTKEY_VALUE = "content";
        private const string TAGSKEY_VALUE = "tags";

        private StringTable _localizedStrings;
        private ApplicationBarIconButton _saveIconButton;
        private ApplicationBarIconButton _publishIconButton;
        private List<UploadFileRPC> _mediaUploadRPCs;
        private Dictionary<UploadFileRPC, Size> _rpcToImageSizeMap;
        private Dictionary<UploadedFileInfo, UploadFileRPC> _infoToRpcMap;
        private List<UploadedFileInfo> _uploadInfo;

        private bool _mediaDialogPresented = false;
        
        #endregion

        #region constructors

        public EditPostPage()
        {
            InitializeComponent();

            _localizedStrings = App.Current.Resources["StringTable"] as StringTable;

            ApplicationBar = new ApplicationBar();
            ApplicationBar.BackgroundColor = (Color)App.Current.Resources["AppbarBackgroundColor"];
            ApplicationBar.ForegroundColor = (Color)App.Current.Resources["WordPressGrey"];

            _saveIconButton = new ApplicationBarIconButton(new Uri("/Images/appbar.save.png", UriKind.Relative));
            _saveIconButton.Text = _localizedStrings.ControlsText.SaveDraft;
            _saveIconButton.Click += OnSaveButtonClick;
            ApplicationBar.Buttons.Add(_saveIconButton);

            _publishIconButton = new ApplicationBarIconButton(new Uri("/Images/appbar.upload.png", UriKind.Relative));
            _publishIconButton.Text = _localizedStrings.ControlsText.Publish;
            _publishIconButton.Click += OnSaveButtonClick;
            ApplicationBar.Buttons.Add(_publishIconButton);

            _mediaUploadRPCs = new List<UploadFileRPC>();
            _rpcToImageSizeMap = new Dictionary<UploadFileRPC, Size>();
            _infoToRpcMap = new Dictionary<UploadedFileInfo, UploadFileRPC>();

            _uploadInfo = new List<UploadedFileInfo>();

            Loaded += OnPageLoaded;
        }

        #endregion

        #region methods

        private void OnPageLoaded(object sender, EventArgs args)
        {
            App.WaitIndicationService.RootVisualElement = LayoutRoot;

            if (!(State.ContainsKey(TITLEKEY_VALUE)))
            {
                LoadBlog();
            }
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            if (Visibility.Visible == addLinkControl.Visibility)
            {
                HideAddLinkControl();
                e.Cancel = true;
            }
            else
            {
                string prompt = string.Format(_localizedStrings.Prompts.SureCancel, _localizedStrings.Prompts.Post);
                MessageBoxResult result = MessageBox.Show(prompt, _localizedStrings.Prompts.CancelEditing, MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    base.OnBackKeyPress(e);
                }
                else
                {
                    e.Cancel = true;
                }
            }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            //check for transient data stored in State dictionary
            if (State.ContainsKey(TITLEKEY_VALUE))
            {
                RestorePageState();
            }
        }

        /// <summary>
        /// Retrieves transient data from the page's State dictionary
        /// </summary>
        private void RestorePageState()
        {
            if (State.ContainsKey(DATACONTEXT_VALUE))
            {
                DataContext = State[DATACONTEXT_VALUE];
            }

            if (State.ContainsKey(TITLEKEY_VALUE))
            {
                titleTextBox.Text = State[TITLEKEY_VALUE] as string;
            }

            if (State.ContainsKey(CONTENTKEY_VALUE))
            {
                contentTextBox.Text = State[CONTENTKEY_VALUE] as string;
            }

            if (State.ContainsKey(TAGSKEY_VALUE))
            {
                tagsTextBox.Text = State[TAGSKEY_VALUE] as string;
            }

            CategoryContentConverter converter = Resources["CategoryContentConverter"] as CategoryContentConverter;
            if (null == converter) return;

            categoriesTextBlock.Text = converter.Convert(App.MasterViewModel.CurrentPost.Categories, typeof(string), null, null) as string;
        }

        /// <summary>
        /// Locates a Post object and specifies the result as the page's DataContext
        /// </summary>
        private void LoadBlog()
        {
            Blog currentBlog = App.MasterViewModel.CurrentBlog;

            BlogName.Text = currentBlog.BlogNameUpper;

            bool isSharingPhoto = (App.MasterViewModel.SharingPhotoToken != null);
            
            if (null != App.MasterViewModel.CurrentPostListItem && !isSharingPhoto)
            {
                string postId = App.MasterViewModel.CurrentPostListItem.PostId;

                GetPostRPC rpc = new GetPostRPC(currentBlog, postId);
                rpc.Completed += OnGetPostRPCCompleted;
                rpc.ExecuteAsync();

                App.WaitIndicationService.ShowIndicator(_localizedStrings.Messages.RetrievingPost);
            }
            else
            {
                Post post = new Post();
                App.MasterViewModel.CurrentPost = post;
                post.DateCreated = DateTime.Now;
                post.DateCreatedGMT = DateTime.Now.ToUniversalTime();
                DataContext = post;
                /*postTimePicker.Value = post.DateCreated;
                postDatePicker.Value = post.DateCreated;*/
                if (isSharingPhoto)
                {
                    MediaLibrary library = new MediaLibrary();
                    Picture picture = library.GetPictureFromToken(App.MasterViewModel.SharingPhotoToken);
                    AddNewMediaStream(picture.GetImage(), picture.Name);

                    // clear the photo token so we don't try to add it to another post
                    App.MasterViewModel.SharingPhotoToken = null;

                    // blog selection page will be in the backstack, but if the user hits Back they should leave the app
                    // and return to the photo that they were sharing (e.g., so they can share it on another service)
                    NavigationService.RemoveBackEntry();
                }
            }
        }

        private void OnGetPostRPCCompleted(object sender, XMLRPCCompletedEventArgs<Post> args)
        {            
            GetPostRPC rpc = sender as GetPostRPC;
            rpc.Completed -= OnGetPostRPCCompleted;

            if (null == args.Error)
            {
                Post post = args.Items[0];
                DataContext = post;
                App.MasterViewModel.CurrentPost = post;
                if (post.MtKeyWords != "")
                {
                    tagsTextBox.Text = post.MtKeyWords;
                }
            }
            else
            {
                this.HandleException(args.Error);
            }

            App.WaitIndicationService.HideIndicator();
        }

        private void OnSaveButtonClick(object sender, EventArgs e)
        {
            _mediaDialogPresented = false;
            Post post = App.MasterViewModel.CurrentPost;
            if (sender == _publishIconButton)
                post.PostStatus = "publish";
            else
                post.PostStatus = "draft";

            if (0 < _mediaUploadRPCs.Count)
            {
                UploadImagesAndSavePost();
                return;
            }

            SavePost();
        }

        private void UploadImagesAndSavePost()
        {
            this.Focus(); //hide the keyboard
            ApplicationBar.IsVisible = false; //hide the application bar
            App.WaitIndicationService.ShowIndicator(_localizedStrings.Messages.UploadingMedia);
            
            //make sure nothing is in our results collection
            _uploadInfo.Clear();

            //fire off the worker rpcs
            _mediaUploadRPCs.ForEach(rpc => rpc.ExecuteAsync());
        }
       
        //Title text field KeyUp event handler: Dismiss the keyboard by focusing the main control if the Key pressed is the Enter key
        private void Input_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this.Focus();
            }
        }

        private void SavePost()
        {
            //Post post = DataContext as Post;
            //changed to CurrentPost so categories would save
            Post post = App.MasterViewModel.CurrentPost;
            Blog blog = App.MasterViewModel.CurrentBlog;
            //make sure the post has the latest UI data--the Save button is a ToolbarButton
            //which doesn't force focus to change
            post.Title = titleTextBox.Text;
            post.Description = contentTextBox.Text;
            post.MtKeyWords = tagsTextBox.Text;

            if (post.IsNew)
            {
                UserSettings settings = new UserSettings();
                if (settings.UseTaglineForNewPosts)
                {
                    post.Description = post.Description + "\r\n<p class=\"post-sig\">" + settings.Tagline + "</p>";
                }
                NewPostRPC rpc = new NewPostRPC(App.MasterViewModel.CurrentBlog, post);
                rpc.PostType = ePostType.post;
                if (post.PostStatus == "publish")
                    rpc.Publish = true;
                else
                    rpc.Publish = false;
                rpc.Completed += OnNewPostRPCCompleted;
                rpc.ExecuteAsync();
            }
            else
            {
                EditPostRPC rpc = new EditPostRPC(App.MasterViewModel.CurrentBlog, post);
                if (post.PostStatus == "publish")
                    rpc.Publish = true;
                else
                    rpc.Publish = false;
                rpc.Completed += OnEditPostRPCCompleted;
                rpc.ExecuteAsync();
            }

            this.Focus();
            ApplicationBar.IsVisible = false;
            App.WaitIndicationService.ShowIndicator(_localizedStrings.Messages.UploadingChanges);
        }

        private void OnEditPostRPCCompleted(object sender, XMLRPCCompletedEventArgs<Post> args)
        {
            EditPostRPC rpc = sender as EditPostRPC;
            rpc.Completed -= OnEditPostRPCCompleted;
            ApplicationBar.IsVisible = true;

            if (null == args.Error)
            {
                DataService.Current.FetchCurrentBlogPostsAsync();
                NavigationService.GoBack();
            }
            else
            {
                this.HandleException(args.Error);
            }

            App.WaitIndicationService.HideIndicator();
        }

        private void OnNewPostRPCCompleted(object sender, XMLRPCCompletedEventArgs<Post> args)
        {
            NewPostRPC rpc = sender as NewPostRPC;
            rpc.Completed -= OnNewPostRPCCompleted;
            ApplicationBar.IsVisible = true;

            if (null == args.Error)
            {
                DataService.Current.FetchCurrentBlogPostsAsync();
                NavigationService.GoBack();
            }
            else
            {
                this.HandleException(args.Error);
            }

            App.WaitIndicationService.HideIndicator();
        }

        private void OnBoldToggleButtonClick(object sender, RoutedEventArgs e)
        {
            InsertMarkupTagIntoContent(boldToggleButton, WordPressMarkupTags.BOLD_OPENING_TAG, WordPressMarkupTags.BOLD_CLOSING_TAG);
        }

        private void OnItalicToggleButtonClick(object sender, RoutedEventArgs e)
        {
            InsertMarkupTagIntoContent(italicToggleButton, WordPressMarkupTags.ITALICS_OPENING_TAG, WordPressMarkupTags.ITALICS_CLOSING_TAG);
        }

        private void OnUnderlineToggleButtonClick(object sender, RoutedEventArgs e)
        {
            InsertMarkupTagIntoContent(underlineToggleButton, WordPressMarkupTags.UNDERLINE_OPENING_TAG, WordPressMarkupTags.UNDERLINE_CLOSING_TAG);
        }

        private void OnStrikethroughToggleButtonClick(object sender, RoutedEventArgs e)
        {
            InsertMarkupTagIntoContent(strikethroughToggleButton, WordPressMarkupTags.STRIKETHROUGH_OPENING_TAG, WordPressMarkupTags.STRIKETHROUGH_CLOSING_TAG);
        }

        private void OnBlockquoteToggleButtonClick(object sender, RoutedEventArgs e)
        {
            InsertMarkupTagIntoContent(blockquoteToggleButton, WordPressMarkupTags.BLOCKQUOTE_OPENING_TAG, WordPressMarkupTags.BLOCKQUOTE_CLOSING_TAG);
        }

        private void InsertMarkupTagIntoContent(ToggleButton toggleButton, string openingTag, string closingTag)
        {
            Post post = DataContext as Post;
            string description = contentTextBox.Text;

            int startIndex = contentTextBox.SelectionStart;
            if (description.Length <= startIndex)
            {
                startIndex = description.Length;
            }

            string tag;
            int selectionLength = contentTextBox.SelectionLength;
            if (selectionLength > 0) 
            {
                tag = openingTag;

                description = description.Insert(startIndex, openingTag);
                description = description.Insert(startIndex + openingTag.Length + selectionLength, closingTag);

                // cancel toggle switch
                toggleButton.IsChecked = !toggleButton.IsChecked.Value;
            }
            else 
            {
                if (toggleButton.IsChecked.Value)
                {
                    tag = openingTag;
                }
                else
                {
                    tag = closingTag;
                }

                description = description.Insert(startIndex, tag);
            }

            post.Description = description;

            ThreadPool.QueueUserWorkItem((state) =>
            {
                //yield long enough for the button to take focus away from the text box,
                //then reset focus to the text box
                Thread.Sleep(200);
                Dispatcher.BeginInvoke(() =>
                {
                    contentTextBox.Focus();
                    contentTextBox.SelectionStart = startIndex + tag.Length;
                    contentTextBox.SelectionLength = selectionLength;
                });
            });
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            //store transient data in the State dictionary
            SavePageState();
        }

        /// <summary>
        /// Stores transient data in the page's State dictionary
        /// </summary>
        private void SavePageState()
        {
            if (State.ContainsKey(DATACONTEXT_VALUE))
            {
                State.Remove(DATACONTEXT_VALUE);
            }
            State.Add(DATACONTEXT_VALUE, DataContext);

            if (State.ContainsKey(TITLEKEY_VALUE))
            {
                State.Remove(TITLEKEY_VALUE);
            }
            State.Add(TITLEKEY_VALUE, titleTextBox.Text);

            if (State.ContainsKey(CONTENTKEY_VALUE))
            {
                State.Remove(CONTENTKEY_VALUE);
            }
            State.Add(CONTENTKEY_VALUE, contentTextBox.Text);

            if (State.ContainsKey(TAGSKEY_VALUE))
            {
                State.Remove(TAGSKEY_VALUE);
            }
            State.Add(TAGSKEY_VALUE, tagsTextBox.Text);
        }

        private void OnSelectCategoriesButtonClick(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/SelectCategoriesPage.xaml", UriKind.Relative));
        }

        private void OnAddNewMediaButtonClick(object sender, RoutedEventArgs e)
        {
            AddNewMedia();
        }

        private void AddNewMedia()
        {
            PhotoChooserTask task = new PhotoChooserTask();            
            task.Completed += new EventHandler<PhotoResult>(OnChoosePhotoTaskCompleted);
            task.ShowCamera = true;
            task.Show();
        }

        private void OnChoosePhotoTaskCompleted(object sender, PhotoResult e)
        {
            PhotoChooserTask task = sender as PhotoChooserTask;
            task.Completed -= OnChoosePhotoTaskCompleted;

            if (TaskResult.OK != e.TaskResult) return;

            Stream chosenPhoto = e.ChosenPhoto;
            AddNewMediaStream(chosenPhoto, e.OriginalFileName);
        }

        private void AddNewMediaStream(Stream bitmapStream, string originalFileName)
        {
            //build out ui updates
            BitmapImage image = BuildBitmap(bitmapStream);
            Image imageElement = BuildImageElement(image);
            imageWrapPanel.Children.Add(imageElement);

            // 1.Resize the picture and save the output to the isolated storage if 'PreserveBandwidth' is enabled and dimensions are > 800 px.
            if (App.MasterViewModel.CurrentBlog.PreserveBandwidth && (image.PixelWidth > 800 || image.PixelHeight > 800))
            {
                // Create a file name for the JPEG file in isolated storage.
                String tempJPEG = "TempJPEG";

                // Create a virtual store and file stream. Check for duplicate tempJPEG files.
                var myStore = IsolatedStorageFile.GetUserStoreForApplication();
                if (myStore.FileExists(tempJPEG))
                {
                    myStore.DeleteFile(tempJPEG);
                }

                IsolatedStorageFileStream myFileStream = myStore.CreateFile(tempJPEG);

                /* BitmapImage bitmap = new BitmapImage();
                 bitmap.CreateOptions = BitmapCreateOptions.None;
                 bitmap.SetSource(bitmapStream);
                 WriteableBitmap wb = new WriteableBitmap(bitmap);*/
                WriteableBitmap wb = new WriteableBitmap(image);

                float wRatio = image.PixelWidth / 800F;
                float hRatio = image.PixelHeight / 800F;
                float currentRatio = Math.Max(wRatio, hRatio);
                int resizedWidth = (int)(image.PixelWidth / currentRatio);
                int resizedHeight = (int)(image.PixelHeight / currentRatio);

                // Encode the WriteableBitmap object to a JPEG stream.
                wb.SaveJpeg(myFileStream, resizedWidth, resizedHeight, 0, 85);
                myFileStream.Close();

                //The file is now encoded in the IsolatedStorage
                // Create a new stream from isolated storage
                bitmapStream = myStore.OpenFile(tempJPEG, FileMode.Open, FileAccess.Read);//change the stream reference for uploading
            }

            //Save the picture to the picture library if it's a new picture           
            DateTime capture = DateTime.Now;
            string fileNameFormat = "SavedPicture-{0}{1}{2}{3}{4}{5}{6}"; //year, month, day, hours, min, sec, file extension
            string fileName = string.Format(fileNameFormat,
                capture.Year,
                capture.Month,
                capture.Day,
                capture.Hour,
                capture.Minute,
                capture.Second,
                Path.GetExtension(originalFileName));

            // Save the image to the camera roll or saved pictures album.
            MediaLibrary library = new MediaLibrary();
            // Save the image to the saved pictures album.
            Picture pic = library.SavePicture(fileName, bitmapStream);
            bitmapStream.Close();

            UploadFileRPC rpc = new UploadFileRPC(App.MasterViewModel.CurrentBlog, originalFileName, fileName, true);
            rpc.Completed += OnUploadMediaRPCCompleted;
       
            //store this for later--we'll upload the files once the user hits save
            _mediaUploadRPCs.Add(rpc);

            //we also need the original dimensions for the thumbnail calculations
            _rpcToImageSizeMap.Add(rpc, new Size(image.PixelWidth, image.PixelHeight)); //this is not needed
        }

        private BitmapImage BuildBitmap(Stream bitmapStream)
        {
            BitmapImage image = new BitmapImage();
            image.SetSource(bitmapStream);
            return image;
        }

        private Image BuildImageElement(BitmapImage image)
        {
            Image imageElement = new Image();
            imageElement.Source = image;

            float width = 100F;
            int height = (int)(width / image.PixelWidth * image.PixelHeight);
            imageElement.Width = width;
            imageElement.Height = height;

            imageElement.Margin = new Thickness(10);
            return imageElement;
        }

        private void OnClearMediaButtonClick(object sender, RoutedEventArgs e)
        {
            ClearMedia();
        }

        private void ClearMedia()
        {
            imageWrapPanel.Children.Clear();
            _mediaUploadRPCs.ForEach(rpc => rpc.Completed -= OnUploadMediaRPCCompleted);
            _mediaUploadRPCs.Clear();
            _rpcToImageSizeMap.Clear();
        }

        private void OnUploadMediaRPCCompleted(object sender, XMLRPCCompletedEventArgs<UploadedFileInfo> args)
        {
            UploadFileRPC rpc = sender as UploadFileRPC;
            rpc.Completed -= OnUploadMediaRPCCompleted;

            lock (_syncRoot)
            {
                _mediaUploadRPCs.Remove(rpc);
                if (null == args.Error)
                {
                    _uploadInfo.Add(args.Items[0]);
                }
                if (args.Items.Count > 0)
                {
                    _infoToRpcMap.Add(args.Items[0], rpc);
                }
                else
                {
                    //uh oh, media upload problem
                    App.WaitIndicationService.KillSpinner();
                    ApplicationBar.IsVisible = true;

                    if (!_mediaDialogPresented)
                    {
                        _mediaDialogPresented = true;
                        MessageBoxResult result = MessageBox.Show(_localizedStrings.Prompts.MediaErrorContent, _localizedStrings.Prompts.MediaError, MessageBoxButton.OKCancel);
                        if (result == MessageBoxResult.OK)
                        {
                            UpdatePostContent();
                            SavePost();
                            return;
                        }
                        else
                        {
                            //add the object back since the user wants to have another go at uploading
                            rpc.Completed += OnUploadMediaRPCCompleted;
                            _mediaUploadRPCs.Add(rpc);
                            return;
                        }
                    }
                    
                }
            }

            //if we're not done, bail
            if (0 < _mediaUploadRPCs.Count) return;

            UpdatePostContent();
            App.WaitIndicationService.KillSpinner();
            SavePost();
        }

        private void UpdatePostContent()
        {
            StringBuilder builder = new StringBuilder();

            _uploadInfo.ForEach(info =>
            {
                builder.Append(BuildImgMarkup(info));
            });

            Blog currentBlog = App.MasterViewModel.CurrentBlog;

            if (currentBlog.PlaceImageAboveText)
            {
                contentTextBox.Text = builder.ToString() + contentTextBox.Text;
            }
            else
            {
                contentTextBox.Text += builder.ToString();
            }

            contentTextBox.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        }

        private string BuildImgMarkup(UploadedFileInfo info)
        {
            if (!_infoToRpcMap.ContainsKey(info))
            {
                return string.Empty;
            }

            UploadFileRPC rpc = _infoToRpcMap[info];
            Size originalImageSize = _rpcToImageSizeMap[rpc];

            Blog currentBlog = App.MasterViewModel.CurrentBlog;

            XElement imageNode = new XElement("img");
            imageNode.SetAttributeValue("src", info.Url);
                       
            if (currentBlog.AlignThumbnailToCenter)
            {
                StringBuilder styleBuilder = new StringBuilder();
                styleBuilder.Append("display:block; margin-right:auto; margin-left:auto;");
                imageNode.SetAttributeValue("style", styleBuilder.ToString());
                imageNode.SetAttributeValue("class", "size-full;");
            }
            else
            {
                imageNode.SetAttributeValue("class", "alignnone size-full;");
            }
           
            if (!currentBlog.CreateLinkToFullImage)
            {
                return "<br /><br />" + imageNode.ToString();
            }

            XElement anchorNode = new XElement("a");                        
            anchorNode.SetAttributeValue("href", info.Url);
            anchorNode.Add(imageNode);
            
            return "<br /><br />" + anchorNode.ToString();
        }

        private void OnLinkButtonClick(object sender, RoutedEventArgs e)
        {
            ShowLinkControl();
        }

        private void ShowLinkControl()
        {
            ApplicationBar.IsVisible = false;
            addLinkControl.Show();

            // if content text is selected, pre-populate the dialog's fields
            if (contentTextBox.SelectionLength > 0)
            {
                addLinkControl.LinkText = contentTextBox.SelectedText;

                if (Uri.IsWellFormedUriString(contentTextBox.SelectedText, UriKind.Absolute))
                {
                    addLinkControl.Url = contentTextBox.SelectedText;
                }
            }
        }

        private void HideAddLinkControl()
        {
            addLinkControl.Hide();
            ApplicationBar.IsVisible = true;
        }

        private void OnLinkChosen(object sender, EventArgs e)
        {
            HideAddLinkControl();
            string linkMarkup = addLinkControl.CreateLinkMarkup();
            contentTextBox.SelectedText = linkMarkup;
            contentTextBox.Focus();
        }

        private void OnDatePickerChanged(object sender, DateTimeValueChangedEventArgs e)
        {
            Post post = (Post) App.MasterViewModel.CurrentPost;
            if (post != null && e.NewDateTime != null)
            {
                if (sender == postDatePicker)
                {
                    postTimePicker.Value = e.NewDateTime;
                }
                else if (sender == postTimePicker)
                {
                    postDatePicker.Value = e.NewDateTime;
                }
                post.DateCreated = (DateTime)e.NewDateTime;
                post.DateCreatedGMT = ((DateTime)e.NewDateTime).ToUniversalTime();
            }
        }

        #endregion
    }
}