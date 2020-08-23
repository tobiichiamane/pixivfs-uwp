using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace PixivFSUWP
{
    /// <summary>
    /// 提供特定于应用程序的行为，以补充默认的应用程序类。
    /// </summary>
    sealed partial class App : Application
    {
        Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        // 什么叫流畅设计啊(后仰
        public static NavigationTransitionInfo FromRightTransitionInfo = new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight };
        public static NavigationTransitionInfo FromLeftTransitionInfo = new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromLeft };
        public static NavigationTransitionInfo DrillInTransitionInfo = new DrillInNavigationTransitionInfo();

        /// <summary>
        /// 初始化单一实例应用程序对象。这是执行的创作代码的第一行，
        /// 已执行，逻辑上等同于 main() 或 WinMain()。
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            this.UnhandledException += App_UnhandledException;
            switch (localSettings.Values["ColorTheme"])
            {
                case false:
                    Current.RequestedTheme = ApplicationTheme.Light;
                    break;
                case true:
                    Current.RequestedTheme = ApplicationTheme.Dark;
                    break;
            }
        }

        private void CoreWindow_PointerPressed(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.PointerEventArgs args)
        {
            if (args.CurrentPoint.Properties.IsXButton1Pressed) Data.OverAll.TheMainPage.GoBack();
            bool forward = args.CurrentPoint.Properties.IsXButton2Pressed;
        }

        private void App_UnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            //遇到未处理的异常时
            e.Handled = true;
            if (localSettings.Values.ContainsKey("exception"))
                localSettings.Values.Remove("exception");
            localSettings.Values["exception"] = e.Exception.ToString();
            localSettings.Values["isCrashed"] = true;
            //只储存异常信息，而不强制退出应用。很多异常不影响应用稳定性。
            //MessageDialog dialog = new MessageDialog("Pixiv UWP has crashed. Please restart this app in order to report this issue.\n程序已崩溃，请重启本应用以便于报告此问题。", "Crashed/程序崩溃");
            //await dialog.ShowAsync();
            //this.Exit();
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            //由Uri启动时
            if (args.Kind == ActivationKind.Protocol)
            {
                ProtocolActivatedEventArgs eventArgs = args as ProtocolActivatedEventArgs;

                //储存Uri
                Data.OverAll.AppUri = eventArgs.Uri;

                //按照正常流程启动应用
                Frame rootFrame = Window.Current.Content as Frame;
                if (rootFrame == null)
                {
                    rootFrame = new Frame();
                    rootFrame.NavigationFailed += OnNavigationFailed;
                    if (eventArgs.PreviousExecutionState == ApplicationExecutionState.Terminated)
                    {
                        //TODO: 从之前挂起的应用程序加载状态
                    }
                    Window.Current.Content = rootFrame;
                }
                if (rootFrame.Content == null)
                {
                    rootFrame.Navigate(typeof(LoginPage));
                }
                else if (rootFrame.Content is MainPage)
                {
                    (rootFrame.Content as MainPage).HandleUri();
                }
                Window.Current.Activate();
            }
        }

        /// <summary>
        /// 在应用程序由最终用户正常启动时进行调用。
        /// 将在启动应用程序以打开特定文件等情况下使用。
        /// </summary>
        /// <param name="e">有关启动请求和过程的详细信息。</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;

            Frame rootFrame = Window.Current.Content as Frame;

            // 不要在窗口已包含内容时重复应用程序初始化，
            // 只需确保窗口处于活动状态
            if (rootFrame == null)
            {
                // 创建要充当导航上下文的框架，并导航到第一页
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: 从之前挂起的应用程序加载状态
                }

                // 将框架放在当前窗口中
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    ////检测上次的崩溃
                    //if (localSettings.Values["isCrashed"] != null && (bool)localSettings.Values["isCrashed"] == true)
                    //{
                    //    localSettings.Values.Remove("isCrashed");
                    //    var yesCommand = new UICommand("Yes/是");
                    //    var noCommand = new UICommand("No/否");
                    //    MessageDialog dialog = new MessageDialog("A crash has been detected in your last session. Do you want to report this issue?\n在您上次的会话中我们探测到一次崩溃。请问您要报告此问题吗？", "Crash Report/错误报告");
                    //    dialog.Commands.Add(yesCommand);
                    //    dialog.Commands.Add(noCommand);
                    //    dialog.DefaultCommandIndex = 0;
                    //    var cmd = await dialog.ShowAsync();
                    //    if (cmd == yesCommand)
                    //    {
                    //        rootFrame.Navigate(typeof(ReportIssuePage), e.Arguments);
                    //    }
                    //    else
                    //    {
                    //        rootFrame.Navigate(typeof(LoginPage), e.Arguments);
                    //    }
                    //}
                    //else 
                    //不再在程序起始时弹出反馈窗口
                    rootFrame.Navigate(typeof(LoginPage), e.Arguments, App.FromRightTransitionInfo);
                }
                // 确保当前窗口处于活动状态
                Window.Current.Activate();
            }
        }

        /// <summary>
        /// 导航到特定页失败时调用
        /// </summary>
        ///<param name="sender">导航失败的框架</param>
        ///<param name="e">有关导航失败的详细信息</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// 在将要挂起应用程序执行时调用。  在不知道应用程序
        /// 无需知道应用程序会被终止还是会恢复，
        /// 并让内存内容保持不变。
        /// </summary>
        /// <param name="sender">挂起的请求的源。</param>
        /// <param name="e">有关挂起请求的详细信息。</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: 保存应用程序状态并停止任何后台活动
            deferral.Complete();
        }
    }
}
