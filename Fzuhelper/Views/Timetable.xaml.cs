﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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
using Windows.Web.Http;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上提供

namespace Fzuhelper.Views
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class Timetable : Page
    {
        //private StorageFolder fzuhelperDataFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("FzuhelperData");

        private static bool initialAgain = true;

        private string jsonData;

        private TimetableReturnValue ttrv;

        private static List<TableInfoArr> tableInfo;

        private string currentWeek;

        private static List<List<SingleDayCourseArr>> singleDayCourseList = new List<List<SingleDayCourseArr>>();

        private static WeekToInt weekToInt = new WeekToInt();

        public Timetable()
        {
            this.InitializeComponent();
            IniList();
        }

        private async void IniList()
        {
            try
            {
                refreshIndicator.IsActive = true;
                //Get data from storage
                StorageFolder fzuhelperDataFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("FzuhelperData");
                StorageFile timetable = await fzuhelperDataFolder.GetFileAsync("timetable.dat");
                jsonData = await FileIO.ReadTextAsync(timetable);
                //System.Diagnostics.Debug.WriteLine(jsonData);
                ttrv = JsonConvert.DeserializeObject<TimetableReturnValue>(jsonData);
                //System.Diagnostics.Debug.WriteLine(srv.data["markArr"]);

                //A list of every week courses ,and the first item is the current week courses
                tableInfo = JsonConvert.DeserializeObject<List<TableInfoArr>>(ttrv.data["tableInfo"].ToString());
                currentWeek = tableInfo[0].week;
                ShowOneWeekCourse(0);
                IniWeekOption();
                initialAgain = true;
                refreshIndicator.IsActive = false;
            }
            catch
            {
                refreshIndicator.IsActive = false;
                if (initialAgain)
                {
                    refreshIndicator.IsActive = true;
                    initialAgain = !initialAgain;
                    await HttpRequest.GetTimetable();
                    IniList();
                }
                else
                {
                    //MainPage.SendToast("无法获取列表");
                }
                return;
            }
        }

        private void ShowOneWeekCourse(int week)
        {
            //System.Diagnostics.Debug.WriteLine(timeTableMain.Children.Count);
            //Clear timetable
            while (timeTableMain.Children.Count != 18)
            {
                timeTableMain.Children.Remove(timeTableMain.Children.Last());
            }
            //Clear single day course arr list
            singleDayCourseList.Clear();

            List<DayCourseArr> dayCourseList = new List<DayCourseArr>();

            DayCourseArr dayCourse = new DayCourseArr();

            for (int i = 1; i <= 7; i++)
            {
                for(int jie = 1; jie <= 11; jie++)
                {
                    dayCourse = JsonConvert.DeserializeObject<DayCourseArr>(tableInfo[week].courseArr[i.ToString()][jie.ToString()].ToString());
                    dayCourseList.Add(dayCourse);
                }
            }

            for(int i = 1; i <= 7; i++)
            {
                //Initialize single day course arr list
                singleDayCourseList.Add(new List<SingleDayCourseArr>());
                //System.Diagnostics.Debug.WriteLine(tableInfo[week].courseArr[i.ToString()]["1"]);
                //i represents the column index that course should be placed and jie represents the row index
                for (int jie = 1; jie <= 11; jie++)
                {
                    DayCourseArr currentCourse = dayCourseList[(i - 1) * 11 + jie - 1];
                    //If the course is empty
                    if (currentCourse.courseName == "")
                    {
                        continue;
                    }
                    //Check rowspan
                    int rowspans = 0;
                    while (true)
                    {
                        //System.Diagnostics.Debug.WriteLine(rowspans);
                        if(currentCourse.courseName== dayCourseList[(i - 1) * 11 + jie + rowspans].courseName&& currentCourse.place == dayCourseList[(i - 1) * 11 + jie + rowspans].place&& currentCourse.teacherName == dayCourseList[(i - 1) * 11 + jie + rowspans].teacherName&& currentCourse.betweenWeek == dayCourseList[(i - 1) * 11 + jie + rowspans].betweenWeek)
                        {
                            if(( ((i - 1) * 11) < ((i - 1) * 11 + jie + rowspans) && ((i - 1) * 11 + jie + rowspans) <= ((i - 1) * 11 + 10)))
                            {
                                rowspans++;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    //jieDuration = jie ~ jie+rowspans
                    string jieDur = jie.ToString() + "-" + (jie + rowspans).ToString();
                    singleDayCourseList[i - 1].Add(new SingleDayCourseArr(currentCourse.courseName, currentCourse.place, currentCourse.teacherName, currentCourse.betweenWeek+"周", jieDur));
                    //Create stackpanel and visible textblocks
                    StackPanel stackPanel = new StackPanel() { Orientation=Orientation.Vertical};
                    TextBlock textBlock1 = new TextBlock() {Text = currentCourse.courseName , FontSize=12 , HorizontalAlignment=HorizontalAlignment.Stretch , TextWrapping=TextWrapping.WrapWholeWords};
                    TextBlock textBlock2 = new TextBlock() {Text = currentCourse.place , FontSize = 12, HorizontalAlignment = HorizontalAlignment.Stretch , TextWrapping = TextWrapping.WrapWholeWords};
                    //Invisible textblocks
                    TextBlock textBlock3 = new TextBlock() {Text = currentCourse.teacherName , Visibility = Visibility.Collapsed};
                    TextBlock textBlock4 = new TextBlock() {Text = currentCourse.betweenWeek, Visibility = Visibility.Collapsed };
                    TextBlock textBlock5 = new TextBlock() {Text = jieDur, Visibility = Visibility.Collapsed };

                    //Add elements to stackpanel
                    stackPanel.Children.Add(textBlock1);
                    stackPanel.Children.Add(textBlock2);
                    stackPanel.Children.Add(textBlock3);
                    stackPanel.Children.Add(textBlock4);
                    stackPanel.Children.Add(textBlock5);

                    //Create button
                    Button singleCourse = new Button() {  Margin=new Thickness(2) , Padding=new Thickness(2) , HorizontalAlignment=HorizontalAlignment.Stretch , VerticalAlignment=VerticalAlignment.Stretch };
                    singleCourse.Content = stackPanel;
                    singleCourse.Flyout = AddCourseFlyout(stackPanel);
                    singleCourse.Click += SingleCourse_Click;
                    Grid.SetColumn(singleCourse, i);
                    Grid.SetRow(singleCourse, jie);
                    if (rowspans != 0)
                    {
                        Grid.SetRowSpan(singleCourse, rowspans + 1);
                    }
                    timeTableMain.Children.Add(singleCourse);
                    jie += rowspans;
                }
            }
            //Set selected week
            selectedWeek.Text = tableInfo[week].week;
            if (tableInfo[week].week.Equals(tableInfo[0].week))
            {
                selectedWeekBtn.Label = "(本周)";
            }
            else
            {
                selectedWeekBtn.Label = "(非本周)";
            }
            //Set single day course data binding
            MonCourse.ItemsSource = singleDayCourseList[0];
            TueCourse.ItemsSource = singleDayCourseList[1];
            WedCourse.ItemsSource = singleDayCourseList[2];
            ThuCourse.ItemsSource = singleDayCourseList[3];
            FriCourse.ItemsSource = singleDayCourseList[4];
            SatCourse.ItemsSource = singleDayCourseList[5];
            SunCourse.ItemsSource = singleDayCourseList[6];
            //Display current day
            int currentDayIndex = weekToInt.w2i[DateTime.Now.DayOfWeek.ToString()];
            singleDayCourseView.SelectedItem = singleDayCourseView.Items.ElementAt(currentDayIndex);
            //Check empty
            foreach(List<SingleDayCourseArr> item in singleDayCourseList)
            {
                if (item.Count == 0)
                {
                    TextBlock tb = new TextBlock() { Text = "没课啦", FontSize = 24, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                    ListViewItem lvi = new ListViewItem();
                    lvi.Content = tb;
                    switch (singleDayCourseList.IndexOf(item))
                    {
                        case 0:
                            MonCourse.ItemsSource = null;
                            MonCourse.Items.Clear();
                            MonCourse.Items.Add(lvi);
                            break;
                        case 1:
                            TueCourse.ItemsSource = null;
                            TueCourse.Items.Clear();
                            TueCourse.Items.Add(lvi);
                            break;
                        case 2:
                            WedCourse.ItemsSource = null;
                            WedCourse.Items.Clear();
                            WedCourse.Items.Add(lvi);
                            break;
                        case 3:
                            ThuCourse.ItemsSource = null;
                            ThuCourse.Items.Clear();
                            ThuCourse.Items.Add(lvi);
                            break;
                        case 4:
                            FriCourse.ItemsSource = null;
                            FriCourse.Items.Clear();
                            FriCourse.Items.Add(lvi);
                            break;
                        case 5:
                            SatCourse.ItemsSource = null;
                            SatCourse.Items.Clear();
                            SatCourse.Items.Add(lvi);
                            break;
                        case 6:
                            SunCourse.ItemsSource = null;
                            SunCourse.Items.Clear();
                            SunCourse.Items.Add(lvi);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private Flyout AddCourseFlyout(StackPanel sp)
        {
            TextBlock t1 = (TextBlock)sp.Children.ElementAt(0);//courseName
            TextBlock t2 = (TextBlock)sp.Children.ElementAt(1);//place
            TextBlock t3 = (TextBlock)sp.Children.ElementAt(2);//teacherName
            TextBlock t4 = (TextBlock)sp.Children.ElementAt(3);//betweenWeek
            TextBlock t5 = (TextBlock)sp.Children.ElementAt(4);//jie
            //Create main stackpanel
            StackPanel spMain = new StackPanel() { Padding = new Thickness(5), Margin = new Thickness(10), HorizontalAlignment = HorizontalAlignment.Stretch, Orientation = Orientation.Horizontal };
            //Create left and right stackpanel
            StackPanel spLeft = new StackPanel() { Margin = new Thickness(0,0,20,0), HorizontalAlignment = HorizontalAlignment.Stretch, Orientation = Orientation.Vertical };
            StackPanel spRight = new StackPanel() { HorizontalAlignment = HorizontalAlignment.Stretch, Orientation = Orientation.Vertical };
            //Create new textblocks
            //indicators
            TextBlock tbi1 = new TextBlock() { Text = "课程", Margin = new Thickness(0, 5, 0, 5) };
            TextBlock tbi2 = new TextBlock() { Text = "教室", Margin = new Thickness(0, 5, 0, 5) };
            TextBlock tbi3 = new TextBlock() { Text = "老师", Margin = new Thickness(0, 5, 0, 5) };
            TextBlock tbi4 = new TextBlock() { Text = "周数", Margin = new Thickness(0, 5, 0, 5) };
            TextBlock tbi5 = new TextBlock() { Text = "节数", Margin = new Thickness(0, 5, 0, 5) };
            //values
            TextBlock tb1 = new TextBlock() { Text = t1.Text, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 5, 0, 5) };
            TextBlock tb2 = new TextBlock() { Text = t2.Text, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 5, 0, 5) };
            TextBlock tb3 = new TextBlock() { Text = t3.Text, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 5, 0, 5) };
            TextBlock tb4 = new TextBlock() { Text = t4.Text, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 5, 0, 5) };
            TextBlock tb5 = new TextBlock() { Text = t5.Text, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 5, 0, 5) };
            //Add textblocks to stackpanels
            spLeft.Children.Add(tbi1);
            spLeft.Children.Add(tbi2);
            spLeft.Children.Add(tbi3);
            spLeft.Children.Add(tbi4);
            spLeft.Children.Add(tbi5);
            spRight.Children.Add(tb1);
            spRight.Children.Add(tb2);
            spRight.Children.Add(tb3);
            spRight.Children.Add(tb4);
            spRight.Children.Add(tb5);
            spMain.Children.Add(spLeft);
            spMain.Children.Add(spRight);
            Flyout flyout = new Flyout();
            flyout.Content = spMain;
            //FlyoutBase.SetAttachedFlyout(sp, flyout);
            return flyout;
        }

        private void selectedWeekBtn_Click(object sender, RoutedEventArgs e)
        {
            topbar.IsOpen = !topbar.IsOpen;
        }

        private void SingleDayCourseToggle_Click(object sender, RoutedEventArgs e)
        {
            AppBarToggleButton sdct = (AppBarToggleButton)sender;
            if ((bool)sdct.IsChecked)
            {
                timeTableView.Visibility = Visibility.Collapsed;
                singleDayCourseView.Visibility = Visibility.Visible;
            }
            else
            {
                timeTableView.Visibility = Visibility.Visible;
                singleDayCourseView.Visibility = Visibility.Collapsed;
            }
        }

        private async void refreshTimetable_Click(object sender, RoutedEventArgs e)
        {
            refreshIndicator.IsActive = true;
            await HttpRequest.GetTimetable();
            refreshIndicator.IsActive = false;
            IniList();
        }

        private void SingleCourse_Click(object sender, RoutedEventArgs e)
        {
            Button singleCourseBtn = (Button)sender;
        }

        private void IniWeekOption()
        {
            topbar.SecondaryCommands.Clear();
            foreach (TableInfoArr t in tableInfo)
            {
                AppBarButton weekOption = new AppBarButton();
                weekOption.Label = t.week;
                if (t.week.Equals(tableInfo[0].week))
                {
                    weekOption.Label += "(本周)";
                }
                weekOption.Click += Week_Switch;
                topbar.SecondaryCommands.Add(weekOption);
            }
        }

        private void Week_Switch(object sender, RoutedEventArgs e)
        {
            AppBarButton selectedWeek = (AppBarButton)sender;
            ShowOneWeekCourse(topbar.SecondaryCommands.IndexOf(selectedWeek));
        }

        private class TimetableReturnValue
        {
            public bool status { get; set; }

            public string errMsg { get; set; }

            public PropertySet data { get; set; }
        }


        private class TableInfoArr
        {
            public string week { get; set; }

            public Dictionary<string,PropertySet> courseArr { get; set; }
        }

        public class DayCourseArr
        {
            public string courseName { get; set; }

            public string place { get; set; }

            public string teacherName { get; set; }

            public string betweenWeek { get; set; }

            //public string jie { get; set; }
        }

        private class SingleDayCourseArr : DayCourseArr
        {
            public string jieDuration { get; set; }

            public SingleDayCourseArr() { }

            public SingleDayCourseArr(string courseName,string place,string teacherName,string betweenWeek,string jieDuration)
            {
                this.courseName = courseName;
                this.place = place;
                this.teacherName = teacherName;
                this.betweenWeek = betweenWeek;
                this.jieDuration = jieDuration;
            }
        }

        private class WeekToInt
        {
            public Dictionary<string, int> w2i = new Dictionary<string, int>();

            public WeekToInt()
            {
                w2i.Add("Monday", 0);
                w2i.Add("Tuesday", 1);
                w2i.Add("Wednesday", 2);
                w2i.Add("Thursday", 3);
                w2i.Add("Friday", 4);
                w2i.Add("Saturday", 5);
                w2i.Add("Sunday", 6);
            }
        }
    }
}
