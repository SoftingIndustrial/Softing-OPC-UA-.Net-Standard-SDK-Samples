using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SampleClientXamarin.Models;
using SampleClientXamarin.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Xaml;

namespace SampleClientXamarin.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ReadWriteSamplePage : ContentPage
	{
	    private ReadWriteViewModel m_viewModel;
		public ReadWriteSamplePage ()
		{
			InitializeComponent ();

		    BindingContext = m_viewModel = new ReadWriteViewModel();
		}

	    private void Read_OnClicked(object sender, EventArgs e)
	    {
	        m_viewModel.Read();
	    }

	    private void Write_OnClicked(object sender, EventArgs e)
	    {
	        m_viewModel.Write();
        }

	    private void AddArrayItem_OnClicked(object sender, EventArgs e)
	    {
	        m_viewModel.ArrayValue.Add(new Int64Item());
	    }

	    private void RemoveArrayItem_OnClicked(object sender, EventArgs e)
	    {
	        if (m_viewModel.ArrayValue.Count > 0)
	        {
	            m_viewModel.ArrayValue.RemoveAt(m_viewModel.ArrayValue.Count-1);
            }
	    }
	}
}