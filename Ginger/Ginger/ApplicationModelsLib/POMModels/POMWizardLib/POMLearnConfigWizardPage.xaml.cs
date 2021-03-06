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

using amdocs.ginger.GingerCoreNET;
using Amdocs.Ginger;
using Amdocs.Ginger.Common;
using Amdocs.Ginger.Common.UIElement;
using Amdocs.Ginger.Repository;
using Ginger.Actions.Locators.ASCF;
using Ginger.Agents;
using Ginger.Drivers.PowerBuilder;
using Ginger.Drivers.Windows;
using Ginger.UserControls;
using Ginger.WindowExplorer;
using Ginger.WindowExplorer.Android;
using Ginger.WindowExplorer.Appium;
using Ginger.WindowExplorer.HTMLCommon;
using Ginger.WindowExplorer.Java;
using Ginger.WindowExplorer.Mainframe;
using GingerCore;
using GingerCore.Actions.UIAutomation;
using GingerCore.Drivers;
using GingerCore.Drivers.AndroidADB;
using GingerCore.Drivers.Appium;
using GingerCore.Drivers.Common;
using GingerCore.Drivers.JavaDriverLib;
using GingerCore.Platforms;
using GingerCore.Platforms.PlatformsInfo;
using GingerCoreNET.SolutionRepositoryLib.RepositoryObjectsLib.PlatformsLib;
using GingerWPF.WizardLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Xml;
using static GingerCore.Agent;

namespace Ginger.ApplicationModelsLib.POMModels.AddEditPOMWizardLib
{
    /// <summary>
    /// Interaction logic for LearnWizardPage.xaml
    /// </summary>
    public partial class POMLearnConfigWizardPage : Page, IWizardPage
    {
        private AddPOMWizard mWizard;
        private ePlatformType mAppPlatform;

        public POMLearnConfigWizardPage()
        {
            InitializeComponent();           
        }

        public void WizardEvent(WizardEventArgs WizardEventArgs)
        {
            switch (WizardEventArgs.EventType)
            {
                case EventType.Init:
                    mWizard = (AddPOMWizard)WizardEventArgs.Wizard;
                    //In case 
                    if (mWizard.POM.TargetApplicationKey != null)
                         mAppPlatform = GetTargetApplicationPlatform();
                    mWizard.OptionalAgentsList = GingerCore.General.ConvertListToObservableList((from x in WorkSpace.Instance.SolutionRepository.GetAllRepositoryItems<Agent>() where x.Platform == mAppPlatform select x).ToList());
                    xAgentControlUC.Init(mWizard.OptionalAgentsList);
                    App.ObjFieldBinding(xAgentControlUC, ucAgentControl.SelectedAgentProperty, mWizard, nameof(mWizard.Agent));
                    xAgentControlUC.AddValidationRule(new AgentControlValidationRule(AgentControlValidationRule.eAgentControlValidationRuleType.AgentIsMappedAndRunning));
                    xAgentControlUC.PropertyChanged += XAgentControlUC_PropertyChanged;

                    ClearAutoMapElementTypesSection();
                    SetAutoMapElementTypesGridView();                    
                    break;
            }
        }

        private void SetAutoMapElementTypes()
        {
            if (mWizard.AutoMapElementTypesList.Count == 0)
            {                
                List<eElementType> UIElementsTypeList = null;
                switch (mAppPlatform)
                {
                    case ePlatformType.Web:
                        WebPlatform webPlatformInfo = new WebPlatform();
                        UIElementsTypeList = webPlatformInfo.GetPlatformUIElementsType();
                        break;
                }

                foreach (eElementType eET in UIElementsTypeList)                
                    mWizard.AutoMapElementTypesList.Add(new UIElementFilter(eET, string.Empty));                
            }
        }

        private ePlatformType GetTargetApplicationPlatform()
        {
            string targetapp = mWizard.POM.TargetApplicationKey.ItemName;
            ePlatformType platform = (from x in App.UserProfile.Solution.ApplicationPlatforms where x.AppName == targetapp select x.Platform).FirstOrDefault();
            return platform;

        }

        private void SetAutoMapElementTypesGridView()
        {
            //tool bar
            xAutoMapElementTypesGrid.AddToolbarTool("@UnCheckAllColumn_16x16.png", "Check/Uncheck All Elements", new RoutedEventHandler(CheckUnCheckAllElements));

            //Set the Data Grid columns            
            GridViewDef view = new GridViewDef(GridViewDef.DefaultViewName);
            view.GridColsView = new ObservableList<GridColView>();

            view.GridColsView.Add(new GridColView() { Field = nameof(UIElementFilter.Selected), Header = "To Map", WidthWeight = 20, StyleType = GridColView.eGridColStyleType.CheckBox});           
            view.GridColsView.Add(new GridColView() { Field = nameof(UIElementFilter.ElementType), Header = "Element Type", WidthWeight = 100, ReadOnly=true });
            view.GridColsView.Add(new GridColView() { Field = nameof(UIElementFilter.ElementExtraInfo), Header = "Element Extra Info", WidthWeight = 100, ReadOnly = true });

            xAutoMapElementTypesGrid.SetAllColumnsDefaultView(view);
            xAutoMapElementTypesGrid.InitViewItems();
        }

        private void CheckUnCheckAllElements(object sender, RoutedEventArgs e)
        {
            if (mWizard.AutoMapElementTypesList.Count > 0)
            {
                bool valueToSet = !mWizard.AutoMapElementTypesList[0].Selected;
                foreach (UIElementFilter elem in mWizard.AutoMapElementTypesList)
                    elem.Selected = valueToSet;
            }
        }

        private void XAgentControlUC_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ucAgentControl.AgentIsRunning))
            {
                if (xAgentControlUC.AgentIsRunning)
                    SetAutoMapElementTypesSection();
                else
                    ClearAutoMapElementTypesSection();
            }
        }


        private void ClearAutoMapElementTypesSection()
        {
            xAutoMapElementTypesExpander.IsExpanded = false;
            xAutoMapElementTypesExpander.IsEnabled = false;
            mWizard.AutoMapElementTypesList = new ObservableList<UIElementFilter>();
            xAutoMapElementTypesGrid.DataSourceList = mWizard.AutoMapElementTypesList;
        }

        private void SetAutoMapElementTypesSection()
        {
            xAutoMapElementTypesExpander.IsExpanded = true;
            xAutoMapElementTypesExpander.IsEnabled = true;
            SetAutoMapElementTypes();
            xAutoMapElementTypesGrid.DataSourceList = mWizard.AutoMapElementTypesList;
        }

    }
}
