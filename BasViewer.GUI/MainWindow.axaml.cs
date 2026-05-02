using Avalonia.Controls;

namespace BasViewer.GUI
{
    public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			Editor.Text = "Hello from BasAnalysis Viewer\n\nYour output goes here.";
		}
	}
}