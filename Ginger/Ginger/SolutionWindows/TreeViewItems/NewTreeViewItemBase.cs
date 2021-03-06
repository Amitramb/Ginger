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
using Amdocs.Ginger.Common;
using Amdocs.Ginger.Common.Enums;
using Amdocs.Ginger.Repository;
using Amdocs.Ginger.UserControls;
using Ginger;
using Ginger.SourceControl;
using GingerCore;
using GingerCoreNET.SourceControl;
using GingerWPF.UserControlsLib.UCTreeView;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace GingerWPF.TreeViewItemsLib
{
    public class NewTreeViewItemBase : TreeViewItemGenericBase
    {
        public SourceControlFileInfo.eRepositoryItemStatus ItemSourceControlStatus;//TODO: combine it with GingerCore one
        static bool mBulkOperationIsInProcess = false;
        public override bool SaveTreeItem(object item, bool saveOnlyIfDirty = false)
        {         
            if (item is RepositoryItemBase)
            {
                RepositoryItemBase RI = (RepositoryItemBase)item;
                if (saveOnlyIfDirty && RI.IsDirty == false) return false;//no need to Save because not Dirty
                Reporter.ToGingerHelper(eGingerHelperMsgKey.SaveItem, null, RI.ItemName, "item");
                WorkSpace.Instance.SolutionRepository.SaveRepositoryItem(RI);
                Reporter.CloseGingerHelper();               

                //refresh node header               
                mTreeView.Tree.SelectParentItem((ITreeViewItem)this);//to allow catch isDirty again when user will select this item again so we move to parent
                PostSaveTreeItemHandler();
                return true;
            }
            else
            {
                //implement for other item types
                Reporter.ToUser(eUserMsgKeys.StaticWarnMessage, "Save operation for this item type was not implemented yet.");
                return false;
            }
        }

        public override bool SaveBackup(object item)
        {
            if (item is RepositoryItemBase)
            {
                RepositoryItemBase RI = (RepositoryItemBase)item;
                RI.SaveBackup();
                return true;
            }

            return false;
        }

        public override bool ItemIsDirty(object item)
        {
            if (item is RepositoryItemBase)
            {
                return ((RepositoryItemBase)item).IsDirty;
            }

            return false;
        }

        public override bool DeleteTreeItem(object item, bool deleteWithoutAsking = false, bool refreshTreeAfterDelete = true)
        {
            if (item is RepositoryItemBase)
            {
                if (!deleteWithoutAsking)
                    //TODO: Fix with New Reporter (on GingerWPF)
                    if (System.Windows.MessageBox.Show(string.Format("Are you sure you want to delete '{0}' item ?", ((RepositoryItemBase)item).GetNameForFileName()), "Delete", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning, System.Windows.MessageBoxResult.No) == System.Windows.MessageBoxResult.No)                
                        return false;

                WorkSpace.Instance.SolutionRepository.DeleteRepositoryItem((RepositoryItemBase)item);               
                return true;
            }
            else
            {
                //implement for other item types              
                string filePath = string.Empty;
                if (item == null)
                    filePath = this.NodePath();
                else
                {                     
                    filePath = ((RepositoryItemBase)item).FilePath;
                }
                    
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    return true;
                }
            }

            return false;
        }

        public override void DuplicateTreeItem(object item)
        {
            if (item is RepositoryItemBase)
            {
                //string newName = ((RepositoryItemBase)item).GetNameForFileName() + "_Copy";
                //if (GingerCore.GeneralLib.InputBoxWindow.GetInputWithValidation("Duplicated Item Name", "Name:", ref newName, System.IO.Path.GetInvalidPathChars()))
                //{
                //    RepositoryItemBase copy = ((RepositoryItemBase)item).CreateCopy();
                //    copy.ItemName = newName;
                //    (WorkSpace.Instance.SolutionRepository.GetItemRepositoryFolder(((RepositoryItemBase)item))).AddRepositoryItem(copy);
                //}  
                RepositoryItemBase copiedItem = CopyTreeItemWithNewName((RepositoryItemBase)item);
                if (copiedItem != null)
                    (WorkSpace.Instance.SolutionRepository.GetItemRepositoryFolder(((RepositoryItemBase)item))).AddRepositoryItem(copiedItem);
            }
            else
            {
                //implement for other item types
                Reporter.ToUser(eUserMsgKeys.StaticWarnMessage, "Item type " + item.GetType().Name + " - operation for this item type was not implemented yet.");                
            }            
        }


        public override bool PasteCopiedTreeItem(object nodeItemToCopy, TreeViewItemGenericBase targetFolderNode, bool toRefreshFolder = true)
        {
            if (nodeItemToCopy is RepositoryItemBase)
            {
                RepositoryItemBase copiedItem = CopyTreeItemWithNewName((RepositoryItemBase)nodeItemToCopy);
                if (copiedItem != null)
                {
                    ((RepositoryFolderBase)(((ITreeViewItem)targetFolderNode).NodeObject())).AddRepositoryItem(copiedItem);
                    return true;
                }
                return false;
            }
            else
            {
                //implement for other item types
                Reporter.ToUser(eUserMsgKeys.StaticWarnMessage, "The " + mCurrentFolderNodePastOperations.ToString() + " operation for this item type was not implemented yet.");
                return false;
            }
        }

        public override void PasteCopiedTreeItems()
        {           
            foreach(RepositoryItemBase childItemToCopy in ((RepositoryFolderBase)(((ITreeViewItem)mNodeManipulationsSource).NodeObject())).GetFolderRepositoryItems())
            {
                RepositoryItemBase copiedItem = CopyTreeItemWithNewName((RepositoryItemBase)childItemToCopy);
                if (copiedItem != null)
                {
                    ((RepositoryFolderBase)(((ITreeViewItem)this).NodeObject())).AddRepositoryItem(copiedItem);                    
                }
            }
        }

        public override bool PasteCutTreeItem(object nodeItemToCut, TreeViewItemGenericBase targetFolderNode, bool toRefreshFolder = true)
        {
            if (nodeItemToCut is RepositoryItemBase)
            {
                // TODO: change me, ugly but will work for now
                string relativeFolder = targetFolderNode.NodePath().Replace(WorkSpace.Instance.SolutionRepository.SolutionFolder, @"~\");
                WorkSpace.Instance.SolutionRepository.MoveItem((RepositoryItemBase)nodeItemToCut, relativeFolder);

              

                return true;
            }
            else
            {
                //implement for other item types
                Reporter.ToUser(eUserMsgKeys.StaticWarnMessage, "The " + mCurrentFolderNodePastOperations.ToString() + " operation for this item type was not implemented yet.");
                return false;
            }
        }

        public override void PasteCutTreeItems()
        {
            foreach (RepositoryItemBase childItemToCut in ((RepositoryFolderBase)(((ITreeViewItem)mNodeManipulationsSource).NodeObject())).GetFolderRepositoryItems())
            {
                PasteCutTreeItem((RepositoryItemBase)childItemToCut, this);                
            }
        }

        public override void SaveAllTreeFolderItems()
        {
            List<ITreeViewItem> childNodes = mTreeView.Tree.GetTreeNodeChildsIncludingSubChilds((ITreeViewItem)this);
                       
            int itemsSavedCount = 0;
            foreach (ITreeViewItem node in childNodes)
            {
                if (node != null && node.NodeObject() is RepositoryItemBase)
                {                    
                    RepositoryItemBase RI = (RepositoryItemBase)node.NodeObject();
                    if (RI != null)
                    {
                        if ((RI.DirtyStatus == eDirtyStatus.NoTracked && RI.IsDirty) 
                                || (RI.DirtyStatus == eDirtyStatus.Modified))
                        {
                            // Try to save only items with file name = standalone xml, avoid items like env app                            
                            if (!string.IsNullOrEmpty(RI.ContainingFolder))
                            {
                                if (SaveTreeItem(node.NodeObject(), true))
                                {
                                    itemsSavedCount++;
                                }
                            }
                        }
                    }
                }
            }
            if (itemsSavedCount == 0)
            {
                Reporter.ToUser(eUserMsgKeys.StaticWarnMessage, "Nothing found to Save.");               
            }
            else
            {
                mTreeView.Tree.SelectItem((ITreeViewItem)this);//in case the event was called from diffrent class                                                             
            }
        }
        
        public override void RefreshTreeFolder(Type itemType, string path)
        {
            try
            {
                if (System.Windows.MessageBox.Show("Un saved items changes under the refreshed folder will be lost, to continue with refresh?", "Refresh Folder", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning, System.Windows.MessageBoxResult.No) == System.Windows.MessageBoxResult.Yes)
                {
                    mBulkOperationIsInProcess = true;
                    //refresh cache
                    RepositoryFolderBase repoFolder = (RepositoryFolderBase)(((ITreeViewItem)this).NodeObject());
                    if (repoFolder != null)
                        repoFolder.ReloadItems(); // .RefreshFolderCache();
                    
                    //refresh tree
                    mTreeView.Tree.RefresTreeNodeChildrens((ITreeViewItem)this);

                    mBulkOperationIsInProcess = false;
                }
            }
            catch (Exception ex)
            {
                //TODO: Fix with New Reporter (on GingerWPF)
                System.Windows.MessageBox.Show(String.Format("Failed to refresh the item type cache for the folder: '{0}'", path), "Refresh Failed", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error, System.Windows.MessageBoxResult.OK);
                Reporter.ToLog(eLogLevel.ERROR, $"Method - {MethodBase.GetCurrentMethod().Name}, Error - {ex.Message}");
                mBulkOperationIsInProcess = false;
            }
        }

        public override ITreeViewItem AddSubFolder(Type typeOfFolder, string newFolderName, string newFolderPath)
        {
            try
            {
                object folderItem;
                RepositoryFolderBase repoFolder = (RepositoryFolderBase)(((ITreeViewItem)this).NodeObject());
                object newFolder = repoFolder.AddSubFolder(newFolderName);


                //FIXME not good approuch
                try
                {
                    folderItem = Activator.CreateInstance(this.GetType(), newFolder);
                }
                catch (Exception ex)
                {
                    folderItem = Activator.CreateInstance(this.GetType(),
                                                                    BindingFlags.CreateInstance |
                                                                    BindingFlags.Public |
                                                                    BindingFlags.Instance |
                                                                    BindingFlags.OptionalParamBinding, null, new object[] { repoFolder }, CultureInfo.CurrentCulture);
                    Reporter.ToLog(eLogLevel.ERROR, $"Method - {MethodBase.GetCurrentMethod().Name}, Error - {ex.Message}");
                }
                if (folderItem == null)
                {
                    return null;
                }

                return (ITreeViewItem)folderItem;
            }
            catch (Exception ex)
            {
                Reporter.ToLog(eLogLevel.ERROR, $"Method - {MethodBase.GetCurrentMethod().Name}, Error - {ex.Message}");
                return null;
            }
        }

        public override bool RenameTreeFolder(string originalName, string newFolderName, string newPath)
        {
            try
            {
                RepositoryFolderBase repoFolder = (RepositoryFolderBase)((ITreeViewItem)this).NodeObject();
                repoFolder.RenameFolder(newFolderName);
            }
            catch (Exception ex)
            {
                Reporter.ToLog(eLogLevel.ERROR, $"Method - {MethodBase.GetCurrentMethod().Name}, Error - {ex.Message}");
                return false;
            }
            return true;
        }

        public override void DeleteTreeFolder()
        {
            try
            {
                //TODO: Fix with New Reporter (on GingerWPF)
                //if (System.Windows.MessageBox.Show(string.Format("Are you sure you want to delete the '{0}' folder and all of it content?", mTreeView.Tree.GetSelectedTreeNodeName()), "Delete Foler", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning, System.Windows.MessageBoxResult.No) == System.Windows.MessageBoxResult.Yes)
                //{
                //    ITreeViewItem TVI = (ITreeViewItem)(mTreeView.Tree.CurrentSelectedTreeViewItem);
                //    RepositoryFolderBase RF = (RepositoryFolderBase)TVI.NodeObject();
                //    WorkSpace.Instance.SolutionRepository.DeleteRepositoryItemFolder((RepositoryFolderBase)((ITreeViewItem)this).NodeObject());


                //    mBulkOperationIsInProcess = true;
                //    List<ITreeViewItem> childNodes = mTreeView.Tree.GetTreeNodeChildsIncludingSubChilds((ITreeViewItem)this);
                //    childNodes.Reverse();
                //    foreach (ITreeViewItem node in childNodes)
                //    {
                //        if (node == null || node.NodeObject() == null) continue;
                //        if ((node.NodeObject().GetType().BaseType != typeof(RepositoryFolderBase)))
                //        {
                //            DeleteTreeItem(node.NodeObject(), true, false);
                //        }
                //        else
                //        {
                //            if (Directory.Exists(((TreeViewItemBase)node).NodePath()))
                //                WorkSpace.Instance.SolutionRepository.DeleteRepositoryItemFolder((RepositoryFolderBase)node.NodeObject());
                //        }
                //    }

                    //delete root and refresh tree                    
                    WorkSpace.Instance.SolutionRepository.DeleteRepositoryItemFolder((RepositoryFolderBase)((ITreeViewItem)this).NodeObject());
                    mTreeView.Tree.RefreshSelectedTreeNodeParent();
               //}
            }
            finally
            {
                mBulkOperationIsInProcess = false;
            }
        }

        public override void AddTreeItem()
        {
            System.Windows.MessageBox.Show("Functionality was not yet implemented", "Not Implemented", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning, System.Windows.MessageBoxResult.OK);
        }

        public virtual ITreeViewItem GetFolderTreeItem(RepositoryFolderBase folder)
        {
            throw new NotImplementedException();
        }

        public virtual ITreeViewItem GetTreeItem(object item)
        {
            throw new NotImplementedException();
        }

        public void TreeFolderItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
                if (mTreeView == null || e == null) return;

                if (mBulkOperationIsInProcess) return;

                // Since refresh of tree items can be triggered from FileWatcher runnning on seperate thread, all TV handling is done on the TV.Dispatcher
                switch (e.Action)
                {
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                        if (e.NewItems != null && e.NewItems.Count > 0)
                        {
                        mTreeView.Tree.Dispatcher.Invoke(() =>
                        {
                            mTreeView.Tree.AddChildItemAndSelect((ITreeViewItem)this, GetTreeItem((dynamic)e.NewItems[0]));
                        });
                        }
                        break;

                    case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                        if (e.OldItems != null && e.OldItems.Count > 0)
                        {
                            mTreeView.Tree.Dispatcher.Invoke(() =>
                            {
                                mTreeView.Tree.DeleteItemByObjectAndSelectParent(e.OldItems[0], (ITreeViewItem)this);
                            });
                        }
                    break;

                    case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                        mTreeView.Tree.Dispatcher.Invoke(() =>
                        {
                            mTreeView.Tree.RefresTreeNodeChildrens((ITreeViewItem)this);
                        });
                    break;
                }
        }

        public override void AddSourceControlOptions(ContextMenu CM, bool addDetailsOption = true, bool addLocksOption = true)
        {
            if (App.UserProfile.Solution != null && App.UserProfile.Solution.SourceControl != null)
            {
                MenuItem sourceControlMenu = TreeViewUtils.CreateSubMenu(CM, "Source Control");
                if (addDetailsOption)
                    TreeViewUtils.AddSubMenuItem(sourceControlMenu, "Get Info", SourceControlGetInfo, null, "@Info_16x16.png");
                TreeViewUtils.AddSubMenuItem(sourceControlMenu, "Check-In Changes", SourceControlCheckIn, null, "@CheckIn2_16x16.png");
                if (App.UserProfile.Solution.SourceControl.IsSupportingGetLatestForIndividualFiles)
                    TreeViewUtils.AddSubMenuItem(sourceControlMenu, "Get Latest Version", SourceControlGetLatestVersion, null, "@GetLatest2_16x16.png");
                if (App.UserProfile.Solution.ShowIndicationkForLockedItems && App.UserProfile.Solution.SourceControl.IsSupportingLocks && addLocksOption)
                    if (ItemSourceControlStatus == SourceControlFileInfo.eRepositoryItemStatus.LockedByAnotherUser || ItemSourceControlStatus == SourceControlFileInfo.eRepositoryItemStatus.LockedByMe)
                    {
                        TreeViewUtils.AddSubMenuItem(sourceControlMenu, "UnLock Item", SourceControlUnlock, null, "@Unlock_16x16.png");
                    }
                    else
                    {
                        TreeViewUtils.AddSubMenuItem(sourceControlMenu, "Lock Item", SourceControlLock, null, "@Lock_16x16.png");
                    }
                TreeViewUtils.AddSubMenuItem(sourceControlMenu, "Undo Changes", SourceControlUndoChanges, null, "@Undo_16x16.png");
            }
        }

        private void SourceControlGetInfo(object sender, RoutedEventArgs e)
        {
            SourceControlItemInfoDetails SCIID = SourceControlIntegration.GetInfo(App.UserProfile.Solution.SourceControl, this.NodePath());
            SourceControlItemInfoPage SCIIP = new SourceControlItemInfoPage(SCIID);
            SCIIP.ShowAsWindow();
        }

        private void SourceControlUnlock(object sender, RoutedEventArgs e)
        {
            SourceControlIntegration.UnLock(App.UserProfile.Solution.SourceControl, this.NodePath());
            mTreeView.Tree.RefreshHeader((ITreeViewItem)this);
        }

        private void SourceControlLock(object sender, RoutedEventArgs e)
        {
            string lockComment = string.Empty;
            if (GingerCore.General.GetInputWithValidation("Lock", "Lock Comment:", ref lockComment, System.IO.Path.GetInvalidFileNameChars()))
            {
                SourceControlIntegration.Lock(App.UserProfile.Solution.SourceControl, this.NodePath(), lockComment);
                mTreeView.Tree.RefreshHeader((ITreeViewItem)this);
            }
        }

        public void SourceControlCheckIn(object sender, System.Windows.RoutedEventArgs e)
        {
            CheckInPage CIW = new CheckInPage(this.NodePath());
            CIW.CallBackOnClose += CIWClosed;
            CIW.ShowAsWindow();
        }

        void CIWClosed()
        {                     
            // when the check in window is closed we refresh the source control status icon
            Object nodeObject = ((ITreeViewItem)this).NodeObject();

            if (nodeObject is RepositoryItemBase)   // Single Repository item 
            {
                ((RepositoryItemBase)nodeObject).RefreshSourceControlStatus();
            }
            else if (nodeObject is RepositoryFolderBase)  // repositoryFolder
            {
                var v = ((RepositoryFolderBase)nodeObject).GetFolderRepositoryItems();
                foreach (RepositoryItemBase RI in v)
                {
                    RI.RefreshSourceControlStatus();
                }
            }

        }

        public void SourceControlUndoChanges(object sender, System.Windows.RoutedEventArgs e)
        {
            if (Reporter.ToUser(eUserMsgKeys.SureWantToDoRevert) == MessageBoxResult.Yes)
            {
                Reporter.ToGingerHelper(eGingerHelperMsgKey.RevertChangesFromSourceControl);
                SourceControlIntegration.Revert(App.UserProfile.Solution.SourceControl, this.NodePath());
                App.LocalRepository.RefreshCacheByItemType(this.NodeObjectType(), Path.GetDirectoryName(this.NodePath()));
                mTreeView.Tree.RefreshSelectedTreeNodeParent();
                Reporter.CloseGingerHelper();
            }
        }

        public void SourceControlGetLatestVersion(object sender, System.Windows.RoutedEventArgs e)
        {
            if (Reporter.ToUser(eUserMsgKeys.LoseChangesWarn) == MessageBoxResult.No) return;

            Reporter.ToGingerHelper(eGingerHelperMsgKey.GetLatestFromSourceControl);
            if (string.IsNullOrEmpty(this.NodePath()))
                Reporter.ToUser(eUserMsgKeys.SourceControlUpdateFailed, "Invalid Path provided");
            else
                SourceControlIntegration.GetLatest(this.NodePath(), App.UserProfile.Solution.SourceControl);

            App.LocalRepository.RefreshCacheByItemType(this.NodeObjectType(), Path.GetDirectoryName(this.NodePath()));
            mTreeView.Tree.RefreshSelectedTreeNodeParent();
            Reporter.CloseGingerHelper();
        }

        
        protected eImageType GetSourceControlImage(RepositoryItemBase repositoryItem)
        {                                    
            return GetSourceControlImageByPath(repositoryItem.FilePath);                        
        }


        protected eImageType GetSourceControlImage(RepositoryFolderBase repositoryFolderBase)
        {            
            return GetSourceControlImageByPath(repositoryFolderBase.FolderFullPath);         
        }

        protected eImageType GetSourceControlImageByPath(string path)
        {            
            return SourceControlIntegration.GetFileImage(path);            
        }

        protected List<ITreeViewItem> GetChildrentGeneric<T>(RepositoryFolder<T> RF, string OrderBy)
        {
            List<ITreeViewItem> Childrens = new List<ITreeViewItem>();

            ObservableList<RepositoryFolder<T>> subFolders = RF.GetSubFolders();
            foreach (RepositoryFolder<T> envFolder in subFolders)
            {
                Childrens.Add(GetTreeItem(envFolder));
            }
            subFolders.CollectionChanged -= TreeFolderItems_CollectionChanged; // track sub folders
            subFolders.CollectionChanged += TreeFolderItems_CollectionChanged; // track sub folders

            //Add direct childrens        
            ObservableList<T> folderItems = RF.GetFolderItems();
            // why we need -? in case we did refresh and reloaded the item TODO: research, make children called once
            folderItems.CollectionChanged -= TreeFolderItems_CollectionChanged;
            folderItems.CollectionChanged += TreeFolderItems_CollectionChanged;//adding event handler to add/remove tree items automatically based on folder items collection changes

            foreach (T item in folderItems.OrderBy(OrderBy))
            {
                ITreeViewItem tvi = GetTreeItem(item);
                Childrens.Add(tvi);
            }
            return Childrens;
        }


        protected StackPanel NewTVItemStyle(RepositoryItemBase RI, eImageType imageType, string NameProperty)
        {            
            RI.StartDirtyTracking();

            //The new item style with Source control
            StackPanel stack = new StackPanel();
            stack.Orientation = Orientation.Horizontal;

            if (WorkSpace.Instance.SourceControl != null)
            {
                // Source control image
                ImageMakerControl sourceControlImage = new ImageMakerControl();                
                sourceControlImage.BindControl(RI, nameof(RepositoryItemBase.SourceControlStatus));
                sourceControlImage.Width = 10;
                sourceControlImage.Height = 10;
                stack.Children.Add(sourceControlImage);
            }

            // Add Item Image            
            ImageMakerControl NodeImageType = new ImageMakerControl();
            NodeImageType.ImageType = imageType;
            NodeImageType.Width = 16;
            NodeImageType.Height = 16;
            stack.Children.Add(NodeImageType);

            // Add Item header text 
            Label itemHeaderLabel = new Label();
            itemHeaderLabel.BindControl(RI, NameProperty);
            stack.Children.Add(itemHeaderLabel);

            // add icon of dirty status            
            ImageMakerControl dirtyStatusImage = new ImageMakerControl();            
            dirtyStatusImage.BindControl(RI, nameof(RepositoryItemBase.DirtyStatusImage));
            dirtyStatusImage.Width = 6;
            dirtyStatusImage.Height = 6;            
            dirtyStatusImage.VerticalAlignment = VerticalAlignment.Top;
            dirtyStatusImage.Margin = new Thickness(0, 10, 10, 0);
            stack.Children.Add(dirtyStatusImage);

            return stack;
        }

    }
}
