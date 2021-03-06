﻿using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using WordPress.Commands;
using WordPress.Localization;
using WordPress.Model;
using WordPress.Utils;

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
        private GetUsersBlogsRPC rpc;
        private WebClient rsdWebClient;
        private bool useRecoveryFunctions = true;
        private bool _messageBoxIsShown = false;

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

        private void Input_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (sender == urlTextBox)
                    usernameTextBox.Focus();
                else if (sender == usernameTextBox)
                    passwordPasswordBox.Focus();
                else if (sender == passwordPasswordBox)
                {
                    AttemptToLoginAsync();
                }
            }
        }

        private void OnSaveButtonClick(object sender, EventArgs e)
        {
            AttemptToLoginAsync();
        }

        private void AttemptToLoginAsync()
        {
            this.Focus();

            if (!ValidateUrl(urlTextBox.Text))
            {
                PromptUserForInput(_localizedStrings.Prompts.MissingUrl, urlTextBox);
                return;
            }

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

            string username = usernameTextBox.Text;
            string password = passwordPasswordBox.Password;
                             
            string url = urlTextBox.Text.Trim();
            if (url.EndsWith("/")) url = url.Substring(0, url.Length - 1); //remove the trailing slash
            if (url.EndsWith("/wp-admin")) url = url.Replace("/wp-admin", "");
            if (!url.EndsWith("/xmlrpc.php")) url = url + "/xmlrpc.php";

            this.DebugLog("XML-RPC URL: " + url);

            if (!ValidateUrl(url))
            {
                PromptUserForInput(_localizedStrings.Prompts.MissingUrl, urlTextBox);
                return;
            }

            rpc = new GetUsersBlogsRPC(url, username, password);
            rpc.Completed += OnGetUsersBlogsCompleted;
            rpc.ExecuteAsync();

            useRecoveryFunctions = true;
            ApplicationBar.IsVisible = false; //hide the application bar 
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

        private bool ValidateUrl(String url)
        {
            Uri testUri;
            bool result = Uri.TryCreate(url, UriKind.Absolute, out testUri);
            return result;            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="textbox"></param>
        private void PromptUserForInput(string message, Control control)
        {
            if (_messageBoxIsShown)
                return;
            _messageBoxIsShown = true;
            MessageBox.Show(message);
            _messageBoxIsShown = false;
            control.Focus();
        }

        private void OnGetUsersBlogsCompleted(object sender, XMLRPCCompletedEventArgs<Blog> args)
        {
            GetUsersBlogsRPC rpc = sender as GetUsersBlogsRPC;
            rpc.Completed -= OnGetUsersBlogsCompleted;

            if (args.Cancelled)
            {
            } 
            else if (null == args.Error)
            {
                App.WaitIndicationService.KillSpinner();
                ApplicationBar.IsVisible = true;

                if (0 == args.Items.Count)
                {
                    this.HandleException(null, _localizedStrings.PageTitles.CheckTheUrl, string.Format(_localizedStrings.Messages.NoBlogsFoundAtThisURL, rpc.Url));
                }
                else if (1 == args.Items.Count)
                {
                    if (!(DataService.Current.Blogs.Any(b => b.Xmlrpc == args.Items[0].Xmlrpc)))
                    {
                        DataService.Current.AddBlogToStore(args.Items[0]);
                        PushNotificationsHelper.Instance.sendBlogsList();
                    }
                    NavigationService.Navigate(new Uri("/BlogsPage.xaml", UriKind.Relative));
                }
                else
                {
                    ShowBlogSelectionControl(args.Items);
                }
            }
            else
            {

                if (useRecoveryFunctions && !(args.Error is WordPress.Model.NoConnectionException) && !(args.Error is XmlRPCException))//do not use the recovery function if the connection is not available
                {
                    useRecoveryFunctions = false; //set this to false, since the recovery functions will be used only once.
                    startRecoveryfunctions();
                }
                else
                {
                    App.WaitIndicationService.KillSpinner();
                    ApplicationBar.IsVisible = true;
                    Exception currentException = args.Error;
                    if (currentException is NotSupportedException || currentException is XmlRPCParserException || currentException is ArgumentNullException)
                    {
                        this.HandleException(currentException, _localizedStrings.PageTitles.CheckTheUrl, _localizedStrings.Messages.CheckTheUrl);
                        UIThread.Invoke(() =>
                        {
                            urlTextBox.Focus();
                        });
                        return;
                    }
                    else if (currentException is XmlRPCException && (currentException as XmlRPCException).FaultCode == 403) //username or password error
                    {
                        UIThread.Invoke(() =>
                        {
                            MessageBox.Show(_localizedStrings.Prompts.UsernameOrPasswordError);
                        });
                    }
                    else
                        this.HandleException(args.Error);
                }
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
            LaunchWebBrowserCommand command = new LaunchWebBrowserCommand();
            command.Execute(Constants.WORDPRESS_SIGNUP_URL);
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
                if (!(DataService.Current.Blogs.Any(b => b.Xmlrpc == blog.Xmlrpc)))
                {
                    DataService.Current.AddBlogToStore(blog);
                }
            });
            PushNotificationsHelper.Instance.sendBlogsList();
            NavigationService.Navigate(new Uri("/BlogsPage.xaml", UriKind.Relative));
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if(App.WaitIndicationService.Waiting){
                if (rpc != null)
                {
                    rpc.Completed -= OnGetUsersBlogsCompleted;
                }
                if (rsdWebClient != null)
                {
                    rsdWebClient.DownloadStringCompleted -= downloadRSDdocumentCompleted;
                    rsdWebClient.DownloadStringCompleted -= downloadUserURLContentCompleted;
                }
                App.WaitIndicationService.KillSpinner();
                HideBlogSelectionControl();
                e.Cancel = true;
            }

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

        #region Discover Endpoint methods

        private void startRecoveryfunctions()
        {
            string url = urlTextBox.Text.Trim();
            rsdWebClient = new WebClient();
            try
            {
                rsdWebClient.DownloadStringAsync(new Uri(url));
            }
            catch (Exception)
            {
                this.showErrorMsgOnRecovery(null);
                return;
            }
            rsdWebClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(downloadUserURLContentCompleted);
        }

        void downloadUserURLContentCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            rsdWebClient.DownloadStringCompleted -= downloadUserURLContentCompleted;
            String rsdURL = null;
            if (e.Error == null)
            {
                try
                {
                    string s = e.Result;
                    //parse the HTML document and find the RSD endpoint here
                    string pattern = "<link\\s*?rel=\"EditURI\"\\s*?type=\"application/rsd\\+xml\"\\s*?title=\"RSD\"\\s*?href=\"(.*?)\"";
                    Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
                    if (regex.IsMatch(s))
                    {
                        Match firstMatch = regex.Match(s);
                        s = s.Substring(firstMatch.Index, firstMatch.Length);
                        s = s.Substring(s.IndexOf("href=\""));
                        s = s.Replace("href=\"", "");
                        s = s.Substring(0, s.IndexOf("\""));

                        if (ValidateUrl(s))
                            rsdURL = s;
                    }
                }
                catch (Exception) { }
            }

            if (rsdURL != null)
                this.downloadRSDdocument(rsdURL);
            else
                this.showErrorMsgOnRecovery(null); //No match or error. Show "no WP site found at this URL..."
        }

        private void downloadRSDdocument(string rsdURL)
        {
            rsdWebClient = new WebClient();
            try
            {
                rsdWebClient.DownloadStringAsync(new Uri(rsdURL));
            }
            catch (Exception)
            {
                this.showErrorMsgOnRecovery(null);
                return;
            }
            rsdWebClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(downloadRSDdocumentCompleted);
        }

        void downloadRSDdocumentCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            rsdWebClient.DownloadStringCompleted -= downloadRSDdocumentCompleted;

            string apiLink = null;
            
            if (e.Error == null)
            {
                try
                {
                    string rsdDocumentString = e.Result;
                    //clean the junk b4 the xml preamble.
                    if (!rsdDocumentString.StartsWith("<?xml"))
                    {
                        //clean the junk b4 the xml preamble
                        this.DebugLog("cleaning the junk before the xml preamble");
                        int indexOfFirstLt = rsdDocumentString.IndexOf("<?xml");
                        if (indexOfFirstLt > -1)
                            rsdDocumentString = rsdDocumentString.Substring(indexOfFirstLt);
                    }
                    
                    try
                    {
                        XDocument xDoc = XDocument.Parse(rsdDocumentString, LoadOptions.None);
                        foreach (XElement apiElement in xDoc.Descendants())
                        {
                            if (apiElement.Name.LocalName == "api")
                                if (apiElement.Attribute("name").Value == "WordPress")
                                {
                                    apiLink = apiElement.Attribute("apiLink").Value;
                                }
                        }
                    }
                    catch (Exception) { }

                    if (apiLink == null || !ValidateUrl(apiLink))
                    {
                        apiLink = null;
                        //try to use RegExp
                        String pattern = @"<api\s*?name=\""WordPress\"".*?apiLink=\""(.*?)\""";
                        Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
                        if (regex.IsMatch(rsdDocumentString))
                        {
                            Match firstMatch = regex.Match(rsdDocumentString);
                            rsdDocumentString = rsdDocumentString.Substring(firstMatch.Index, firstMatch.Length);
                            rsdDocumentString = rsdDocumentString.Substring(rsdDocumentString.IndexOf("apiLink=\""));
                            rsdDocumentString = rsdDocumentString.Replace("apiLink=\"", "");
                            rsdDocumentString = rsdDocumentString.Substring(0, rsdDocumentString.IndexOf("\""));

                            if (ValidateUrl(rsdDocumentString))
                                apiLink = rsdDocumentString;
                        }
                    }
                }
                catch (Exception) { }
            }

            if (apiLink != null)
            {
                //restart getUserBlogs with this URL
                string username = usernameTextBox.Text;
                string password = passwordPasswordBox.Password;
                rpc = new GetUsersBlogsRPC(apiLink, username, password);
                rpc.Completed += OnGetUsersBlogsCompleted;
                rpc.ExecuteAsync();
            }
            else
                this.showErrorMsgOnRecovery(null); //No match or error. Show "no WP site found at this URL..."
        }

        private void showErrorMsgOnRecovery(Exception ex)
        {
            if (_messageBoxIsShown)
                return;
            App.WaitIndicationService.KillSpinner();
            ApplicationBar.IsVisible = true;

            if (null == ex)
            {
                this.HandleException(null, _localizedStrings.PageTitles.CheckTheUrl, _localizedStrings.Messages.CheckTheUrl);
            }
            else
            {
                this.HandleException(ex);
            }
            return;
        }

        #endregion


    }
}