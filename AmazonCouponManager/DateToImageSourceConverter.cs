using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Xml.Linq;

namespace AmazonCouponManager
{
	[ValueConversion(typeof(XElement), typeof(string))]
	public class DateToImageSourceConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			try
			{
				//var dateTime = System.Convert.ToDateTime(value);
				//if (dateTime.Date > DateTime.Now)
				//	return "Images\\check.png";
				var element= (XElement)value;
				if (element.Name == "coupon")
				{
					if (System.Convert.ToDateTime(element.Attribute("date_fin").Value).Date > DateTime.Now)
						return "Images\\check.png";
				}
				if (System.Convert.ToDateTime(element.Descendants("coupon").Last().Attribute("date_fin").Value).Date > DateTime.Now)
					return "Images\\check.png";
			}
			catch (Exception e)
			{
				e.ToString();
			}
			return "Images\\cross.png";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
