using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LinkManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            base.DataContext = new ViewModel()
            {
                Back = browser.BackCommand
            };
        }
        
        private void ChromiumWebBrowser_TitleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (this.DataContext.SelectedEntry != null && string.IsNullOrEmpty(this.DataContext.SelectedEntry.Title))
                {
                    this.DataContext.SelectedEntry.Title = ((string)e.NewValue).Replace(" - Stack Overflow", "");
                }
            }));
        }
        public new ViewModel DataContext => base.DataContext as ViewModel;
        private void ChromiumWebBrowser_FrameLoadEnd(object sender, CefSharp.FrameLoadEndEventArgs e)
        {
            this.Dispatcher.BeginInvoke(new Action(async () =>
            {
                this.DataContext.LastLoad = DateTime.Now;
                if (this.DataContext.SelectedEntry?.Url != e.Browser.MainFrame.Url)
                {
                    var url = e.Browser.MainFrame.Url;
                    var value = await e.Browser.MainFrame.EvaluateScriptAsync("document.title");
                    this.DataContext.SelectedEntry = this.DataContext.Context.Urls.FirstOrDefault(u => u.Url == url) ?? new Entry
                    {
                        Url = url,
                        Title = ((string)value.Result).Replace(" - Stack Overflow", "")
                    };
                }
                if (this.DataContext.SelectedEntry?.Url == e.Url)
                {
                    this.DataContext.SelectedEntry.HtmlData = await e.Browser.MainFrame.GetSourceAsync();
                    this.DataContext.SelectedEntry.TextData = await e.Browser.MainFrame.GetTextAsync();
                }
            }));
        }
    }


}
