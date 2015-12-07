﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Windows.Web.Http;
using System.Collections.ObjectModel;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上提供

namespace Fzuhelper.Views
{

    public class ShortenMark : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string mark = (string)value;
            return mark == "成绩尚未录入" ? "尚未录入" : mark;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class Score : Page
    {
        //private StorageFolder fzuhelperDataFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("FzuhelperData");

        //private static bool initialAgain = true;

        private static bool firstTimeLoad = true;

        private string jsonData;

        private ScoreReturnValue srv;

        private GradePointReturnValue gprv;

        private static List<ScoreArr> markArr;

        private static List<GradePointArr> gradePointArr = new List<GradePointArr>();

        public static List<string> countTime = new List<string>();

        private ObservableCollection<Group> Groups;

        public Score()
        {
            this.InitializeComponent();

            IniList(false);
        }

        private async void IniList(bool IsRefresh)
        {
            refreshIndicator.IsActive = true;
            if (!IsRefresh)
            {
                try
                {
                    //Get data from storage
                    StorageFolder fzuhelperDataFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("FzuhelperData");
                    StorageFile score = await fzuhelperDataFolder.GetFileAsync("score.dat");
                    jsonData = await FileIO.ReadTextAsync(score);
                }
                catch
                {
                    if (firstTimeLoad)
                    {
                        IniList(true);
                        firstTimeLoad = false;
                        return;
                    }
                    MainPage.SendToast("获取数据出错，请刷新");
                    refreshIndicator.IsActive = false;
                    return;
                }
            }
            else
            {
                try
                {
                    jsonData = await HttpRequest.GetScore();
                    if (jsonData == "error")
                    {
                        MainPage.SendToast("获取数据出错");
                        refreshIndicator.IsActive = false;
                        return;
                    }
                }
                catch
                {
                    MainPage.SendToast("获取数据出错");
                    refreshIndicator.IsActive = false;
                    return;
                }
            }
            try
            {
                srv = JsonConvert.DeserializeObject<ScoreReturnValue>(jsonData);
                markArr = JsonConvert.DeserializeObject<List<ScoreArr>>(srv.data["markArr"].ToString());
                Groups = CreateGroups();
                cvsGroups.Source = Groups;
                listViewZoomOutView.ItemsSource = cvsGroups.View.CollectionGroups;
                GetAllGradePoint(IsRefresh);
                //Unknown error, just login again
                if (markArr.Count == 0)
                {
                    await HttpRequest.ReLogin();
                    IniList(true);
                }
            }
            catch
            {
                MainPage.SendToast("获取数据出错，请刷新");
            }
            refreshIndicator.IsActive = false;
        }

        private async void GetAllGradePoint(bool IsRefresh)
        {
            gradePointListView.ItemsSource = null;
            gradePointArr.Clear();
            string year = "", term = "";
            foreach (string item in countTime)
            {
                year = item.Substring(0, 4);
                term = item.Substring(4);
                if (IsRefresh)
                {
                    try
                    {
                        jsonData = await HttpRequest.GetGradePoint(year, term);
                    }
                    catch
                    {
                        return;
                    }
                }
                else
                {
                    try
                    {
                        //from storage
                        StorageFolder fzuhelperDataFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("FzuhelperData");
                        StorageFile gradePointFile = await fzuhelperDataFolder.GetFileAsync(year + term + ".dat");
                        jsonData = await FileIO.ReadTextAsync(gradePointFile);
                    }
                    catch
                    {
                        jsonData = await HttpRequest.GetGradePoint(year, term);
                    }
                }
                try
                {
                    gprv = JsonConvert.DeserializeObject<GradePointReturnValue>(jsonData);
                    GradePointArr gpArr = new GradePointArr();
                    gpArr.term = item;
                    gpArr.point = gprv.data["point"];
                    gpArr.rank_total = gprv.data["rank"] + "/" + gprv.data["totalStudents"];
                    gradePointArr.Add(gpArr);
                }
                catch
                {
                    if (IsRefresh)
                    {
                        MainPage.SendToast(year + term + "绩点更新失败");
                    }
                }
            }
            gradePointListView.ItemsSource = gradePointArr;
        }

        private class ScoreReturnValue
        {
            public bool status { get; set; }

            public string errMsg { get; set; }

            public PropertySet data { get; set; }
        }

        private class ScoreArr
        {
            public string majorType { get; set; }

            public string courseTime { get; set; }

            public string courseName { get; set; }

            public string credit { get; set; }

            public string sore { get; set; }

            public string gradePoint { get; set; }

            public string getPoint { get; set; }

            public string minorType { get; set; }
        }

        private class GradePointReturnValue
        {
            public bool status { get; set; }

            public Dictionary<string,string> data { get; set; }
        }

        private class GradePointArr
        {
            public string term { get; set; }

            //public string name { get; set; }

            public string point { get; set; }

            public string rank_total { get; set; }
        }

        //Define Group
        private class Group
        {
            public string Name { get; set; }

            public ObservableCollection<ScoreArr> Items
            {
                get;
                private set;
            }

            public Group()
            {
                this.Items = new ObservableCollection<ScoreArr>();
            }
        }

        private void refreshScore_Click(object sender, RoutedEventArgs e)
        {
            IniList(true);
        }

        private static ObservableCollection<Group> CreateGroups()
        {
            var groups = new ObservableCollection<Group>();

            countTime.Clear();

            var group = new Group();

            foreach (ScoreArr item in markArr)
            {
                if (countTime.Count == 0)
                {
                    countTime.Add(item.courseTime);
                    group.Name = item.courseTime;
                }
                else if (!countTime.Contains(item.courseTime))
                {
                    groups.Add(group);
                    group = new Group();
                    countTime.Add(item.courseTime);
                    group.Name = item.courseTime;
                }
                group.Items.Add(item);
                if (item.Equals(markArr.Last())){
                    groups.Add(group);
                }
            }
            return groups;
        }
    }
}
