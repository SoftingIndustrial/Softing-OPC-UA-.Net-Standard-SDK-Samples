using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace SampleClientXamarin.Models
{
    class SampleItem
    {
        /// <summary>
        /// Sample Display Name 
        /// </summary>
        public string SampleName { get; set; }

        /// <summary>
        /// Sample Command
        /// </summary>
        public SampleCommand Command { get; set; }

        /// <summary>
        /// Sample descriptive text
        /// </summary>
        [DefaultValue("blalalalalala")]
        public string Description { get; set; }
    }
}
