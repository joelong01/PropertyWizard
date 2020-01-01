using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PropertyWizard
{
    public class PropertyModel : INotifyPropertyChanged
    {
		string _PropertyName = "";
		string _FieldName = "";
		string _PropertyType = "";
		string _ClassType = "";
		string _Default = "";		
		
		bool _isDependencyProperty = false;
		bool _ChangeNotification = false;
		bool _HasSetter = true;
		public bool HasSetter
		{
			get
			{
				return _HasSetter;
			}
			set
			{
				if (_HasSetter != value)
				{
					_HasSetter = value;
					NotifyPropertyChanged();
				}
			}
		}

		public string FieldName
		{
			get
			{
				return _FieldName;
			}
			set
			{
				if (_FieldName != value)
				{
					_FieldName = value.Trim();
					NotifyPropertyChanged();
				}
			}
		}
		public bool ChangeNotification
		{
			get
			{
				return _ChangeNotification;
			}
			set
			{
				if (_ChangeNotification != value)
				{
					_ChangeNotification = value;
					NotifyPropertyChanged();
				}
			}
		}
		public bool IsDependencyProperty
		{
			get
			{
				return _isDependencyProperty;
			}
			set
			{
				if (_isDependencyProperty != value)
				{
					_isDependencyProperty = value;
					NotifyPropertyChanged();
				}
			}
		}
		public string Default
		{
			get
			{
				return _Default;
			}
			set
			{
				if (_Default != value)
				{
					_Default = value.Trim();
					NotifyPropertyChanged();
				}
			}
		}
		public string ClassType
		{
			get
			{
				return _ClassType;
			}
			set
			{
				if (_ClassType != value)
				{
					_ClassType = value.Trim();
					NotifyPropertyChanged();
				}
			}
		}
		public string PropertyType
		{
			get
			{
				return _PropertyType;
			}
			set
			{
				if (_PropertyType != value)
				{
					_PropertyType = value.Trim();
					NotifyPropertyChanged();
				}
			}
		}
		public string PropertyName
		{
			get
			{
				return _PropertyName;
			}
			set
			{
				if (_PropertyName != value)
				{
					_PropertyName = value.Trim();
					if (FieldName == "" && _PropertyName.Length > 0)
					{
						FieldName = "_" + char.ToLower(_PropertyName[0]) + _PropertyName.Substring(1);
					}
					NotifyPropertyChanged();
				}
			}
		}
		string _UserCode = "";
		public string UserCode
		{
			get
			{
				return _UserCode;
			}
			set
			{
				if (_UserCode != value)
				{
					_UserCode = value.Trim();
					NotifyPropertyChanged();
				}
			}
		}



		public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
			Debug.WriteLine($"NotifyPropertyChanged: [{propertyName}={this.GetType().GetProperty(propertyName).GetValue(this)}]");
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
