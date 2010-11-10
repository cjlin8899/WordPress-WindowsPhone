﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

using WordPress.Localization;
using WordPress.Model;

namespace WordPress
{
    public partial class AddExistingWordPressSitePage : PhoneApplicationPage
    {
        #region member variables

        private const string URLKEY_VALUE = "url";
        private const string USERNAMEKEY_VALUE = "username";
        private const string PASSWORDKEY_VALUE = "password";

        private ApplicationBarIconButton _saveIconButton;
        private StringTable _localizedStrings;

        #endregion

        #region constructor

        public AddExistingWordPressSitePage()
        {
            InitializeComponent();

            _localizedStrings = App.Current.Resources["StringTable"] as StringTable;

            ApplicationBar = new ApplicationBar();
            ApplicationBar.BackgroundColor = (Color)App.Current.Resources["AppbarBackgroundColor"];
            ApplicationBar.ForegroundColor = (Color)App.Current.Resources["WordPressGrey"];
            
            _saveIconButton = new ApplicationBarIconButton(new Uri("/Images/appbar.save.png", UriKind.Relative));
            _saveIconButton.Text = _localizedStrings.ControlsText.Save;
            _saveIconButton.Click += OnSaveButtonClick;
            ApplicationBar.Buttons.Add(_saveIconButton);
        }

        #endregion

        #region properties

        #endregion

        #region methods

        private void OnSaveButtonClick(object sender, EventArgs e)
        {
            AttemptToLoginAsync();
        }

        private void AttemptToLoginAsync()
        {
            if (!ValidateUserName())
            {
                PromptUserForInput(_localizedStrings.Prompts.MissingUserName, usernameTextBox);
                return;
            }

            if (!ValidatePassword())
            {
                PromptUserForInput(_localizedStrings.Prompts.MissingPassword, passwordPasswordBox);
                return;
            }

            if (!ValidateUrl())
            {
                PromptUserForInput(_localizedStrings.Prompts.MissingUrl, urlTextBox);
            }

            string username = usernameTextBox.Text;
            string password = passwordPasswordBox.Password;
            string url = urlTextBox.Text;

            GetUsersBlogsRPC rpc = new GetUsersBlogsRPC(url, username, password);
            rpc.Completed += OnGetUsersBlogsCompleted;
            rpc.ExecuteAsync();

            App.WaitIndicationService.ShowIndicator(_localizedStrings.Messages.LoggingIn);
        }

        /// <summary>
        /// If true, the user input for "user name" is valid.
        /// </summary>
        /// <returns></returns>
        private bool ValidateUserName()
        {
            bool result = !string.IsNullOrEmpty(usernameTextBox.Text);
            return result;
        }

        /// <summary>
        /// If true, the user input for "password" is valid.
        /// </summary>
        /// <returns></returns>
        private bool ValidatePassword()
        {
            bool result = !string.IsNullOrEmpty(passwordPasswordBox.Password);
            return result;
        }

        private bool ValidateUrl()
        {
            bool result = !string.IsNullOrEmpty(urlTextBox.Text);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="textbox"></param>
        private void PromptUserForInput(string message, Control control)
        {
            MessageBox.Show(message);
            control.Focus();
        }

        private void OnGetUsersBlogsCompleted(object sender, XMLRPCCompletedEventArgs<Blog> args)
        {
            GetUsersBlogsRPC rpc = sender as GetUsersBlogsRPC;
            rpc.Completed -= OnGetUsersBlogsCompleted;

            App.WaitIndicationService.KillSpinner();

            if (null == args.Error)
            {
                if (1 == args.Items.Count)
                {
                    DataService.Current.Blogs.Add(args.Items[0]);
                    NavigationService.Navigate(new Uri("/BlogsPage.xaml", UriKind.Relative));
                }
                else
                {
                    ShowBlogSelectionControl(args.Items);
                }
            }
            else
            {
                this.HandleException(args.Error);
            }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            App.WaitIndicationService.RootVisualElement = LayoutRoot;

            //retrieve transient data from State dictionary
            if (State.ContainsKey(URLKEY_VALUE))
            {
                urlTextBox.Text = (string)State[URLKEY_VALUE];
            }

            if (State.ContainsKey(USERNAMEKEY_VALUE))
            {
                usernameTextBox.Text = (string)State[USERNAMEKEY_VALUE];
            }

            if (State.ContainsKey(PASSWORDKEY_VALUE))
            {
                passwordPasswordBox.Password = (string)State[PASSWORDKEY_VALUE];
            }

            HideBlogSelectionControl();
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            //store transient data in State dictionary
            if (State.ContainsKey(URLKEY_VALUE))
            {
                State.Remove(URLKEY_VALUE);
            }
            State.Add(URLKEY_VALUE, urlTextBox.Text);
            
            if (State.ContainsKey(USERNAMEKEY_VALUE))
            {
                State.Remove(USERNAMEKEY_VALUE);
            }
            State.Add(USERNAMEKEY_VALUE, usernameTextBox.Text);

            if (State.ContainsKey(PASSWORDKEY_VALUE))
            {
                State.Remove(PASSWORDKEY_VALUE);
            }
            State.Add(PASSWORDKEY_VALUE, passwordPasswordBox.Password);
        }

        private void OnCreateNewBlogButtonClick(object sender, RoutedEventArgs e)
        {
            string uriString = string.Format("/BrowserShellPage.xaml?uri={0}", Constants.WORDPRESS_SIGNUP_URL);
            NavigationService.Navigate(new Uri(uriString, UriKind.Relative));
        }

        private void HideBlogSelectionControl()
        {
            blogSelectionControl.Visibility = Visibility.Collapsed;
            blogSelectionControl.Blogs = null;
            ContentPanel.Visibility = Visibility.Visible;
            ApplicationBar.IsVisible = true;
        }

        private void ShowBlogSelectionControl(List<Blog> items)
        {
            ApplicationBar.IsVisible = false;
            ContentPanel.Visibility = Visibility.Collapsed;
            blogSelectionControl.Visibility = Visibility.Visible;
            blogSelectionControl.Blogs = items;
        }

        private void OnBlogsSelected(object sender, RoutedEventArgs e)
        {
            blogSelectionControl.SelectedItems.ForEach(blog =>
            {
                if (!(DataService.Current.Blogs.Any(b => b.BlogId == blog.BlogId)))
                {
                    DataService.Current.Blogs.Add(blog);
                }
            });

            NavigationService.Navigate(new Uri("/BlogsPage.xaml", UriKind.Relative));
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (Visibility.Visible == blogSelectionControl.Visibility)
            {
                HideBlogSelectionControl();
                e.Cancel = true;
            }
            else
            {
                base.OnBackKeyPress(e);
            }
        }
        #endregion

    }
}