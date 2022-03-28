﻿using MiscHelpers;
using PrivateWin10.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace PrivateWin10
{
    public class ServiceModel
    {
        private static ServiceModel mInstance = null;
        public static ServiceModel GetInstance()
        {
            if (mInstance == null)
                mInstance = new ServiceModel();
            return mInstance;
        }

        public ObservableCollection<Service> Services { get; set; }

        public ServiceModel()
        {
            Services = new ObservableCollection<Service>();
        }

        public void Reload()
        {
            Services.Clear();

            Services.Add(new Service() { Content = Translate.fmt("svc_all"), Value="*", Group = Translate.fmt("lbl_selec") });

            foreach (ServiceController svc in ServiceController.GetServices())
            {
                var ImagePath = ServiceHelper.GetServiceImagePath(svc.ServiceName);
                var ServicePath = ImagePath != null ? ProcFunc.GetPathFromCmdLine(ImagePath) : "";

                Services.Add(new Service() { Value = svc.ServiceName, Path = ServicePath, Content = svc.DisplayName + " (" + svc.ServiceName + ")", Group = Translate.fmt("lbl_known") });
            }
        }

        public class Service : ContentControl
        {
            public string Value { get; set; }
            public string Path { get; set; }
            public string Group { get; set; }
        }

        public IEnumerable GetServices()
        {
            Reload();

            ListCollectionView lcv = new ListCollectionView(Services);
            lcv.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
            lcv.SortDescriptions.Add(new SortDescription("Content", ListSortDirection.Ascending));
            return lcv;
        }
    }
}
