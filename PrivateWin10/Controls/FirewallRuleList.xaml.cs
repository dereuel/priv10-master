﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PrivateWin10.Pages;
using PrivateWin10.Windows;
using MiscHelpers;
using PrivateAPI;
using WinFirewallAPI;

namespace PrivateWin10.Controls
{
    /// <summary>
    /// Interaction logic for FirewallRuleList.xaml
    /// </summary>
    public partial class FirewallRuleList : UserControl
    {
        DataGridExt rulesGridExt;

        ObservableCollection<RuleItem> RulesList;

        public FirewallPage firewallPage = null;

        FirewallRule.Actions actionFilter = FirewallRule.Actions.Undefined;
        FirewallRule.Directions directionFilter = FirewallRule.Directions.Unknown;
        string textFilter = "";


        MenuItem menuEnableRule;
        MenuItem menuDisableRule;
        MenuItem menuRemoveRule;
        MenuItem menuBlockRule;
        MenuItem menuAllowRule;
        MenuItem menuEditRule;
        MenuItem menuCloneRule;
        MenuItem menuApproveRule;
        MenuItem menuRestoreRule;
        MenuItem menuRedoRule;

        public FirewallRuleList()
        {
            InitializeComponent();

            /*this.grpRules.Header = Translate.fmt("grp_firewall");
            this.grpRuleTools.Header = Translate.fmt("grp_tools");
            this.grpRuleView.Header = Translate.fmt("grp_view");

            this.btnCreateRule.Content = Translate.fmt("btn_mk_rule");
            this.btnEnableRule.Content = Translate.fmt("btn_enable_rule");
            this.btnDisableRule.Content = Translate.fmt("btn_disable_rule");
            this.btnRemoveRule.Content = Translate.fmt("btn_remove_rule");
            this.btnBlockRule.Content = Translate.fmt("btn_block_rule");
            this.btnAllowRule.Content = Translate.fmt("btn_allow_rule");
            this.btnEditRule.Content = Translate.fmt("btn_edit_rule");
            this.btnCloneRule.Content = Translate.fmt("btn_clone_rule");*/

            //this.chkNoDisabled.Content = Translate.fmt("chk_hide_disabled");
            //this.lblFilterRules.Content = Translate.fmt("lbl_filter_rules");

            //this.lblFilter.Content = Translate.fmt("lbl_filter");
            this.txtRuleFilter.LabelText = Translate.fmt("lbl_text_filter");
            this.cmbAll.Content = Translate.fmt("str_all_actions");
            this.cmbAllow.Content = Translate.fmt("str_allow");
            this.cmbBlock.Content = Translate.fmt("str_block");
            this.cmbBooth.Content = Translate.fmt("str_all_rules");
            this.cmbIn.Content = Translate.fmt("str_inbound");
            this.cmbOut.Content = Translate.fmt("str_outbound");
            this.chkNoDisabled.ToolTip = Translate.fmt("str_no_disabled");

            int i = 0;
            this.rulesGrid.Columns[++i].Header = Translate.fmt("lbl_name");
            this.rulesGrid.Columns[++i].Header = Translate.fmt("lbl_group");
            this.rulesGrid.Columns[++i].Header = Translate.fmt("lbl_index");
            this.rulesGrid.Columns[++i].Header = Translate.fmt("lbl_status");
            this.rulesGrid.Columns[++i].Header = Translate.fmt("lbl_count");
            this.rulesGrid.Columns[++i].Header = Translate.fmt("lbl_profiles");
            this.rulesGrid.Columns[++i].Header = Translate.fmt("lbl_action");
            this.rulesGrid.Columns[++i].Header = Translate.fmt("lbl_direction");
            this.rulesGrid.Columns[++i].Header = Translate.fmt("lbl_protocol");
            this.rulesGrid.Columns[++i].Header = Translate.fmt("lbl_remote_ip");
            this.rulesGrid.Columns[++i].Header = Translate.fmt("lbl_local_ip");
            this.rulesGrid.Columns[++i].Header = Translate.fmt("lbl_remote_port");
            this.rulesGrid.Columns[++i].Header = Translate.fmt("lbl_local_port");
            this.rulesGrid.Columns[++i].Header = Translate.fmt("lbl_icmp");
            this.rulesGrid.Columns[++i].Header = Translate.fmt("lbl_interfaces");
            this.rulesGrid.Columns[++i].Header = Translate.fmt("lbl_edge");
            this.rulesGrid.Columns[++i].Header = Translate.fmt("lbl_program");


            RulesList = new ObservableCollection<RuleItem>();
            rulesGrid.ItemsSource = RulesList;

            var contextMenu = new ContextMenu();


            WpfFunc.AddMenu(contextMenu, Translate.fmt("btn_mk_rule"), btnCreateRule_Click, TryFindResource("Icon_Plus"));
            contextMenu.Items.Add(new Separator());
            menuEnableRule = WpfFunc.AddMenu(contextMenu, Translate.fmt("btn_enable_rule"), btnEnableRule_Click, TryFindResource("Icon_Enable"));
            menuDisableRule = WpfFunc.AddMenu(contextMenu, Translate.fmt("btn_disable_rule"), btnDisableRule_Click, TryFindResource("Icon_Disable"));
            contextMenu.Items.Add(new Separator());
            menuBlockRule = WpfFunc.AddMenu(contextMenu, Translate.fmt("btn_block_rule"), btnBlockRule_Click, TryFindResource("Icon_Deny"));
            menuAllowRule = WpfFunc.AddMenu(contextMenu, Translate.fmt("btn_allow_rule"), btnAllowRule_Click, TryFindResource("Icon_Check"));
            contextMenu.Items.Add(new Separator());
            menuRemoveRule = WpfFunc.AddMenu(contextMenu, Translate.fmt("btn_remove_rule"), btnRemoveRule_Click, TryFindResource("Icon_Remove"));
            menuEditRule = WpfFunc.AddMenu(contextMenu, Translate.fmt("btn_edit_rule"), btnEditRule_Click, TryFindResource("Icon_Edit"));
            menuCloneRule = WpfFunc.AddMenu(contextMenu, Translate.fmt("btn_clone_rule"), btnCloneRule_Click, TryFindResource("Icon_Clone"));
            contextMenu.Items.Add(new Separator());
            menuApproveRule = WpfFunc.AddMenu(contextMenu, Translate.fmt("btn_approve_rule"), btnApproveRule_Click, TryFindResource("Icon_Approve"));
            menuRestoreRule = WpfFunc.AddMenu(contextMenu, Translate.fmt("btn_restore_rule"), btnRestoreRule_Click, TryFindResource("Icon_Undo"));
            menuRedoRule = WpfFunc.AddMenu(contextMenu, Translate.fmt("btn_redo_rule"), btnRedoRule_Click, TryFindResource("Icon_Redo"));

            rulesGrid.ContextMenu = contextMenu;

            rulesGridExt = new DataGridExt(rulesGrid);
            rulesGridExt.Restore(App.GetConfig("GUI", "rulesGrid_Columns", ""));

            UwpFunc.AddBinding(rulesGrid, new KeyGesture(Key.F, ModifierKeys.Control), (s,e)=> { this.txtRuleFilter.Focus(); });
            UwpFunc.AddBinding(rulesGrid, new KeyGesture(Key.Delete), btnRemoveRule_Click);

            try
            {
                textFilter = App.GetConfig("FwRules", "Filter", "");
                txtRuleFilter.Text = textFilter;
                cmbAction.SelectedIndex = App.GetConfigInt("FwRules", "Actions", 0);
                cmbDirection.SelectedIndex = App.GetConfigInt("FwRules", "Directions", 0);
                this.chkNoDisabled.IsChecked = App.GetConfigInt("FwRules", "NoDisabled", 0) == 1;
            }
            catch { }

            CheckRules();
        }

