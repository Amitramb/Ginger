#region License
/*
Copyright © 2014-2018 European Support Limited

Licensed under the Apache License, Version 2.0 (the "License")
you may not use this file except in compliance with the License.
You may obtain a copy of the License at 

http://www.apache.org/licenses/LICENSE-2.0 

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS, 
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
See the License for the specific language governing permissions and 
limitations under the License. 
*/
#endregion

using Amdocs.Ginger.Common;
using Ginger.Activities;
using Ginger.BusinessFlowWindows;
using GingerWPF.DragDropLib;
using Ginger.UserControls;
using Ginger.Variables;
using GingerCore;
using GingerCore.Activities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Amdocs.Ginger.Common.Enums;

namespace Ginger.BusinessFlowFolder
{
    /// <summary>
    /// Interaction logic for BusinessFlowPage.xaml
    /// </summary>
    public partial class ActivitiesPage : Page
    {
        BusinessFlow mBusinessFlow;

        public ActivitiesPage(BusinessFlow businessFlow = null, General.RepositoryItemPageViewMode editMode = General.RepositoryItemPageViewMode.SharedReposiotry)
        {
            InitializeComponent();

            if (businessFlow != null)
            {
                mBusinessFlow = businessFlow;
                mBusinessFlow.PropertyChanged += BusinessFlow_PropertyChanged;
            }
            else
            {
                mBusinessFlow = App.BusinessFlow;
                mBusinessFlow.PropertyChanged += BusinessFlow_PropertyChanged;
                App.PropertyChanged += AppPropertychanged;
                grdActivities.AddFloatingImageButton("@ContinueFlow_16x16.png", "Continue Run Activity", App.MainWindow.FloatingContinueRunActivityButton_Click, 4);
                grdActivities.AddFloatingImageButton("@RunAction_20x20.png", "Run Selected Action", App.MainWindow.RunActionButton_Click, 4);
                grdActivities.AddFloatingImageButton("@Run2_20x20.png", "Run " + GingerDicser.GetTermResValue(eTermResKey.Activity), RunFloatingButtonClicked, 4); 
            }
          
            SetActivitiesGridView();
            RefreshActivitiesGrid();
            SetGridRowStyle();
            if (editMode == General.RepositoryItemPageViewMode.View)
            {
                grdActivities.ShowToolsBar = Visibility.Collapsed;
                grdActivities.ToolsTray.Visibility = Visibility.Collapsed;
                grdActivities.RowDoubleClick -= grdActivities_grdMain_MouseDoubleClick;
                grdActivities.DisableGridColoumns();
            }
        }

        private void SetGridRowStyle()
        {
            Style st = grdActivities.grdMain.RowHeaderStyle;
            DataTrigger DT = new DataTrigger();
            PropertyPath PT = new PropertyPath(Activity.Fields.IsNotGherkinOptimizedActivity);
            DT.Binding = new Binding() { Path = PT };
            DT.Value = false;
            DT.Setters.Add(new Setter(DataGridRow.BackgroundProperty, Brushes.Orange));
            st.Triggers.Add(DT);

            Style st2 = grdActivities.grdMain.RowHeaderStyle;
            DataTrigger DT2 = new DataTrigger();
            PropertyPath PT2 = new PropertyPath(Activity.Fields.AGSelected);
            DT2.Binding = new Binding() { Path = PT2 };
            DT2.Value = true;
            DT2.Setters.Add(new Setter(DataGridRow.BackgroundProperty, Brushes.LightBlue));
            st2.Triggers.Add(DT2);
        }

        private void MarkUnMarkAllActivities(bool ActiveStatus)
        {
            if (mBusinessFlow.Activities.Count <= 0) return;
            if (mBusinessFlow.Activities.Count > 0)
            {
                foreach (Activity a in mBusinessFlow.Activities)
                    a.Active = ActiveStatus;
            }
        }

