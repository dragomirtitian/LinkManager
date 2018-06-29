using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace LinkManager
{

    public class ViewModel : Base
    {
        private Entry _selectedEntry = new Entry { Url = "https://stackoverflow.com/" };
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
                        .Select(t => new
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
                        if (token.Negate)
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

        ICommand _All;
        public ICommand All => Comamnd.Single(ref _All, async _ =>
        {
            var doc = new HtmlDocument();
            var regex = new Regex("/questions/[0-9]+");
            doc.LoadHtml(this.SelectedEntry.HtmlData);

            var uri = new Uri(this.SelectedEntry.Url);
            var basePath = this.SelectedEntry.Url.Replace(uri.PathAndQuery, "");

            var nodes = doc.DocumentNode.SelectNodes("//a")
                .Select(a => a.GetAttributeValue("href", null))
                .Where(a => a != null)
                .Where(a => regex.IsMatch(a))
                .Select(url => basePath + url)
                .ToArray();
            int newEntries = 0;
            foreach (var url in nodes)
            {
                this.SelectedEntry = this.Context.Urls.FirstOrDefault(u => u.Url == url)
                        ?? new Entry { Url = url };

                await Utils.WaitForStableValue(() => this.LastLoad, 2000);
                if (string.IsNullOrEmpty(this.SelectedEntry.Tags))
                {
                    this.SelectedEntry.Tags = "*auto";
                }
                if (this.SelectedEntry.Id == 0)
                {
                    this.Context.Urls.Add(this.SelectedEntry);
                    newEntries++;
                }
            }
            MessageBox.Show($"New entries {newEntries}");
            this.Context.SaveChanges();
            this.RaisePropertyChanged(nameof(this.Entries));
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
                if (this.Set(ref _searchText, value))
                {
                    this.RaisePropertyChanged(nameof(Entries));
                }
            }
        }

        public DateTime LastLoad { get; set; }
    }

    public class TagViewModel : Base
    {
        public string Name { get; set; }
        public ICommand AddTag { get; set; }
    }
}
