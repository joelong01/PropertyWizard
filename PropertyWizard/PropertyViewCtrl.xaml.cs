using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
        private static ObservableCollection<string> _propertyTypes = new ObservableCollection<string>()
        {
             "int","string","bool","byte","sbyte","char","decimal","double","float","int","uint","long","ulong","object","short","ushort","string"

        };


        private  ObservableCollection<string> PropertyTypes { get => _propertyTypes; } 

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
            Debug.WriteLine("TextBox_GotFocus");
            ((TextBox)sender).SelectAll();
            var lv = FindVisualParent<ListViewItem>(sender as TextBox);
            lv.IsSelected = true;
        }

        public static T FindVisualParent<T>(UIElement element) where T : UIElement
        {
            UIElement parent = element; while (parent != null)
            {
                if (parent is T correctlyTyped)
                {
                    return correctlyTyped;
                }
                parent = VisualTreeHelper.GetParent(parent) as UIElement;
            }
            return null;
        }

        private void ComboBox_GotFocus(object sender, RoutedEventArgs e)
        {
            // base.OnGotFocus(e);
            ComboBox cb = sender as ComboBox;
            if (cb == null) return;
            string t = cb.Text.Trim();
            if (t == "") return;
            if (_propertyTypes.Contains(t) == false)
            {
                _propertyTypes.Insert(0, t);
            }

        }
    }
}
