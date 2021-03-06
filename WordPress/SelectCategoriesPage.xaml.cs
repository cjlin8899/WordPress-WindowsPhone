﻿using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

using WordPress.Localization;
using WordPress.Model;
using System.Diagnostics;
using System.Windows.Controls;

namespace WordPress
{
    public partial class SelectCategoriesPage : PhoneApplicationPage
    {
        #region member variables

        private const string PREV_SELECTED_CATEGORIES_KEY = "prev_selected_categories_key";
        private ApplicationBarIconButton _refreshIconButton;
        private ApplicationBarIconButton _addIconButton;
        private ApplicationBarIconButton _saveIconButton;
        private StringTable _localizedStrings;

        #endregion

        #region constructors

        public SelectCategoriesPage()
        {
            InitializeComponent();

            _localizedStrings = App.Current.Resources["StringTable"] as StringTable;

            ApplicationBar = new ApplicationBar();
            ApplicationBar.BackgroundColor = (Color)App.Current.Resources["AppbarBackgroundColor"];
            ApplicationBar.ForegroundColor = (Color)App.Current.Resources["WordPressGrey"];

            _refreshIconButton = new ApplicationBarIconButton(new Uri("/Images/appbar.refresh.png", UriKind.Relative));
            _refreshIconButton.Text = _localizedStrings.ControlsText.Refresh;
            _refreshIconButton.Click += OnRefreshButtonClick;
            ApplicationBar.Buttons.Add(_refreshIconButton);

            _addIconButton = new ApplicationBarIconButton(new Uri("/Images/appbar.add.png", UriKind.Relative));
            _addIconButton.Text = _localizedStrings.ControlsText.Add;
            _addIconButton.Click += OnAddButtonClick;
            ApplicationBar.Buttons.Add(_addIconButton);

            _saveIconButton = new ApplicationBarIconButton(new Uri("/Images/appbar.save.png", UriKind.Relative));
            _saveIconButton.Text = _localizedStrings.ControlsText.Save;
            _saveIconButton.Click += OnSaveButtonClick;
            ApplicationBar.Buttons.Add(_saveIconButton);

            Loaded += OnPageLoaded;
        }

        #endregion

        #region methods

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (State.ContainsKey(PREV_SELECTED_CATEGORIES_KEY))
            {
                string[] selectedCategories = State[PREV_SELECTED_CATEGORIES_KEY] as string[];
                categoriesListBox.SelectedItems.Clear();
                foreach (string categoryName in selectedCategories)
                {
                    foreach (Category category in App.MasterViewModel.CurrentBlog.Categories)
                    {
                        if (categoryName.Equals(category.Description))
                        {
                            //Debug.WriteLine("Match found: " + categoryName);
                            categoriesListBox.SelectedItems.Add(category);
                        }
                    }
                }
                State.Remove(PREV_SELECTED_CATEGORIES_KEY);
            }
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (e.Content is AddNewCategoryPage)
            {
                if (State.ContainsKey(PREV_SELECTED_CATEGORIES_KEY))
                {
                    State.Remove(PREV_SELECTED_CATEGORIES_KEY);
                }
                
	            string[] selectedCategories = new string[categoriesListBox.SelectedItems.Count];
                int i = 0;
                foreach (Category category in categoriesListBox.SelectedItems)
                {
                    selectedCategories[i] = category.Description;
                    i++;
                }
                State.Add(PREV_SELECTED_CATEGORIES_KEY, selectedCategories);
            }
          
            base.OnNavigatedFrom(e);
            DataService.Current.FetchComplete -= OnFetchCurrentBlogCategoriesComplete;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnPageLoaded;
            App.WaitIndicationService.RootVisualElement = LayoutRoot;
            BlogName.Text = App.MasterViewModel.CurrentBlog.BlogNameUpper;
            categoriesListBox.SelectedItems.Clear();
            this.categoriesListBox.IsSelectionEnabled = true;
            //Add a listener on the selection changes
            categoriesListBox.SelectionChanged += new SelectionChangedEventHandler(multiselectList_SelectionChanged);

