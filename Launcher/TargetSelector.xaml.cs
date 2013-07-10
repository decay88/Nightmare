﻿using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using Syringe;
using System.Security.Principal;
using System.Diagnostics;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for WoWSelector.xaml
    /// </summary>
    public partial class TargetSelector : Window
    {
        private const string
            BootstrapperPath = "NetLoader.dll",
            //InjectedLibPath = "Bloodstream.dll",
            InjectedLibPath = "Bitchstream.dll",
            TargetString = "Notepad++"
            //TargetString = "Borderlands2"
            ;

        private readonly bool v_series = false;
        private Action<FrameworkElement, int> attachCallback;

        [StructLayout(LayoutKind.Sequential)]
        struct NetLoaderInitializer
        {
            [CustomMarshalAs(CustomUnmanagedType.LPWStr)]
            public string AssemblyPath;
            [CustomMarshalAs(CustomUnmanagedType.LPWStr)]
            public string ClassName;
            [CustomMarshalAs(CustomUnmanagedType.LPWStr)]
            public string MethodName;
            [CustomMarshalAs(CustomUnmanagedType.LPWStr)]
            public string Argument;
        }

        public TargetSelector()
        {
            InitializeComponent();

            var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            Debug.Assert(principal.IsInRole(WindowsBuiltInRole.Administrator));

            attachCallback = (sender, i) =>
            {
                IsEnabled = false;

                var targetProc = System.Diagnostics.Process.GetProcessById(i);
                using (var inj = new Injector(targetProc, true))
                {
                    //var nli = new NetLoaderInitializer
                    //{
                    //    AssemblyPath = Path.GetFullPath(InjectedLibPath),
                    //    ClassName = "Bloodstream.EntryPoint",
                    //    MethodName = "Main",
                    //    Argument = "testtesttest",
                    //};

                    inj.InjectLibrary(InjectedLibPath);
                    inj.CallExport(InjectedLibPath, "Main");
                }

                LoadingPanel.Visibility = System.Windows.Visibility.Visible;
                Thumbnail_SelectedTarget.Visibility = System.Windows.Visibility.Hidden;

                Thumbnail_SelectedTarget.Source = IntPtr.Zero;
                Thumbnail_SelectedTarget.Tag = 0;
            };

            var os = Environment.OSVersion;
            if (os.Platform == PlatformID.Win32NT && os.Version.Major == 6)
                v_series = true;// ^ force_nonvseries;

            Listbox_Targets.DataContext = new RunningTargetList(TargetString);
        }
        private void Listbox_Targets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var WAV = Listbox_Targets.SelectedItem as RunningTargetList.AttachVisual;
            if (WAV == null) return;

            if (v_series)
            {
                Thumbnail_SelectedTarget.Source = WAV.DWM;
                Thumbnail_SelectedTarget.Tag = WAV.PID;
            }
            else
                attachCallback(sender as FrameworkElement, WAV.PID);
        }

        //DWM = Devil's Window to Madness
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (!v_series || Thumbnail_SelectedTarget.Source == IntPtr.Zero) return;

            Point mouse = e.GetPosition(Thumbnail_SelectedTarget);
            if (mouse.X >= 0 && mouse.Y >= 0)
                e.MouseDevice.SetCursor(Cursors.Hand);
            else
                e.MouseDevice.UpdateCursor();
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point mouse = e.GetPosition(Thumbnail_SelectedTarget);
            if (mouse.X >= 0 && mouse.Y >= 0 && Thumbnail_SelectedTarget.Tag != null)
                attachCallback(sender as FrameworkElement, (int)Thumbnail_SelectedTarget.Tag);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!v_series)
                Thumbnail_SelectedTarget.Visibility = System.Windows.Visibility.Hidden;
        }

        private void Listbox_Targets_SourceUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
        {
            (sender as ListBox).SelectedIndex = -1;
        }
    }
}
