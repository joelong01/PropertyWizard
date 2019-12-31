using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PropertyWizard
{
    static class Templates
    {
        public static string RegularProperty { get; } = @"
private __TYPE__ __FIELDNAME__ = __DEFAULT__;	
public __TYPE__ __PROPERTYNAME__
{
    get
    {
        return __FIELDNAME__;
    }
    set
    {
        if (value != __FIELDNAME__)
        {
            __FIELDNAME__ = value;
            __NOTIFY__
        }
    }
}";
        public static string Notify { get; } = "NotifyPropertyChanged();";
 
        public static string DependencProperty { get; } = @"
 public static readonly DependencyProperty __PROPERTYNAME__Property = DependencyProperty.Register(""__PROPERTYNAME__"", typeof(__TYPE__), typeof(__CLASS__), new PropertyMetadata(__DEFAULT____DEPENDENCY_PROP_NOTIFY__));
 public string __PROPERTYNAME__
 {
     get => (__TYPE__)GetValue(__PROPERTYNAME__Property);
     set => SetValue(__PROPERTYNAME__Property, value);
 }
";
        public static string DependencyNotify { get; } = @"
private static void  __PROPERTYNAME__Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
{
    var depPropClass = d as __CLASS__;
    var depPropValue = (__TYPE__)e.NewValue;
    depPropClass?.Set__PROPERTYNAME__(depPropValue);
            
}
private void Set__PROPERTYNAME__(__TYPE__ value)
{
    __USER_CODE__
}";

    }
}
