using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinkManager
{
    public class Entry : Base
    {
        private string _url;
        private string _summary;
        private string _title;
        private string _tags;

        [Key]
        public int Id { get; set; }
        public string Url
        {
            get => _url;
            set => Set(ref _url, value);
        }
        public string Summary
        {
            get => _summary;
            set => Set(ref _summary, value);
        }
        public string Title
        {
            get => _title;
            set => Set(ref _title, value);
        }
        public string Tags
        {
            get => _tags;
            set => Set(ref _tags, value);
        }

        [Column(TypeName = "text")]
        public string HtmlData { get; set; }
        [Column(TypeName = "text")]
        public string TextData { get; set; }

        public override string ToString()
        {
            return this.Title;
        }
    }

    public class Context : DbContext
    {
        public DbSet<Entry> Urls { get; set; }
    }

}