        private void AppPropertychanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "BusinessFlow")
            {
                if (App.BusinessFlow != mBusinessFlow)
                {
                    mBusinessFlow = App.BusinessFlow;
                    if (mBusinessFlow != null)
                        mBusinessFlow.PropertyChanged += BusinessFlow_PropertyChanged;
                }
                RefreshActivitiesGrid();
            }
        }

        private void BusinessFlow_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Activities")
            {
                RefreshActivitiesGrid();
            }
        }

        private void RunFloatingButtonClicked(object sender, RoutedEventArgs e)
        {
            App.AutomateTabGingerRunner.ExecutionLogger.Configuration.ExecutionLoggerAutomationTabContext = Ginger.Reports.ExecutionLoggerConfiguration.AutomationTabContext.ActivityRun;
            App.MainWindow.RunActivity(); 
        }

        private void CurrentActivity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "HandlerType")
                grdActivities.setDefaultView();
        }      

        private void grdActivities_PreviewDragItem(object sender, EventArgs e)
        {
            if (DragDrop2.DragInfo.DataIsAssignableToType(typeof(Activity))
                 || DragDrop2.DragInfo.DataIsAssignableToType(typeof(ActivitiesGroup)))
            {
                // OK to drop                         
                DragDrop2.DragInfo.DragIcon = GingerWPF.DragDropLib.DragInfo.eDragIcon.Copy;
            }            
        }

        private void grdActivities_ItemDropped(object sender, EventArgs e)
        {
            object droppedItem= ((DragInfo)sender).Data;
            if (droppedItem.GetType() == typeof(Activity))
            {
                Activity instance = (Activity)((Activity)droppedItem).CreateInstance(true);
                instance.Active = true;
                
                mBusinessFlow.SetActivityTargetApplication(instance);
                mBusinessFlow.AddActivity(instance);
                
                //Placing the item after current item
                int selectedActIndex = -1;
                ObservableList<Activity> actsList = mBusinessFlow.Activities;
                if (actsList.CurrentItem != null)
                {
                    selectedActIndex = actsList.IndexOf((Activity)actsList.CurrentItem);
                }
                if (selectedActIndex >= 0)
                {
                    actsList.Move(actsList.Count - 1, selectedActIndex + 1);
                }            
            }                
            else if (droppedItem.GetType() == typeof(ActivitiesGroup))
            {
                ActivitiesGroup droppedGroupIns = (ActivitiesGroup)((ActivitiesGroup)droppedItem).CreateInstance(true);
                mBusinessFlow.AddActivitiesGroup(droppedGroupIns);              
                
                mBusinessFlow.ImportActivitiesGroupActivitiesFromRepository(droppedGroupIns, App.LocalRepository.GetSolutionRepoActivities(), false);
                mBusinessFlow.AttachActivitiesGroupsAndActivities();

                int selectedActIndex = -1;
                ObservableList<ActivitiesGroup> actsList = App.BusinessFlow.ActivitiesGroups;
                if (actsList.CurrentItem != null)
                {
                    selectedActIndex = actsList.IndexOf((ActivitiesGroup)actsList.CurrentItem);
                }
                if (selectedActIndex >= 0)
                {
                    actsList.Move(actsList.Count - 1, selectedActIndex + 1);
                }
            }
        }
        
        private void AddErrorHandler(object sender, RoutedEventArgs e)
        {
            mBusinessFlow.Activities.Add(new ErrorHandler() { ActivityName = "Error & Popup Handler " + mBusinessFlow.Activities.Where(a=>a.GetType()==typeof(ErrorHandler)).Count() });
        }

        private void grdActivities_RowChangedEvent(object sender, EventArgs e)
        {
            if (mBusinessFlow != null)
            {
                mBusinessFlow.CurrentActivity = (Activity)grdActivities.CurrentItem;
                if (mBusinessFlow.CurrentActivity != null)
                    mBusinessFlow.CurrentActivity.PropertyChanged += CurrentActivity_PropertyChanged;
            }
        }

        private void RefreshActivitiesGridHandler(object sender, RoutedEventArgs e)
        {
            RefreshActivitiesGrid();
        }
             
        private void SetActivitiesGridView()
        {
            //Columns View
            //# Default View
            GridViewDef defView = new GridViewDef(GridViewDef.DefaultViewName);
            defView.GridColsView = new ObservableList<GridColView>();
            defView.GridColsView.Add(new GridColView() { Field = Activity.Fields.Image, Header = " ", StyleType = GridColView.eGridColStyleType.Image, WidthWeight = 2.5, MaxWidth = 20 });
            defView.GridColsView.Add(new GridColView() { Field = RepositoryItem.Fields.SharedRepoInstanceImage, Header = "S.R.", StyleType = GridColView.eGridColStyleType.Image, WidthWeight = 2.5, MaxWidth = 20 });
            defView.GridColsView.Add(new GridColView() { Field = Activity.Fields.Active, WidthWeight = 2.5, MaxWidth = 50, StyleType = GridColView.eGridColStyleType.Template, CellTemplate = (DataTemplate)this.mainGrdActivities.Resources["FieldActive"] });
            defView.GridColsView.Add(new GridColView() { Field = Activity.Fields.Mandatory, Header="Mand.", WidthWeight =3.0, MaxWidth = 50, StyleType = GridColView.eGridColStyleType.CheckBox });
            defView.GridColsView.Add(new GridColView() { Field = Activity.Fields.ActivityName, WidthWeight = 15, Header = "Name", StyleType = GridColView.eGridColStyleType.Template, CellTemplate = (DataTemplate)this.mainGrdActivities.Resources["FieldName"] });
            defView.GridColsView.Add(new GridColView() { Field = Activity.Fields.Description, WidthWeight = 10 });
            defView.GridColsView.Add(new GridColView() { Field = Activity.Fields.TargetApplication, WidthWeight = 7.5, Header = "T. Application" });
            defView.GridColsView.Add(new GridColView() { Field = Activity.Fields.ActivitiesGroupID, Header = GingerDicser.GetTermResValue(eTermResKey.ActivitiesGroup), WidthWeight = 7.5, ReadOnly = true });
            defView.GridColsView.Add(new GridColView() { Field = Activity.Fields.VariablesNames, Header = GingerDicser.GetTermResValue(eTermResKey.Variables), WidthWeight = 7.5, BindingMode = BindingMode.OneWay });           
            List<string> automationStatusList = GingerCore.General.GetEnumValues(typeof(Activity.eActivityAutomationStatus));
            defView.GridColsView.Add(new GridColView() { Field = Activity.Fields.AutomationStatus, WidthWeight = 6, Header="Auto. Status", StyleType = GridColView.eGridColStyleType.ComboBox, CellValuesList = automationStatusList });
            List<GingerCore.General.ComboEnumItem> runOptionList = GingerCore.General.GetEnumValuesForCombo(typeof(Activity.eActionRunOption));
            defView.GridColsView.Add(new GridColView() { Field = Activity.Fields.ActionRunOption, WidthWeight = 10, Header = "Actions Run Option", StyleType = GridColView.eGridColStyleType.ComboBox, CellValuesList = runOptionList });
            defView.GridColsView.Add(new GridColView() { Field = Activity.Fields.Status, WidthWeight = 6, Header="Run Status", BindingMode = BindingMode.OneWay, PropertyConverter = (new ColumnPropertyConverter(new ActivityStatusConverter(), TextBlock.ForegroundProperty)) });                       
            defView.GridColsView.Add(new GridColView() { Field = Activity.Fields.ElapsedSecs, WidthWeight = 6, Header="Elapsed", BindingMode = BindingMode.OneWay, HorizontalAlignment = System.Windows.HorizontalAlignment.Right });                        
            grdActivities.SetAllColumnsDefaultView(defView);

            //# Custome Views
            GridViewDef desView = new GridViewDef(eAutomatePageViewStyles.Design.ToString());
            desView.GridColsView = new ObservableList<GridColView>();
            desView.GridColsView.Add(new GridColView() { Field = Activity.Fields.Status, Visible = false });
            desView.GridColsView.Add(new GridColView() { Field = Activity.Fields.ElapsedSecs, Visible = false });            
            grdActivities.AddCustomView(desView);

            GridViewDef execView = new GridViewDef(eAutomatePageViewStyles.Execution.ToString());
            execView.GridColsView = new ObservableList<GridColView>();
            execView.GridColsView.Add(new GridColView() { Field = Activity.Fields.VariablesNames, Visible = false });           
            execView.GridColsView.Add(new GridColView() { Field = Activity.Fields.AutomationStatus, Visible = false });
            grdActivities.AddCustomView(execView);
            
            grdActivities.InitViewItems();

            //Tool bar
            grdActivities.btnMarkAll.Visibility = Visibility.Visible;
            grdActivities.btnEdit.AddHandler(Button.ClickEvent, new RoutedEventHandler(EditActivity));
            grdActivities.btnAdd.AddHandler(Button.ClickEvent, new RoutedEventHandler(AddActivity));
            grdActivities.ShowCopyCutPast = System.Windows.Visibility.Visible;
            grdActivities.ShowTagsFilter = Visibility.Visible;
            grdActivities.btnRefresh.AddHandler(Button.ClickEvent, new RoutedEventHandler(RefreshActivitiesGridHandler));
            grdActivities.AddToolbarTool("@Handler_16x16.png", "Add Error Handler", new RoutedEventHandler(AddErrorHandler));
            grdActivities.AddToolbarTool("@RoadSign_16x16.png", "Set " + GingerDicser.GetTermResValue(eTermResKey.Activities) + "-" + GingerDicser.GetTermResValue(eTermResKey.Variables) + " Dependencies", new RoutedEventHandler(LoadActivitiesVariablesDependenciesPage));
            grdActivities.AddToolbarTool("@Group_16x16.png", GingerDicser.GetTermResValue(eTermResKey.ActivitiesGroups) + " Manager", new RoutedEventHandler(LoadActivitiesGroupsManagr));
            grdActivities.AddToolbarTool("@UploadStar_16x16.png", "Add to Shared Repository", new RoutedEventHandler(AddToRepository));
            grdActivities.AddToolbarTool(eImageType.Reset, "Reset Run Details", new RoutedEventHandler(ResetActivity));

            //Events
            grdActivities.RowChangedEvent += grdActivities_RowChangedEvent;
            grdActivities.RowDoubleClick += grdActivities_grdMain_MouseDoubleClick;
            grdActivities.ItemDropped += grdActivities_ItemDropped;
            grdActivities.PreviewDragItem += grdActivities_PreviewDragItem;
            grdActivities.MarkUnMarkAllActive += MarkUnMarkAllActivities;
        }

        private void ResetActivity(object sender, RoutedEventArgs e)
        {
            mBusinessFlow.CurrentActivity.Reset();
        }

        //TODO: put in seperate class
        public class ActivityStatusConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter,
                    System.Globalization.CultureInfo culture)
            {
                string status = value.ToString();
                
                if (status.Equals(Amdocs.Ginger.CoreNET.Execution.eRunStatus.Running.ToString())) return Brushes.Purple;
                if (status.Equals(Amdocs.Ginger.CoreNET.Execution.eRunStatus.Passed.ToString())) return App.Current.TryFindResource("$PassedStatusColor") as SolidColorBrush;
                if (status.Equals(Amdocs.Ginger.CoreNET.Execution.eRunStatus.Failed.ToString())) return App.Current.TryFindResource("$FailedStatusColor") as SolidColorBrush;
                if (status.Equals(Amdocs.Ginger.CoreNET.Execution.eRunStatus.Pending.ToString())) return App.Current.TryFindResource("$PendingStatusColor") as SolidColorBrush;
                if (status.Equals(Amdocs.Ginger.CoreNET.Execution.eRunStatus.Blocked.ToString())) return App.Current.TryFindResource("$BlockedStatusColor") as SolidColorBrush;
                if (status.Equals(Amdocs.Ginger.CoreNET.Execution.eRunStatus.Stopped.ToString())) return App.Current.TryFindResource("$StoppedStatusColor") as SolidColorBrush;
                if (status.Equals(Amdocs.Ginger.CoreNET.Execution.eRunStatus.Skipped.ToString())) return App.Current.TryFindResource("$SkippedStatusColor") as SolidColorBrush;
                
                return Brushes.Gray;
            }

            public object ConvertBack(object value, Type targetType,
                object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        private void AddToRepository(object sender, RoutedEventArgs e)

        {
            Repository.SharedRepositoryOperations.AddItemsToRepository(grdActivities.Grid.SelectedItems.Cast<RepositoryItem>().ToList());
          
        }

        private void EditActivity(object sender, RoutedEventArgs e)
        {
            if (grdActivities.CurrentItem != null)
            {
                BusinessFlowWindows.ActivityEditPage w = new BusinessFlowWindows.ActivityEditPage((Activity)grdActivities.CurrentItem, activityParentBusinessFlow: mBusinessFlow);
                w.ShowAsWindow();
            }
            else
            {
                Reporter.ToUser(eUserMsgKeys.AskToSelectItem);
            }
        }

        private void AddActivity(object sender, RoutedEventArgs e)
        {
            Activity a = new Activity() { Active = true };
            a.Active = true;
            a.TargetApplication = mBusinessFlow.MainApplication;
            a.ActivityName = "New " + GingerDicser.GetTermResValue(eTermResKey.Activity);

            //adding the Activity directly to the Grid without opening it for Edit            
            int selectedActIndex = -1;
            if (mBusinessFlow.Activities.CurrentItem != null)            
                selectedActIndex = mBusinessFlow.Activities.IndexOf((Activity)mBusinessFlow.Activities.CurrentItem);            
            mBusinessFlow.Activities.Add(a);
            if (selectedActIndex >= 0)
                mBusinessFlow.Activities.Move(mBusinessFlow.Activities.Count - 1, selectedActIndex + 1);
            mBusinessFlow.Activities.CurrentItem = a;
            mBusinessFlow.CurrentActivity = a;
        }
        
        private void RefreshActivitiesGrid()
        {
            if (mBusinessFlow != null)
            {
                grdActivities.Title = "'" + mBusinessFlow.Name + "' - " + GingerDicser.GetTermResValue(eTermResKey.Activities);
                App.LocalRepository.MarkSharedRepositoryItems((IEnumerable<object>)mBusinessFlow.Activities, (IEnumerable<object>)App.LocalRepository.GetSolutionRepoActivities());
                grdActivities.DataSourceList = mBusinessFlow.Activities;                
            }
            else
            {
                grdActivities.DataSourceList = new ObservableList<Activity>();
            }
        }
        
        private void btnSetGridView_Click(object sender, RoutedEventArgs e)
        {
            SetActivitiesGridView();            
        }

        private void grdActivities_grdMain_MouseDoubleClick(object sender, EventArgs e)
        {
            BusinessFlowWindows.ActivityEditPage w = new BusinessFlowWindows.ActivityEditPage((Activity)grdActivities.CurrentItem, activityParentBusinessFlow:mBusinessFlow);
            w.ShowAsWindow();
        }

        private void LoadActivitiesVariablesDependenciesPage(object sender, RoutedEventArgs e)
        {
            VariablesDependenciesPage activitiesVarsDepPage = new VariablesDependenciesPage(mBusinessFlow);
            activitiesVarsDepPage.ShowAsWindow();
        }

        private void LoadActivitiesGroupsManagr(object sender, RoutedEventArgs e)
        {
            ActivitiesGroupsManagerPage activitiesGroupsManagerPage = new ActivitiesGroupsManagerPage(mBusinessFlow);
            activitiesGroupsManagerPage.ShowAsWindow();
        }
    }
}
