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
using System;
using System.Windows;
using System.Windows.Controls;
using GingerCore;

namespace Ginger.Dictionaries
{
    /// <summary>
    /// Interaction logic for SetTerminologyTypePage.xaml
    /// </summary>
    public partial class SetTerminologyTypePage : Page
    {
        Amdocs.Ginger.Core.eTerminologyDicsType mInitialType;
        GenericWindow _pageGenericWin = null;

        public SetTerminologyTypePage()
        {
            InitializeComponent();

            TerminologyTypeCombo.ItemsSource = GingerCore.General.GetEnumValues(typeof(Amdocs.Ginger.Core.eTerminologyDicsType));
            App.ObjFieldBinding(TerminologyTypeCombo, ComboBox.TextProperty, App.UserProfile, nameof(UserProfile.TerminologyDictionaryType));

            mInitialType = App.UserProfile.TerminologyDictionaryType;
        }

        public void ShowAsWindow(eWindowShowStyle windowStyle = eWindowShowStyle.Dialog)
        {
            ObservableList<Button> winButtons = new ObservableList<Button>();
            Button okBtn = new Button();
            okBtn.Content = "Ok";
            okBtn.Click += new RoutedEventHandler(okBtn_Click);
            winButtons.Add(okBtn);

            Button undoBtn = new Button();
            undoBtn.Content = "Cancel";
            undoBtn.Click += new RoutedEventHandler(undoBtn_Click);
            winButtons.Add(undoBtn);

            GingerCore.General.LoadGenericWindow(ref _pageGenericWin, App.MainWindow, windowStyle, this.Title, this, winButtons, false, string.Empty, CloseWinClicked);
        }

        private void undoBtn_Click(object sender, RoutedEventArgs e)
        {
            App.UserProfile.TerminologyDictionaryType = mInitialType;
            _pageGenericWin.Close();
        }

        private void okBtn_Click(object sender, RoutedEventArgs e)
        {
            if (App.UserProfile.TerminologyDictionaryType != mInitialType)
            {
                Reporter.ToUser(eUserMsgKeys.SettingsChangeRequireRestart);
            }
            _pageGenericWin.Close();
        }

        private void CloseWinClicked(object sender, EventArgs e)
        {
            App.UserProfile.TerminologyDictionaryType = mInitialType;
            _pageGenericWin.Close();
        }
    }
}