        public void OnClose()
        {
            App.SetConfig("GUI", "rulesGrid_Columns", rulesGridExt.Save());
        }

        public void SetPage(FirewallPage page)
        {
            firewallPage = page;

            // translate
            firewallPage.btnCreateRule.Label = Translate.fmt("btn_mk_rule");
            firewallPage.btnReloadRules.Label = Translate.fmt("btn_reload");
            firewallPage.btnCleanUpRules.Label = Translate.fmt("btn_cleanup_rules");
            //todo: localize btnDeDup and otehrs

            firewallPage.btnEnableRule.Label = Translate.fmt("btn_enable_rule");
            firewallPage.btnDisableRule.Label = Translate.fmt("btn_disable_rule");
            firewallPage.btnRemoveRule.Label = Translate.fmt("btn_remove_rule");
            firewallPage.btnBlockRule.Label = Translate.fmt("btn_block_rule");
            firewallPage.btnAllowRule.Label = Translate.fmt("btn_allow_rule");
            firewallPage.btnEditRule.Label = Translate.fmt("btn_edit_rule");
            firewallPage.btnCloneRule.Label = Translate.fmt("btn_clone_rule");

            firewallPage.btnApprove.Label = Translate.fmt("btn_approve_rule");
            (firewallPage.btnApprove.Items[0] as RibbonMenuItem).Header = Translate.fmt("btn_approve_all");
            firewallPage.btnRestore.Label = Translate.fmt("btn_restore_rule");
            (firewallPage.btnRestore.Items[0] as RibbonMenuItem).Header = Translate.fmt("btn_restore_all");
            firewallPage.btnApply.Label = Translate.fmt("btn_redo_rule");
            (firewallPage.btnApply.Items[0] as RibbonMenuItem).Header = Translate.fmt("btn_redo_all");

            // and connect
            firewallPage.btnCreateRule.Click += btnCreateRule_Click;
            firewallPage.btnReloadRules.Click += btnReload_Click;
            firewallPage.btnCleanUpRules.Click += btnCleanup_Click;

            firewallPage.btnDeDupRules.Click += btnCleanup_Click;
            firewallPage.btnDeDupAllow.Click += btnCleanup_Click;
            firewallPage.btnDeDupBlock.Click += btnCleanup_Click;

            firewallPage.btnEditRule.Click += btnEditRule_Click;
            firewallPage.btnEnableRule.Click += btnEnableRule_Click;
            firewallPage.btnDisableRule.Click += btnDisableRule_Click;
            firewallPage.btnRemoveRule.Click += btnRemoveRule_Click;
            firewallPage.btnBlockRule.Click += btnBlockRule_Click;
            firewallPage.btnAllowRule.Click += btnAllowRule_Click;
            firewallPage.btnCloneRule.Click += btnCloneRule_Click;

            firewallPage.btnApprove.Click += btnApproveRule_Click;
            (firewallPage.btnApprove.Items[0] as RibbonMenuItem).Click += btnApproveAllRules_Click;
            firewallPage.btnRestore.Click += btnRestoreRule_Click;
            (firewallPage.btnRestore.Items[0] as RibbonMenuItem).Click += btnRestoreAllRules_Click;
            firewallPage.btnApply.Click += btnRedoRule_Click;
            (firewallPage.btnApply.Items[0] as RibbonMenuItem).Click += btnRedoAllRules_Click;

            CheckRules();
        }
        

