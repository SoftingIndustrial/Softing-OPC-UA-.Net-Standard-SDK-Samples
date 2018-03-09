using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XamarinSampleClient.Models;
using XamarinSampleClient.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Xaml;

namespace XamarinSampleClient.Views
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

	    /// <summary>When overridden, allows the application developer to customize behavior as the <see cref="T:Xamarin.Forms.Page" /> disappears.</summary>
	    /// <remarks>To be added.</remarks>
	    protected override void OnDisappearing()
	    {
	        //ensure the session is disconnected 
	        m_viewModel.DisconnectSession();

	        base.OnDisappearing();
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
	        NodeValueItem newItem = new NodeValueItem();
	        newItem.Value = 0;
            m_viewModel.ArrayValue.Add(newItem);
	        ArrayValueList.ScrollTo(newItem, ScrollToPosition.MakeVisible, true);
        }

	    private void RemoveArrayItem_OnClicked(object sender, EventArgs e)
	    {
	        if (m_viewModel.ArrayValue.Count > 0)
	        {
	            m_viewModel.ArrayValue.RemoveAt(m_viewModel.ArrayValue.Count-1);

	            if (m_viewModel.ArrayValue.Count > 0)
	            {
	                ArrayValueList.ScrollTo(m_viewModel.ArrayValue[m_viewModel.ArrayValue.Count-1], ScrollToPosition.MakeVisible, true);
	            }
            }
	    }
	}
}