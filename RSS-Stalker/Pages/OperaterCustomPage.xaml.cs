using CoreLib.Enums;
using CoreLib.Models;
using CoreLib.Models.App;
using CoreLib.Tools;
using RSS_Stalker.Controls;
using RSS_Stalker.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace RSS_Stalker.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class OperaterCustomPage : Page
    {
        private List<GroupChannelList> AllList = new List<GroupChannelList>();
        private CustomPage _sourcePage=null;
        private ObservableCollection<GroupChannelList> SourceChannelCollection = new ObservableCollection<GroupChannelList>();
        public ObservableCollection<FilterRule> RuleCollection = new ObservableCollection<FilterRule>();
        private ObservableCollection<FilterItem> FilterCollection = new ObservableCollection<FilterItem>();
        public ObservableCollection<string> IconCollection = new ObservableCollection<string>();
        public static OperaterCustomPage Current;
        private bool _isInit = false;
        public OperaterCustomPage()
        {
            this.InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;
            Current = this;
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            MainPage.Current._isChannelAbout = true;
            AllList.Clear();
            IconCollection.Clear();
            RuleCollection.Clear();
            foreach (var item in MainPage.Current.Categories)
            {
                AllList.Add(new GroupChannelList(item.Channels) { Key = item.Name });
            }
            var iconList = AppTools.GetIcons();
            foreach (var item in iconList)
            {
                IconCollection.Add(item);
            }
            var rules = FilterRule.GetRules();
            foreach (var item in rules)
            {
                RuleCollection.Add(item);
            }
            
            if (e.Parameter != null)
            {
                if(e.Parameter is CustomPage)
                {
                    _sourcePage = e.Parameter as CustomPage;
                    IconTextBlock.Text = _sourcePage.Icon;
                    PageNameTextBox.Text = _sourcePage.Name;
                    FilterCollection.Clear();
                    foreach (var item in _sourcePage.Rules)
                    {
                        FilterCollection.Add(item);
                    }
                    foreach (var item in AllList)
                    {
                        item.RemoveAll(p => _sourcePage.Channels.Any(i => i.Id == p.Id));
                    }
                    AllList.Insert(0, new GroupChannelList(_sourcePage.Channels) { Key = AppTools.GetReswLanguage("Tip_Selected") });
                    SourceChannelCollection = new ObservableCollection<GroupChannelList>(AllList);
                }
            }
            else
            {
                IconTextBlock.Text = IconCollection.First();
                SourceChannelCollection = new ObservableCollection<GroupChannelList>(AllList);
            }
        }

        private void SourceChannelListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = SourceChannelListView.SelectedIndex;
        }

        private void IconGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var str = e.ClickedItem as string;
            IconTextBlock.Text = str;
            IconFlyout.Hide();
        }
        private void IconContainer_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            int count = SourceChannelListView.SelectedItems.Count;
            var channelList = new List<Channel>();
            foreach (var item in SourceChannelListView.SelectedItems)
            {
                channelList.Add(item as Channel);
            }
            if (channelList == null || channelList.Count == 0)
            {
                new PopupToast(AppTools.GetReswLanguage("Tip_NoChannelSelect"),AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
                return;
            }
            string icon = IconTextBlock.Text;
            string name = PageNameTextBox.Text;
            if(string.IsNullOrEmpty(icon) || string.IsNullOrEmpty(name))
            {
                new PopupToast(AppTools.GetReswLanguage("Tip_FieldEmpty"), AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
                return;
            }
            if (FilterCollection.Count > 0)
            {
                foreach (var item in FilterCollection)
                {
                    if(item.Rule.Type==FilterRuleType.Filter || item.Rule.Type == FilterRuleType.FilterOut)
                    {
                        try
                        {
                            var regex = new Regex(item.Content);
                        }
                        catch (Exception)
                        {
                            new PopupToast(AppTools.GetReswLanguage("Tip_InputRegexError"), AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
                            return;
                        }
                    }
                    else
                    {
                        var numberRegex = new Regex(@"\d+");
                        if (!numberRegex.IsMatch(item.Content))
                        {
                            new PopupToast(AppTools.GetReswLanguage("Tip_InputNumberError"), AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
                            return;
                        }
                    }
                }
            }
            if (_sourcePage == null || string.IsNullOrEmpty(_sourcePage.Id))
            {
                _sourcePage = new CustomPage();
                _sourcePage.Id = Guid.NewGuid().ToString("N");
                _sourcePage.Name = name;
                _sourcePage.Icon = icon;
                _sourcePage.Rules = FilterCollection.ToList();
                _sourcePage.Channels = channelList;
                MainPage.Current.CustomPages.Add(_sourcePage);
                MainPage.Current.PageListView.SelectedItem = _sourcePage;
                MainPage.Current.MainFrame.Navigate(typeof(CustomPageDetailPage), _sourcePage);
                await IOTools.AddPage(_sourcePage);
            }
            else
            {
                _sourcePage.Name = name;
                _sourcePage.Icon = icon;
                _sourcePage.Rules = FilterCollection.ToList();
                _sourcePage.Channels = channelList;
                foreach (var item in MainPage.Current.CustomPages)
                {
                    if (item.Id ==_sourcePage.Id)
                    {
                        item.Name =_sourcePage.Name;
                        item.Icon =_sourcePage.Icon;
                        item.Rules= FilterCollection.ToList();
                        item.Channels= channelList;
                        MainPage.Current.PageListView.SelectedItem = _sourcePage;
                        MainPage.Current.MainFrame.Navigate(typeof(CustomPageDetailPage), _sourcePage);
                        break;
                    }
                }
                await IOTools.UpdatePage(_sourcePage);
            }
        }

        private void AddRuleButton_Click(object sender, RoutedEventArgs e)
        {
            if (FilterCollection.Count < 4)
            {
                FilterCollection.Add(new FilterItem());
            }
            if(FilterCollection.Count>=4)
            {
                AddRuleButton.Visibility = Visibility.Collapsed;
            }
        }

        private void RemoveRuleButton_Click(object sender, RoutedEventArgs e)
        {
            var data = (sender as FrameworkElement).DataContext as FilterItem;
            FilterCollection.Remove(data);
            if (FilterCollection.Count <= 3)
            {
                AddRuleButton.Visibility = Visibility.Visible;
            }
        }

        private void RuleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInit)
            {
                return;
            }
            var comboBox = sender as ComboBox;
            if (comboBox.SelectedIndex == -1)
                return;
            var data = comboBox.DataContext as FilterItem;
            var temp = FilterCollection.Where(p => p.Rule.Type == data.Rule.Type);
            var collection = FilterCollection;
            if (temp.Count() > 1)
            {
                data.Rule = null;
                comboBox.SelectedIndex = -1;
                new PopupToast(AppTools.GetReswLanguage("Tip_RuleRepeat"), AppTools.GetThemeSolidColorBrush(CoreLib.Enums.ColorType.ErrorColor)).ShowPopup();
            }
        }

        private void SourceChannelListView_Loaded(object sender, RoutedEventArgs e)
        {
            if (_sourcePage != null)
            {
                SourceChannelListView.SelectRange(new ItemIndexRange(0, Convert.ToUInt32(_sourcePage.Channels.Count)));
            }
        }

        private void RuleComboBox_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _isInit = true;
        }
    }

    public class GroupChannelList : List<Channel>
    {
        public GroupChannelList(IEnumerable<Channel> items) : base(items) { }
        public object Key { get; set; }
    }
}