            if (null == App.MasterViewModel.CurrentBlog) return;

            if (0 == DataService.Current.CurrentBlog.Categories.Count)
            {
                FetchCategories();
            }
            else
            {
                foreach (string categoryString in App.MasterViewModel.CurrentPost.Categories)
                {
                    foreach (Category category in App.MasterViewModel.CurrentBlog.Categories)
                    {
                        if (categoryString.Equals(category.Description))
                        {
                            selectItemOnPageLoaded(category);
                        }
                    }
                }
            }
        }

        private void selectItemOnPageLoaded(Category category)
        {
            MultiselectItem container = categoriesListBox.ItemContainerGenerator.ContainerFromItem(category) as MultiselectItem;
            if (null != container)
            {
                container.IsSelected = !container.IsSelected;
            }
            else 
            {
                //on long list the container could be null since the list is not fully rendered on the screen, so we can't select items at the end of the list
                categoriesListBox.SelectedItems.Add(category);
            }
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (App.WaitIndicationService.Waiting)
            {
                DataService.Current.FetchComplete -= OnFetchCurrentBlogCategoriesComplete;
                App.WaitIndicationService.HideIndicator();
                ApplicationBar.IsVisible = true;
                e.Cancel = true;
            }
            else
            {
                base.OnBackKeyPress(e);
            }
        }

        private void FetchCategories()
        {
            App.WaitIndicationService.RootVisualElement = LayoutRoot;
            ApplicationBar.IsVisible = false; 
            App.WaitIndicationService.ShowIndicator(_localizedStrings.Messages.RetrievingCategories);
            DataService.Current.FetchComplete += OnFetchCurrentBlogCategoriesComplete;
            DataService.Current.FetchCurrentBlogCategories();
        }

        private void OnFetchCurrentBlogCategoriesComplete(object sender, EventArgs e)
        {
            DataService.Current.FetchComplete -= OnFetchCurrentBlogCategoriesComplete;
            App.WaitIndicationService.HideIndicator();
            ApplicationBar.IsVisible = true; 
            //update the list
            categoriesListBox.SelectedItems.Clear();
            categoriesListBox.IsSelectionEnabled = true;
            foreach (string categoryString in App.MasterViewModel.CurrentPost.Categories)
            {
                foreach (Category category in App.MasterViewModel.CurrentBlog.Categories)
                {
                    if (categoryString.Equals(category.Description))
                    {
                        selectItemOnPageLoaded(category);
                    }
                }
            }
        }

        private void OnRefreshButtonClick(object sender, EventArgs args)
        {
            FetchCategories();
        }

        private void OnAddButtonClick(object sender, EventArgs args)
        {
            NavigationService.Navigate(new Uri("/AddNewCategoryPage.xaml", UriKind.Relative));
        }

        private void OnSaveButtonClick(object sender, EventArgs args)
        {
            Post currentPost = App.MasterViewModel.CurrentPost;
            currentPost.Categories.Clear();

            foreach (Category category in categoriesListBox.SelectedItems)
            {
                currentPost.Categories.Add(category.Description);
            }
            NavigationService.GoBack();
        }
    
        //Occurs when there is a change in the SelectedItems collection.
        private void multiselectList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // when all items are unselected the selection mode automatically turns off
            if ( categoriesListBox.SelectedItems.Count == 0 )
                categoriesListBox.IsSelectionEnabled = true;
        }

        private void CategoryListItem_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Category cat = ((FrameworkElement)sender).DataContext as Category;
            ToggleCategorySelection(cat);
        }

        private void ToggleCategorySelection(Category category)
        {
            MultiselectItem container = categoriesListBox.ItemContainerGenerator.ContainerFromItem(category) as MultiselectItem;
            if (null != container)
            {
                container.IsSelected = !container.IsSelected;
            }
        } 

        #endregion
    }
}