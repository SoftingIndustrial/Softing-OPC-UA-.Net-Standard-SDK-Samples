using System;
using System.Collections.Generic;
using System.Text;
using SampleClientXamarin.Helpers;

namespace SampleClientXamarin.Models
{
    class Int64Item:ObservableObject
    {
        private Int64 m_value;

        public long Value
        {
            get { return m_value; }
            set { SetProperty(ref m_value, value); }
        }
    }
}