        public void UpdateRules(bool clear = false)
        {
            if (firewallPage == null)
                return;

            if (clear)
                RulesList.Clear();

            Dictionary<string, RuleItem> oldRules = new Dictionary<string, RuleItem>();
            foreach (RuleItem oldItem in RulesList)
                oldRules.Add(oldItem.Rule.guid, oldItem);

            Dictionary<Guid, List<FirewallRuleEx>> rules = App.client.GetRules(firewallPage.GetCurGuids(true));
            foreach (var ruleSet in rules)
            {
                foreach (FirewallRuleEx rule in ruleSet.Value)
                {
                    /*if (mHideDisabled && rule.Enabled == false)
                        continue;

                    if (FirewallPage.DoFilter(mRuleFilter, rule.Name, new List<ProgramID>() { rule.ProgID })) // todo: move to helper class
                        continue;*/

                    RuleItem item;
                    if (!oldRules.TryGetValue(rule.guid, out item))
                        RulesList.Add(new RuleItem(rule));
                    else
                    {
                        oldRules.Remove(rule.guid);

                        item.Update(rule);
                    }
                }
            }

            foreach (RuleItem item in oldRules.Values)
                RulesList.Remove(item);

            // update existing cels
            rulesGrid.Items.Refresh();

            CheckRules();
        }

