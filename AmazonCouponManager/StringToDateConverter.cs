using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Xml.Linq;

namespace AmazonCouponManager
{
	[ValueConversion(typeof(XElement), typeof(string))]
	public class StringToDateConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			try
			{
				var element = (XElement)value;
				if (element.Name == "coupon")
					return "\t" + element.Attribute("date_fin").Value + "\t" + element.Attribute("value").Value;
				return "\t" + element.Descendants("coupon").Last().Attribute("date_fin").Value;
			}
			catch (Exception e)
			{
				MessageBox.Show(e.ToString());
			}
			return "";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
