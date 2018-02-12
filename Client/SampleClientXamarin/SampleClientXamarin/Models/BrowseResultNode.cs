using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using SampleClientXamarin.Helpers;

namespace SampleClientXamarin.Models
{
    class BrowseResultNode : ObservableObject
    {
        public string Text { get; set; }
        public string Info { get; set; }
    }
}