        private void btnCreateRule_Click(object sender, RoutedEventArgs e)
        {
            firewallPage.ShowRuleWindow(null);
        }

        private void btnReload_Click(object sender, RoutedEventArgs e)
        {
            App.client.LoadRules();
            UpdateRules(true);
        }

        private void btnCleanup_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true; // fix for ribbon split button missbehavioure

            Priv10Engine.CleanupMode Mode = Priv10Engine.CleanupMode.RemoveExpired;
            if (sender == firewallPage.btnCleanUpRules)
                Mode = Priv10Engine.CleanupMode.RemoveTemporary;
            else if (sender == firewallPage.btnDeDupRules)
                Mode = Priv10Engine.CleanupMode.RemoveDuplicates;
            else if (sender == firewallPage.btnDeDupAllow)
                Mode = Priv10Engine.CleanupMode.RemoveDuplicatesAllow;
            else if (sender == firewallPage.btnDeDupBlock)
                Mode = Priv10Engine.CleanupMode.RemoveDuplicatesBlock;

            int Count = App.client.CleanUpRules(Mode);

            MessageBox.Show(Translate.fmt("msg_clean_res", Count), App.Title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnEditRule_Click(object sender, RoutedEventArgs e)
        {
            RuleItem item = (rulesGrid.SelectedItem as RuleItem);
            if (item == null)
                return;

            firewallPage.ShowRuleWindow(item.Rule);
        }

        private void ruleGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            btnEditRule_Click(null, null);
        }


        private void btnEnableRule_Click(object sender, RoutedEventArgs e)
        {
            foreach (RuleItem item in rulesGrid.SelectedItems)
            {
                item.Rule.Enabled = true;
                App.client.UpdateRule(item.Rule);
            }
        }

        private void btnDisableRule_Click(object sender, RoutedEventArgs e)
        {
            foreach (RuleItem item in rulesGrid.SelectedItems)
            {
                item.Rule.Enabled = false;
                App.client.UpdateRule(item.Rule);
            }
        }

        private void btnRemoveRule_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Translate.fmt("msg_remove_rules"), App.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            foreach (RuleItem item in rulesGrid.SelectedItems)
                App.client.RemoveRule(item.Rule);
        }

        private void btnBlockRule_Click(object sender, RoutedEventArgs e)
        {
            foreach (RuleItem item in rulesGrid.SelectedItems)
            {
                item.Rule.Action = FirewallRule.Actions.Block;
                App.client.UpdateRule(item.Rule);
            }
        }

        private void btnAllowRule_Click(object sender, RoutedEventArgs e)
        {
            foreach (RuleItem item in rulesGrid.SelectedItems)
            {
                item.Rule.Action = FirewallRule.Actions.Allow;
                App.client.UpdateRule(item.Rule);
            }
        }

