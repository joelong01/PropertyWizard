using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace PropertyWizard
{
    public sealed partial class PropertyViewCtrl : UserControl
    {
        public static readonly DependencyProperty ModelProperty = DependencyProperty.Register("Model", typeof(PropertyModel), typeof(PropertyViewCtrl), new PropertyMetadata(null));
        public PropertyModel Model
        {
            get => (PropertyModel)GetValue(ModelProperty);
            set => SetValue(ModelProperty, value);
        }
        public PropertyViewCtrl()
        {
            this.InitializeComponent();
        }

        public PropertyViewCtrl(PropertyModel model)
        {
            this.InitializeComponent();
            Model = model;
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {

        }
    }
}
