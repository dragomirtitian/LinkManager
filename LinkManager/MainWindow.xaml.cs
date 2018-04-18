using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
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

    public class ViewModel : Base
    {
        private Entry _selectedEntry = new Entry();
        public Entry SelectedEntry
        {
            get => _selectedEntry;
            set => this.Set(ref _selectedEntry, value);
        }

        public Context Context { get; } = new Context();

        public IEnumerable<Entry> Entries
        {
            get
            {
                IEnumerable<Entry> entries = this.Context.Urls.Local;
                if (!string.IsNullOrEmpty(this.SearchText))
                {
                    var tokens = this.SearchText.Split(',')
                        .Select(t => t.Trim())
                        .Select(t=> new
                        {
                            Negate = t.StartsWith("!"),
                            Exact = t.StartsWith("!#") || t.StartsWith("#"),
                            Text = t.TrimStart('!', '#')
                        });
                    
                    foreach (var token in tokens)
                    {
                        Func<Entry, bool> filter;
                        if (token.Exact)
                        {
                            filter = e => e.Tags?.Split(',').Any(t => t == token.Text) ?? false;
                        }
                        else
                        {
                            filter = e => (e.Tags?.Split(',').Any(t => t.Contains(token.Text)) ?? false) 
                                || e.Title.Contains(token.Text)
                                || e.TextData.Contains(token.Text);
                        }
                        if(token.Negate)
                        {
                            filter = e => !filter(e);
                        }

                        entries = entries.Where(filter);
                    }
                }
                return entries.OrderByDescending(_ => _.Url);
            }
        }
        public ViewModel()
        {
            this.Context.Urls.ToArray();
            //this.Context.Urls.ForEach(u => HtmlHelper.ParseHtml(u.HtmlData));
        }

        ICommand _Save;
        public ICommand Save => Comamnd.Single(ref _Save, _ =>
        {
            if (this.SelectedEntry.Id == 0)
            {
                this.Context.Urls.Add(this.SelectedEntry);
            }
            this.Context.SaveChanges();
            this.RaisePropertyChanged(nameof(this.Entries));
        });

        ICommand _New;
        public ICommand New => Comamnd.Single(ref _New, _ =>
        {
            this.SelectedEntry = new Entry();
        });

        ICommand _Delete;
        public ICommand Delete => Comamnd.Single(ref _Delete, _ =>
        {
            this.Context.Urls.Remove(this.SelectedEntry);
            this.SelectedEntry = this.Entries.FirstOrDefault() ?? new Entry();
            this.Context.SaveChanges();
            this.RaisePropertyChanged(nameof(this.Entries));
        });

        ICommand _AddTag;
        public ICommand AddTag => Comamnd.Single(ref _AddTag, p =>
        {
            this.SelectedEntry.Tags += (string.IsNullOrWhiteSpace(this.SelectedEntry.Tags) ? "" : ",") + p;
        });

        public ICommand Back { get; set; }

        private string tagFilter = "";
        private string _searchText;

        public string TagFilter
        {
            get { return tagFilter; }
            set
            {
                if (this.Set(ref tagFilter, value))
                {
                    this.RaisePropertyChanged(nameof(Tags));
                }
            }
        }


        public IEnumerable<TagViewModel> Tags
        {
            get
            {
                return this.Entries.Select(_ => _.Tags ?? "")
                    .SelectMany(t => t.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    .Select(t => t.Trim())
                    .Distinct()
                    .Where(t => t.ToLower().Contains(TagFilter.ToLower()) || TagFilter.ToLower().Contains(t.ToLower()))
                    .OrderBy(t => t)
                    .Select(t => new TagViewModel
                    {
                        Name = t,
                        AddTag = this.AddTag
                    });
            }
        }
        public string SearchText
        {
            get => _searchText;
            set
            {
                if(this.Set(ref _searchText, value))
                {
                    this.RaisePropertyChanged(nameof(Entries));
                }
            }
        }
        
    }

    public class TagViewModel: Base
    {
        public string Name { get; set; }
        public ICommand AddTag { get; set; }
    }

}