        private void btnCloneRule_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Translate.fmt("msg_clone_rules"), App.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            foreach (RuleItem item in rulesGrid.SelectedItems)
            {
                FirewallRule rule = item.Rule.Duplicate();
                rule.Name += " - Duplicate"; // todo: translate
                App.client.UpdateRule(rule);
            }
        }

        private void btnApproveRule_Click(object sender, RoutedEventArgs e)
        {
            foreach (RuleItem item in rulesGrid.SelectedItems)
                App.client.SetRuleApproval(Priv10Engine.ApprovalMode.ApproveCurrent, item.Rule);
        }

        private void btnRestoreRule_Click(object sender, RoutedEventArgs e)
        {
            foreach (RuleItem item in rulesGrid.SelectedItems)
                App.client.SetRuleApproval(Priv10Engine.ApprovalMode.RestoreRules, item.Rule);
        }

        private void btnRedoRule_Click(object sender, RoutedEventArgs e)
        {
            foreach (RuleItem item in rulesGrid.SelectedItems)
                App.client.SetRuleApproval(Priv10Engine.ApprovalMode.ApproveChanges, item.Rule);
        }

        private void btnApproveAllRules_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true; // or else btnApproveRule_Click will be triggered to

            if (MessageBox.Show(Translate.fmt("msg_approve_all"), App.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            App.client.SetRuleApproval(Priv10Engine.ApprovalMode.ApproveCurrent, null);
        }

        private void btnRestoreAllRules_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true; // or else btnRestoreRule_Click will be triggered to

            if (MessageBox.Show(Translate.fmt("msg_restore_all"), App.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            App.client.SetRuleApproval(Priv10Engine.ApprovalMode.RestoreRules, null);
        }

        private void btnRedoAllRules_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true; // or else btnRedoRule_Click will be triggered to

            if (MessageBox.Show(Translate.fmt("msg_apply_all"), App.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            App.client.SetRuleApproval(Priv10Engine.ApprovalMode.ApproveChanges, null);
        }

        private void chkNoDisabled_Click(object sender, RoutedEventArgs e)
        {
            App.SetConfig("FwRules", "NoDisabled", this.chkNoDisabled.IsChecked == true ? 1 : 0);
            //UpdateRules(true);

            rulesGrid.Items.Filter = new Predicate<object>(item => RuleFilter(item));
        }


        private void TxtRuleFilter_Search(object sender, RoutedEventArgs e)
        {
            textFilter = txtRuleFilter.Text;
            App.SetConfig("FwRules", "Filter", textFilter);
            //UpdateRules(true);

            rulesGrid.Items.Filter = new Predicate<object>(item => RuleFilter(item));
        }
        
        private void CmbDirection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            directionFilter = (FirewallRule.Directions)cmbDirection.SelectedIndex;
            cmbDirection.Background = (cmbDirection.SelectedItem as ComboBoxItem).Background;
            App.SetConfig("FwRules", "Direction", (int)directionFilter);
            //UpdateRules(true);

            rulesGrid.Items.Filter = new Predicate<object>(item => RuleFilter(item));
        }

        private void CmbAction_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            actionFilter = (FirewallRule.Actions)cmbAction.SelectedIndex;
            cmbAction.Background = (cmbAction.SelectedItem as ComboBoxItem).Background;
            App.SetConfig("FwRules", "Actions", (int)actionFilter);
            //UpdateRules(true);

            rulesGrid.Items.Filter = new Predicate<object>(item => RuleFilter(item));
        }

        private bool RuleFilter(object obj)
        {
            var item = obj as RuleItem;

            switch (actionFilter)
            {
                case FirewallRule.Actions.Allow: if (item.Rule.Action != FirewallRule.Actions.Allow) return false; break;
                case FirewallRule.Actions.Block: if (item.Rule.Action != FirewallRule.Actions.Block) return false; break;
            }

            switch (directionFilter)
            {
                case FirewallRule.Directions.Inbound: if (item.Rule.Direction != FirewallRule.Directions.Inbound) return false; break;
                case FirewallRule.Directions.Outbound: if (item.Rule.Direction != FirewallRule.Directions.Outbound) return false; break;
            }

            if (chkNoDisabled.IsChecked == true && item.Rule.Enabled == false)
                return false;

            if (item.TestFilter(textFilter))
                return false;
            return true;
        }

        private void RuleGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CheckRules();
        }

        private void CheckRules()
        {
            int SelectedCount = 0;
            int EnabledCount = 0;
            int DisabledCount = 0;
            int AllowingCount = 0;
            int BlockingCount = 0;
            int ChangedCount = 0;
            int ChangedWBack = 0;

            foreach (RuleItem item in rulesGrid.SelectedItems)
            {
                SelectedCount++;
                if (item.Rule.Enabled)
                    EnabledCount++;
                else
                    DisabledCount++;
                if (item.Rule.Action == FirewallRule.Actions.Allow)
                    AllowingCount++;
                if (item.Rule.Action == FirewallRule.Actions.Block)
                    BlockingCount++;

                if (item.Rule.State != FirewallRuleEx.States.Approved)
                {
                    ChangedCount++;
                    if (item.Rule.Backup != null)
                        ChangedWBack++;
                }
            }

            menuEnableRule.IsEnabled = DisabledCount >= 1;
            menuDisableRule.IsEnabled = EnabledCount >= 1;
            menuRemoveRule.IsEnabled = SelectedCount >= 1;
            menuBlockRule.IsEnabled = AllowingCount >= 1;
            menuAllowRule.IsEnabled = BlockingCount >= 1;
            menuEditRule.IsEnabled = SelectedCount == 1;
            menuCloneRule.IsEnabled = SelectedCount >= 1;
            menuApproveRule.IsEnabled = ChangedCount >= 1;
            menuRestoreRule.IsEnabled = ChangedCount >= 1;
            menuRedoRule.IsEnabled = ChangedWBack >= 1;

            if (firewallPage == null)
                return;
            firewallPage.btnEnableRule.IsEnabled = DisabledCount >= 1;
            firewallPage.btnDisableRule.IsEnabled = EnabledCount >= 1;
            firewallPage.btnRemoveRule.IsEnabled = SelectedCount >= 1;
            firewallPage.btnBlockRule.IsEnabled = AllowingCount >= 1;
            firewallPage.btnAllowRule.IsEnabled = BlockingCount >= 1;
            firewallPage.btnEditRule.IsEnabled = SelectedCount == 1;
            firewallPage.btnCloneRule.IsEnabled = SelectedCount >= 1;

            var btnApprove = WpfFunc.FindChild<RibbonButton>(firewallPage.btnApprove);
            var btnRestore = WpfFunc.FindChild<RibbonButton>(firewallPage.btnRestore);
            var btnApply = WpfFunc.FindChild<RibbonButton>(firewallPage.btnApply);
            if (btnApprove != null && btnRestore != null && btnApply != null)
            {
                btnRestore.IsEnabled = btnApprove.IsEnabled = ChangedCount >= 1;
                btnApply.IsEnabled = ChangedWBack >= 1;
            }
        }

        private void RulesGrid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {

        }

        /////////////////////////////////
        /// RuleItem


        public class RuleItem : INotifyPropertyChanged
        {
            public FirewallRuleEx Rule;
            public Program Prog;

            public RuleItem(FirewallRuleEx rule, Program prog = null)
            {
                Rule = rule;
                Prog = prog;
            }

            public bool TestFilter(string textFilter)
            {
                string strings = this.Name;
                strings += " " + this.Grouping;
                strings += " " + this.Index;
                strings += " " + this.Status;
                strings += " " + this.Profiles;
                strings += " " + this.Action;
                strings += " " + this.Direction;
                strings += " " + this.Protocol;
                strings += " " + this.DestAddress;
                strings += " " + this.DestPorts;
                strings += " " + this.SrcAddress;
                strings += " " + this.SrcPorts;
                strings += " " + this.ICMPOptions;
                strings += " " + this.Interfaces;
                strings += " " + this.EdgeTraversal;
                return FirewallPage.DoFilter(textFilter, strings, new List<ProgramID>() { this.Rule.ProgID });
            }

            public ImageSource Icon { get { return ImgFunc.GetIcon(Rule.ProgID.Path, 16); } }
            public string Name { get { return App.GetResourceStr(Rule.Name); } }

            public string Program { get { return ProgramControl.FormatProgID(Rule.ProgID); } }

            public string Grouping
            {
                get
                {
                    if (Rule.Grouping != null && Rule.Grouping.Length > 1 && Rule.Grouping.Substring(0, 1) == "@")
                        return MiscFunc.GetResourceStr(Rule.Grouping);
                    return Rule.Grouping;
                }
            }

            public int Index { get { return Rule.Index; } }

            public string Status
            {
                get
                {
                    string strStatus = Translate.fmt(Rule.Enabled ? "str_enabled" : "str_disabled");
                    switch (Rule.State)
                    {
                        case FirewallRuleEx.States.Changed: strStatus += " - " +  Translate.fmt("str_changed"); break;
                        case FirewallRuleEx.States.Unknown: strStatus += " - " +  Translate.fmt("str_added"); break;
                        case FirewallRuleEx.States.Deleted: strStatus += " - " +  Translate.fmt("str_removed"); break;
                    }
                    return strStatus;
                }
            }

            public Int64 HitCount { get { return Rule.HitCount; } }

            public string NameColor
            {
                get
                {
                    switch (Rule.State)
                    {
                        case FirewallRuleEx.States.Changed: return "changed";
                        case FirewallRuleEx.States.Unknown: return "added";
                        case FirewallRuleEx.States.Deleted: return "removed";
                    }
                    return "";
                }
            }

            public string DisabledColor { get { return Rule.Enabled ? "" : "gray"; } }

            public string Profiles
            {
                get
                {
                    if (Rule.Profile == (int)FirewallRule.Profiles.All)
                        return Translate.fmt("str_all");
                    else
                    {
                        List<string> profiles = new List<string>();
                        if ((Rule.Profile & (int)FirewallRule.Profiles.Private) != 0)
                            profiles.Add(Translate.fmt("str_private"));
                        if ((Rule.Profile & (int)FirewallRule.Profiles.Domain) != 0)
                            profiles.Add(Translate.fmt("str_domain"));
                        if ((Rule.Profile & (int)FirewallRule.Profiles.Public) != 0)
                            profiles.Add(Translate.fmt("str_public"));
                        return string.Join(",", profiles.ToArray().Reverse());
                    }
                }
            }
            public string Action
            {
                get
                {
                    switch (Rule.Action)
                    {
                        case FirewallRule.Actions.Allow: return Translate.fmt("str_allow");
                        case FirewallRule.Actions.Block: return Translate.fmt("str_block");
                        default: return Translate.fmt("str_undefined");
                    }
                }
            }

            public string ActionColor
            {
                get
                {
                    switch (Rule.Action)
                    {
                        case FirewallRule.Actions.Allow: return "green";
                        case FirewallRule.Actions.Block: return "red";
                        default: return "";
                    }
                }
            }

            public string Direction
            {
                get
                {
                    switch (Rule.Direction)
                    {
                        case FirewallRule.Directions.Inbound: return Translate.fmt("str_inbound");
                        case FirewallRule.Directions.Outbound: return Translate.fmt("str_outbound");
                        default: return Translate.fmt("str_undefined");
                    }
                }
            }
            public string Protocol { get { return (Rule.Protocol == (int)NetFunc.KnownProtocols.Any) ? Translate.fmt("pro_any") : NetFunc.Protocol2Str((UInt32)Rule.Protocol); } }
            public string DestAddress { get { return Rule.RemoteAddresses; } }
            public string DestPorts { get { return Rule.RemotePorts; } }
            public string SrcAddress { get { return Rule.LocalAddresses; } }
            public string SrcPorts { get { return Rule.LocalPorts; } }

            public string ICMPOptions { get { return Rule.GetIcmpTypesAndCodes(); } }

            public string Interfaces
            {
                get
                {
                    if (Rule.Interface == (int)FirewallRule.Interfaces.All)
                        return Translate.fmt("str_all");
                    else
                    {
                        List<string> interfaces = new List<string>();
                        if ((Rule.Profile & (int)FirewallRule.Interfaces.Lan) != 0)
                            interfaces.Add(Translate.fmt("str_lan"));
                        if ((Rule.Profile & (int)FirewallRule.Interfaces.RemoteAccess) != 0)
                            interfaces.Add(Translate.fmt("str_ras"));
                        if ((Rule.Profile & (int)FirewallRule.Interfaces.Wireless) != 0)
                            interfaces.Add(Translate.fmt("str_wifi"));
                        return string.Join(",", interfaces.ToArray().Reverse());
                    }
                }
            }

            public void Update(FirewallRuleEx rule)
            {
                Rule = rule;

                NotifyPropertyChanged(null); // update all
            }

            public string EdgeTraversal { get { return Rule.EdgeTraversal.ToString(); } }

            #region INotifyPropertyChanged Members

            public event PropertyChangedEventHandler PropertyChanged;

            #endregion

            #region Private Helpers

            private void NotifyPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            #endregion
        }

    }
}
